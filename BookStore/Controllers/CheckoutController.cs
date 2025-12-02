using BookStore.Data;
using BookStore.Helpers;
using BookStore.Models;
using BookStore.Models.ViewModels;
using BookStore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using BookStore.Services;

namespace BookStore.Controllers;

[Authorize]
public class CheckoutController : Controller
{
    private readonly ICartService _cart;
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userMgr;
    private readonly INotificationService _notificationService;
    private readonly IQKTPaymentService _qktPaymentService;
    
    public CheckoutController(ICartService cart, ApplicationDbContext db,
                              UserManager<IdentityUser> userMgr, INotificationService notificationService,
                              IQKTPaymentService qktPaymentService)
    {
        _cart = cart; _db = db; _userMgr = userMgr; _notificationService = notificationService;
        _qktPaymentService = qktPaymentService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        // Chặn admin không cho truy cập trang checkout
        var user = await _userMgr.GetUserAsync(User);
        if (user != null && await _userMgr.IsInRoleAsync(user, "Admin"))
        {
            TempData["Error"] = "Tài khoản Admin không thể mua hàng.";
            return RedirectToAction("Index", "Cart");
        }

        // Xóa các TempData cũ ngay từ đầu để tránh hiển thị lỗi không đúng
        TempData.Remove("Warning");
        TempData.Remove("Error");
        ModelState.Clear(); // Xóa các lỗi ModelState cũ
        
        var allItems = await _cart.GetItemsAsync();
        if (!allItems.Any())
        {
            TempData["Warning"] = "Giỏ hàng trống. Vui lòng thêm sản phẩm vào giỏ hàng.";
            return RedirectToAction("Index", "Cart");
        }

        // Đọc selectedBookIds trực tiếp từ query string
        var selectedBookIds = Request.Query["selectedBookIds"]
            .Where(v => !string.IsNullOrEmpty(v))
            .Select(v => int.TryParse(v, out var id) ? id : (int?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToList();

        // Lọc các item được chọn
        List<CartItem> items;
        
        if (selectedBookIds != null && selectedBookIds.Count > 0)
        {
            items = allItems.Where(i => selectedBookIds.Contains(i.BookId)).ToList();
            
            // Kiểm tra nếu không tìm thấy item nào
            if (!items.Any())
            {
                TempData["Error"] = "Không tìm thấy sản phẩm đã chọn trong giỏ hàng. Vui lòng thử lại.";
                return RedirectToAction("Index", "Cart");
            }
        }
        else
        {
            // Nếu không có selectedBookIds, lấy tất cả (tương thích ngược)
            items = allItems;
        }

        var vm = new CheckoutVM
        {
            Items = items
        };
        return View(vm);
    }
    private async Task<Order?> CreateOrderAsync(CheckoutVM vm)
    {
        if (!ModelState.IsValid)
        {
            return null;
        }

        // Sử dụng items từ ViewModel (đã được lọc theo selectedBookIds)
        var items = vm.Items ?? new List<CartItem>();
        if (!items.Any())
        {
            ModelState.AddModelError(string.Empty, "Vui lòng chọn ít nhất một sản phẩm để thanh toán.");
            return null;
        }

        var user = await _userMgr.GetUserAsync(User);
        if (user == null) return null;

        // Chặn admin không cho tạo đơn hàng (kiểm tra lại để chắc chắn)
        if (await _userMgr.IsInRoleAsync(user, "Admin"))
        {
            ModelState.AddModelError(string.Empty, "Tài khoản Admin không thể mua hàng.");
            return null;
        }

        // Kiểm tra tồn kho
        var ids = items.Select(i => i.BookId).ToList();
        var books = await _db.Books
            .Include(b => b.FlashSale)
            .Where(b => ids.Contains(b.Id))
            .ToListAsync();
        
        // Kiểm tra xem tất cả sách có tồn tại không
        var missingBookIds = ids.Where(id => !books.Any(b => b.Id == id)).ToList();
        if (missingBookIds.Any())
        {
            ModelState.AddModelError(string.Empty, "Một số sách không còn tồn tại trong hệ thống.");
            return null;
        }
        
        foreach (var it in items)
        {
            var b = books.FirstOrDefault(x => x.Id == it.BookId);
            if (b == null)
            {
                ModelState.AddModelError(string.Empty, $"Không tìm thấy sách với ID {it.BookId}.");
                return null;
            }
            if (b.Stock < it.Quantity)
            {
                ModelState.AddModelError(string.Empty, $"Sách '{b.Title}' không đủ tồn kho (chỉ còn {b.Stock}).");
                return null;
            }
        }

        // Tạo order
        var order = new Order
        {
            UserId = user.Id,
            ShippingAddress = (vm.ShippingAddress ?? string.Empty).Trim(),
            PhoneNumber = (vm.PhoneNumber ?? string.Empty).Trim(),
            CreatedAt = DateTime.UtcNow,
            Status = "Pending" // 🟢 LUÔN LÀ PENDING
        };
        _db.Orders.Add(order);
        await _db.SaveChangesAsync(); // 🟢 Lưu để lấy OrderId

            decimal subtotal = 0;
            foreach (var it in items)
            {
                var b = books.FirstOrDefault(x => x.Id == it.BookId);
                if (b == null)
                {
                    ModelState.AddModelError(string.Empty, $"Không tìm thấy sách với ID {it.BookId}.");
                    return null;
                }
                // Sử dụng giá sau flash sale nếu có
                var unitPrice = b.IsOnFlashSale ? b.FinalPrice : b.Price;
                _db.OrderItems.Add(new OrderItem
                {
                    OrderId = order.Id,
                    BookId = b.Id,
                    UnitPrice = unitPrice,
                    Quantity = it.Quantity
                });
                subtotal += unitPrice * it.Quantity;
            }

        // Áp dụng logic giảm giá tự động (theo tổng tiền)
        decimal autoDiscountAmount = 0;
        if (subtotal >= 5000000)
        {
            autoDiscountAmount = 500000;
        }
        else if (subtotal >= 2000000)
        {
            autoDiscountAmount = 100000;
        }
        else if (subtotal >= 1000000)
        {
            autoDiscountAmount = 70000;
        }
        else if (subtotal >= 500000)
        {
            autoDiscountAmount = 50000;
        }
        else if (subtotal >= 200000)
        {
            autoDiscountAmount = 10000;
        }

        // Áp dụng coupon nếu có
        decimal couponDiscount = 0;
        Coupon? appliedCoupon = null;
        string? appliedCouponCode = null; // Lưu coupon code đã được áp dụng
        
        if (!string.IsNullOrWhiteSpace(vm.CouponCode))
        {
            var couponCode = vm.CouponCode.Trim().ToUpper();
            // Lấy coupon từ database (KHÔNG dùng IsValid vì là computed property, không thể dùng trong LINQ)
            appliedCoupon = await _db.Coupons
                .FirstOrDefaultAsync(c => c.Code == couponCode);

            if (appliedCoupon != null)
            {
                // Kiểm tra tính hợp lệ của coupon (kiểm tra từng điều kiện riêng)
                var now = TimeHelper.GetVietnamTime(); // Dùng VN time (GMT+7)
                var startDate = TimeHelper.ToVietnamTime(appliedCoupon.StartDate);
                var endDate = TimeHelper.ToVietnamTime(appliedCoupon.EndDate);
                bool isValid = appliedCoupon.IsActive &&
                               now >= startDate &&
                               now <= endDate &&
                               (appliedCoupon.UsageLimit == null || appliedCoupon.UsedCount < appliedCoupon.UsageLimit);

                if (!isValid)
                {
                    // Coupon không hợp lệ - trả về thông báo lỗi cụ thể
                    if (!appliedCoupon.IsActive)
                    {
                        ModelState.AddModelError("CouponCode", "Mã giảm giá đã bị tắt.");
                    }
                    else if (now < startDate)
                    {
                        ModelState.AddModelError("CouponCode", $"Mã giảm giá chưa có hiệu lực. Mã sẽ có hiệu lực từ {startDate:dd/MM/yyyy}.");
                    }
                    else if (now > endDate)
                    {
                        ModelState.AddModelError("CouponCode", $"Mã giảm giá đã hết hạn. Mã đã hết hạn vào {endDate:dd/MM/yyyy}.");
                    }
                    else if (appliedCoupon.UsageLimit.HasValue && appliedCoupon.UsedCount >= appliedCoupon.UsageLimit)
                    {
                        ModelState.AddModelError("CouponCode", $"Mã giảm giá đã hết lượt sử dụng ({appliedCoupon.UsedCount}/{appliedCoupon.UsageLimit}).");
                    }
                }
                else
                {
                    // Kiểm tra số lần sử dụng của user cho mã này
                    if (appliedCoupon.MaxUsagePerUser.HasValue)
                    {
                        var userUsageCount = await _db.Orders
                            .AsNoTracking()
                            .CountAsync(o => o.UserId == user.Id && 
                                            o.CouponCode == couponCode && 
                                            o.Status != "Canceled");
                        
                        if (userUsageCount >= appliedCoupon.MaxUsagePerUser.Value)
                        {
                            ModelState.AddModelError("CouponCode", $"Bạn đã sử dụng mã này {userUsageCount}/{appliedCoupon.MaxUsagePerUser.Value} lần. Mỗi user chỉ được sử dụng tối đa {appliedCoupon.MaxUsagePerUser.Value} lần.");
                            isValid = false;
                        }
                    }

                    // Coupon hợp lệ, kiểm tra đơn hàng tối thiểu
                    if (isValid && appliedCoupon.MinOrderAmount.HasValue && subtotal < appliedCoupon.MinOrderAmount.Value)
                    {
                        ModelState.AddModelError("CouponCode", $"Đơn hàng tối thiểu để áp dụng mã này là {appliedCoupon.MinOrderAmount.Value:N0} ₫");
                        isValid = false;
                    }
                    if (isValid)
                    {
                        // Tính toán giảm giá từ coupon
                        if (appliedCoupon.DiscountType == "Percentage")
                        {
                            couponDiscount = subtotal * (appliedCoupon.DiscountValue / 100m);
                            if (appliedCoupon.MaxDiscount.HasValue && couponDiscount > appliedCoupon.MaxDiscount.Value)
                            {
                                couponDiscount = appliedCoupon.MaxDiscount.Value;
                            }
                        }
                        else // FixedAmount
                        {
                            couponDiscount = appliedCoupon.DiscountValue;
                        }

                        // Coupon discount không được vượt quá subtotal
                        if (couponDiscount > subtotal)
                        {
                            couponDiscount = subtotal;
                        }

                        // Lấy discount lớn hơn giữa auto discount và coupon discount
                        if (couponDiscount > autoDiscountAmount)
                        {
                            autoDiscountAmount = 0; // Chỉ dùng coupon
                            appliedCouponCode = appliedCoupon.Code; // Lưu coupon code để set sau
                            
                            // Tăng số lần sử dụng
                            appliedCoupon.UsedCount++;
                        }
                        else
                        {
                            // Dùng auto discount thay vì coupon - không lưu coupon code
                            couponDiscount = 0;
                            appliedCouponCode = null; // Không dùng coupon
                        }
                    }
                }
            }
            else
            {
                ModelState.AddModelError("CouponCode", "Mã giảm giá không tồn tại. Vui lòng kiểm tra lại.");
            }
        }

        // Nếu có lỗi coupon, bỏ qua coupon nhưng vẫn cho phép đặt hàng
        // (User có thể muốn đặt hàng mà không dùng coupon nếu coupon không hợp lệ)
        if (ModelState.ContainsKey("CouponCode") && ModelState["CouponCode"]!.Errors.Count > 0)
        {
            // Xóa coupon discount và coupon code nếu có lỗi
            couponDiscount = 0;
            appliedCouponCode = null; // Không lưu coupon code nếu có lỗi
        }

        decimal finalTotal = subtotal - autoDiscountAmount - couponDiscount;
        if (finalTotal < 0) finalTotal = 0;

        order.TotalAmount = finalTotal;
        
        // Set DiscountAmount và CouponCode - ưu tiên coupon nếu có, nếu không thì dùng auto discount
        if (couponDiscount > 0 && !string.IsNullOrEmpty(appliedCouponCode))
        {
            order.DiscountAmount = couponDiscount;
            order.CouponCode = appliedCouponCode; // Set coupon code khi dùng coupon
        }
        else if (autoDiscountAmount > 0)
        {
            order.DiscountAmount = autoDiscountAmount;
            order.CouponCode = null; // Không có CouponCode vì dùng auto discount
        }
        else
        {
            order.DiscountAmount = 0;
            order.CouponCode = null;
        }

        await _db.SaveChangesAsync(); // Lưu tổng tiền và coupon usage

        return order;
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(CheckoutVM vm)
    {
        // Chặn admin không cho tạo đơn hàng
        var user = await _userMgr.GetUserAsync(User);
        if (user != null && await _userMgr.IsInRoleAsync(user, "Admin"))
        {
            TempData["Error"] = "Tài khoản Admin không thể mua hàng.";
            return RedirectToAction("Index", "Cart");
        }

        // Xóa ModelState cũ trước khi xử lý
        ModelState.Clear();
        
        // Lấy lại items từ giỏ hàng (vì POST không gửi items)
        var allItems = await _cart.GetItemsAsync();
        
        // Đọc selectedBookIds từ form (nếu có) hoặc lấy tất cả
        var selectedBookIds = Request.Form["selectedBookIds"]
            .Where(v => !string.IsNullOrEmpty(v))
            .Select(v => int.TryParse(v, out var id) ? id : (int?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToList();
        
        // Lọc items được chọn
        List<CartItem> items;
        if (selectedBookIds != null && selectedBookIds.Count > 0)
        {
            items = allItems.Where(i => selectedBookIds.Contains(i.BookId)).ToList();
        }
        else
        {
            items = allItems;
        }
        
        // Gán lại items vào ViewModel
        vm.Items = items;
        
        // Kiểm tra phương thức thanh toán
        var paymentMethod = vm.PaymentMethod ?? "COD";
        
        // 1. Tạo đơn hàng "Pending"
        var order = await CreateOrderAsync(vm);
        if (order == null)
        {
            return View(vm);
        }

        // 2. Xử lý theo phương thức thanh toán
        if (paymentMethod == "QKT")
        {
            // QKT Payment: Tạo URL thanh toán và redirect
            // KHÔNG trừ kho và KHÔNG xóa giỏ hàng ở đây
            // Sẽ xử lý sau khi thanh toán thành công (trong QKTPaymentController)
            
            var paymentUrl = _qktPaymentService.CreatePaymentUrl(order, HttpContext);
            return Redirect(paymentUrl);
        }
        else
        {
            // COD: Xử lý logic cho COD (Trừ kho, Xóa giỏ)
            var orderItems = await _db.OrderItems.Where(oi => oi.OrderId == order.Id).ToListAsync();
            foreach (var item in orderItems)
            {
                var book = await _db.Books.FindAsync(item.BookId);
                if (book != null)
                {
                    // Kiểm tra lại tồn kho trước khi trừ (tránh race condition)
                    if (book.Stock < item.Quantity)
                    {
                        // Nếu không đủ kho, hủy đơn và hoàn lại
                        order.Status = "Canceled";
                        await _db.SaveChangesAsync();
                        TempData["Error"] = $"Sách '{book.Title}' không đủ tồn kho. Đơn hàng đã bị hủy.";
                        return RedirectToAction("Index", "Cart");
                    }
                    book.Stock -= item.Quantity; // Trừ kho
                }
            }

            order.Status = "Confirmed"; // 🟢 COD thì xác nhận luôn
            await _db.SaveChangesAsync();

            // 3. Xóa các item đã thanh toán khỏi giỏ hàng (chỉ xóa các item trong order)
            var orderedBookIds = orderItems.Select(oi => oi.BookId).ToList();
            foreach (var bookId in orderedBookIds)
            {
                await _cart.RemoveAsync(bookId);
            }

            // 4. Tạo thông báo cho user
            await _notificationService.CreateNotificationAsync(
                user.Id,
                "Đặt hàng thành công",
                $"Đơn hàng #{order.Id} của bạn đã được xác nhận. Tổng tiền: {order.TotalAmount:N0} ₫",
                "Success",
                $"/Orders/Details/{order.Id}"
            );

            // 5. Tạo thông báo cho tất cả admin về đơn hàng mới
            var adminUsers = await _userMgr.GetUsersInRoleAsync("Admin");
            foreach (var admin in adminUsers)
            {
                await _notificationService.CreateNotificationAsync(
                    admin.Id,
                    "Đơn hàng mới",
                    $"Có đơn hàng mới #{order.Id} từ {user.Email}. Tổng tiền: {order.TotalAmount:N0} ₫",
                    "Info",
                    $"/Admin/Orders/Details/{order.Id}"
                );
            }
            
            TempData["Success"] = $"Đặt hàng COD thành công! Mã đơn: #{order.Id}";
            return RedirectToAction("Details", "Orders", new { id = order.Id });
        }
    }

    // POST: /Checkout/ValidateCoupon
    [HttpPost]
    [Route("Checkout/ValidateCoupon")]
    [Produces("application/json")]
    [IgnoreAntiforgeryToken] // Bỏ qua CSRF cho AJAX request để đơn giản hóa
    public async Task<IActionResult> ValidateCoupon([FromForm] string couponCode, [FromForm] decimal subtotal)
    {
        if (string.IsNullOrWhiteSpace(couponCode))
        {
            return Json(new { success = false, message = "Vui lòng nhập mã giảm giá." });
        }

        var code = couponCode.Trim().ToUpper();
        var coupon = await _db.Coupons
            .FirstOrDefaultAsync(c => c.Code == code);

        if (coupon == null)
        {
            return Json(new { success = false, message = "Mã giảm giá không tồn tại. Vui lòng kiểm tra lại." });
        }

        // Kiểm tra từng điều kiện và trả về thông báo cụ thể
        var now = TimeHelper.GetVietnamTime(); // Dùng VN time (GMT+7)
        var startDate = TimeHelper.ToVietnamTime(coupon.StartDate);
        var endDate = TimeHelper.ToVietnamTime(coupon.EndDate);
        
        if (!coupon.IsActive)
        {
            return Json(new { success = false, message = "Mã giảm giá đã bị tắt." });
        }
        
        if (now < startDate)
        {
            return Json(new { success = false, message = $"Mã giảm giá chưa có hiệu lực. Mã sẽ có hiệu lực từ {startDate:dd/MM/yyyy}." });
        }
        
        if (now > endDate)
        {
            return Json(new { success = false, message = $"Mã giảm giá đã hết hạn. Mã đã hết hạn vào {endDate:dd/MM/yyyy}." });
        }
        
        if (coupon.UsageLimit.HasValue && coupon.UsedCount >= coupon.UsageLimit)
        {
            return Json(new { success = false, message = $"Mã giảm giá đã hết lượt sử dụng ({coupon.UsedCount}/{coupon.UsageLimit})." });
        }

        // Kiểm tra số lần sử dụng của user cho mã này
        if (coupon.MaxUsagePerUser.HasValue)
        {
            var user = await _userMgr.GetUserAsync(User);
            if (user != null)
            {
                var userUsageCount = await _db.Orders
                    .AsNoTracking()
                    .CountAsync(o => o.UserId == user.Id && 
                                    o.CouponCode == code && 
                                    o.Status != "Canceled");
                
                if (userUsageCount >= coupon.MaxUsagePerUser.Value)
                {
                    return Json(new { 
                        success = false, 
                        message = $"Bạn đã sử dụng mã này {userUsageCount}/{coupon.MaxUsagePerUser.Value} lần. Mỗi user chỉ được sử dụng tối đa {coupon.MaxUsagePerUser.Value} lần." 
                    });
                }
            }
        }

        // Kiểm tra đơn hàng tối thiểu
        if (coupon.MinOrderAmount.HasValue && subtotal < coupon.MinOrderAmount.Value)
        {
            return Json(new { 
                success = false, 
                message = $"Đơn hàng tối thiểu để áp dụng mã này là {coupon.MinOrderAmount.Value:N0} ₫." 
            });
        }

        // Tính toán giảm giá
        decimal discount = 0;
        if (coupon.DiscountType == "Percentage")
        {
            discount = subtotal * (coupon.DiscountValue / 100m);
            if (coupon.MaxDiscount.HasValue && discount > coupon.MaxDiscount.Value)
            {
                discount = coupon.MaxDiscount.Value;
            }
        }
        else // FixedAmount
        {
            discount = coupon.DiscountValue;
        }

        // Không được vượt quá subtotal
        if (discount > subtotal)
        {
            discount = subtotal;
        }

        return Json(new { 
            success = true, 
            discount = discount,
            message = coupon.DiscountType == "Percentage" 
                ? $"Giảm {coupon.DiscountValue}% (tối đa {coupon.MaxDiscount?.ToString("N0") ?? "không giới hạn"} ₫)"
                : $"Giảm {coupon.DiscountValue:N0} ₫"
        });
    }

}
