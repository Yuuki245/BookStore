using BookStore.Data;
using BookStore.Models;
using BookStore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Controllers
{
    [AllowAnonymous]
    public class QKTPaymentController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ICartService _cart;
        private readonly UserManager<IdentityUser> _userMgr;
        private readonly INotificationService _notificationService;

        public QKTPaymentController(
            ApplicationDbContext db,
            ICartService cart,
            UserManager<IdentityUser> userMgr,
            INotificationService notificationService)
        {
            _db = db;
            _cart = cart;
            _userMgr = userMgr;
            _notificationService = notificationService;
        }

        // Hiển thị trang thanh toán QKT
        [HttpGet]
        public async Task<IActionResult> Payment(int orderId, string amount)
        {
            var order = await _db.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("Index", "Cart");
            }

            // Kiểm tra đơn hàng đã được thanh toán chưa
            if (order.Status != "Pending")
            {
                TempData["Warning"] = $"Đơn hàng #{order.Id} đã được xử lý (Trạng thái: {order.Status}).";
                return RedirectToAction("Details", "Orders", new { id = order.Id });
            }

            // Parse amount từ string với InvariantCulture để tránh vấn đề locale
            decimal parsedAmount = 0;
            if (!string.IsNullOrEmpty(amount))
            {
                if (decimal.TryParse(amount, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
                {
                    parsedAmount = parsed;
                }
                else
                {
                    // Nếu parse thất bại, dùng TotalAmount từ order
                    parsedAmount = order.TotalAmount;
                }
            }
            else
            {
                parsedAmount = order.TotalAmount;
            }

            ViewBag.OrderId = orderId;
            ViewBag.Amount = parsedAmount;
            ViewBag.Order = order;

            return View();
        }

        // Xử lý kết quả thanh toán
        [HttpPost]
        public async Task<IActionResult> ProcessPayment(int orderId, string result)
        {
            var order = await _db.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("Index", "Cart");
            }

            // Chặn admin không cho xử lý thanh toán
            var orderUser = await _userMgr.FindByIdAsync(order.UserId);
            if (orderUser != null && await _userMgr.IsInRoleAsync(orderUser, "Admin"))
            {
                order.Status = "Canceled";
                await _db.SaveChangesAsync();
                TempData["Error"] = "Tài khoản Admin không thể mua hàng.";
                return RedirectToAction("Index", "Cart");
            }

            // Chỉ xử lý nếu đơn hàng đang "Pending"
            if (order.Status != "Pending")
            {
                TempData["Warning"] = $"Đơn hàng #{order.Id} đã được xử lý (Trạng thái: {order.Status}).";
                return RedirectToAction("Details", "Orders", new { id = order.Id });
            }

            if (result == "success")
            {
                // Thanh toán thành công
                await ProcessSuccessfulPayment(order);
                TempData["Success"] = $"Thanh toán thành công! Đơn hàng #{order.Id} đã được xác nhận.";
                return RedirectToAction("Details", "Orders", new { id = order.Id });
            }
            else
            {
                // Thanh toán thất bại
                order.Status = "Canceled";
                await _db.SaveChangesAsync();
                TempData["Error"] = $"Thanh toán thất bại. Đơn hàng #{order.Id} đã bị hủy.";
                return RedirectToAction("Details", "Orders", new { id = order.Id });
            }
        }

        // Callback URL
        [HttpGet]
        public async Task<IActionResult> Callback(int orderId, string? result = null)
        {
            var order = await _db.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("Index", "Cart");
            }

            // Nếu có result trong query string, xử lý luôn
            if (!string.IsNullOrEmpty(result))
            {
                if (result == "success" && order.Status == "Pending")
                {
                    var orderUser = await _userMgr.FindByIdAsync(order.UserId);
                    if (orderUser != null && !await _userMgr.IsInRoleAsync(orderUser, "Admin"))
                    {
                        await ProcessSuccessfulPayment(order);
                        TempData["Success"] = $"Thanh toán thành công! Đơn hàng #{order.Id} đã được xác nhận.";
                    }
                }
                else if (result == "failed" && order.Status == "Pending")
                {
                    order.Status = "Canceled";
                    await _db.SaveChangesAsync();
                    TempData["Error"] = $"Thanh toán thất bại. Đơn hàng #{order.Id} đã bị hủy.";
                }
            }

            return RedirectToAction("Details", "Orders", new { id = order.Id });
        }

        // Hàm xử lý thanh toán thành công
        private async Task ProcessSuccessfulPayment(Order order)
        {
            order.Status = "Confirmed";

            // Trừ tồn kho
            foreach (var item in order.Items)
            {
                var book = await _db.Books.FindAsync(item.BookId);
                if (book != null)
                {
                    if (book.Stock < item.Quantity)
                    {
                        order.Status = "Canceled";
                        await _db.SaveChangesAsync();
                        return;
                    }
                    book.Stock -= item.Quantity;
                }
            }

            await _db.SaveChangesAsync();

            // Xóa các item đã thanh toán khỏi giỏ hàng
            var orderedBookIds = order.Items.Select(oi => oi.BookId).ToList();
            foreach (var bookId in orderedBookIds)
            {
                await _cart.RemoveAsync(bookId);
            }

            // Tạo thông báo cho user
            var user = await _userMgr.FindByIdAsync(order.UserId);
            if (user != null)
            {
                await _notificationService.CreateNotificationAsync(
                    user.Id,
                    "Thanh toán thành công",
                    $"Đơn hàng #{order.Id} đã được thanh toán thành công qua QKT Payment. Tổng tiền: {order.TotalAmount:N0} ₫",
                    "Success",
                    $"/Orders/Details/{order.Id}"
                );

                // Tạo thông báo cho tất cả admin về đơn hàng mới
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
            }
        }
    }
}

