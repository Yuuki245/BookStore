using BookStore.Data;
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
    
    public CheckoutController(ICartService cart, ApplicationDbContext db,
                              UserManager<IdentityUser> userMgr)
    {
        _cart = cart; _db = db; _userMgr = userMgr;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var items = await _cart.GetItemsAsync();
        if (!items.Any()) return RedirectToAction("Index", "Cart");

        // 🟢 SỬA: Xóa dòng "Subtotal = ..." ra khỏi đây.
        var vm = new CheckoutVM
        {
            Items = items
            // (Bạn có thể giữ lại ShippingAddress = "" và PhoneNumber = "" nếu muốn)
        };
        return View(vm);
    }
    private async Task<Order?> CreateOrderAsync(CheckoutVM vm)
    {
        if (!ModelState.IsValid)
        {
            return null;
        }

        var items = await _cart.GetItemsAsync();
        if (!items.Any())
        {
            ModelState.AddModelError(string.Empty, "Giỏ hàng trống.");
            return null;
        }

        var user = await _userMgr.GetUserAsync(User);
        if (user == null) return null;

        // Kiểm tra tồn kho
        var ids = items.Select(i => i.BookId).ToList();
        var books = await _db.Books.Where(b => ids.Contains(b.Id)).ToListAsync();
        foreach (var it in items)
        {
            var b = books.First(x => x.Id == it.BookId);
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
            ShippingAddress = vm.ShippingAddress.Trim(),
            PhoneNumber = vm.PhoneNumber.Trim(),
            CreatedAt = DateTime.UtcNow,
            Status = "Pending" // 🟢 LUÔN LÀ PENDING
        };
        _db.Orders.Add(order);
        await _db.SaveChangesAsync(); // 🟢 Lưu để lấy OrderId

        decimal subtotal = 0;
        foreach (var it in items)
        {
            var b = books.First(x => x.Id == it.BookId);
            _db.OrderItems.Add(new OrderItem
            {
                OrderId = order.Id,
                BookId = b.Id,
                UnitPrice = b.Price,
                Quantity = it.Quantity
            });
            subtotal += b.Price * it.Quantity;
        }

        // 🟢 BẮT ĐẦU: Áp dụng logic giảm giá (Phải giống hệt logic ở View)
        decimal discountAmount = 0;
        if (subtotal >= 5000000)
        {
            discountAmount = 500000;
        }
        else if (subtotal >= 2000000)
        {
            discountAmount = 100000;
        }
        else if (subtotal >= 1000000)
        {
            discountAmount = 70000;
        }
        else if (subtotal >= 500000)
        {
            discountAmount = 50000;
        }
        else if (subtotal >= 200000)
        {
            discountAmount = 10000;
        }

        decimal finalTotal = subtotal - discountAmount;
        // 🟢 KẾT THÚC: Áp dụng logic giảm giá

        order.TotalAmount = finalTotal; // 🟢 Gán tổng tiền cuối cùng (đã giảm)
        await _db.SaveChangesAsync(); // Lưu tổng tiền

        return order;
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(CheckoutVM vm)
    {
        // 1. Tạo đơn hàng "Pending"
        var order = await CreateOrderAsync(vm);
        if (order == null)
        {
            vm.Items = await _cart.GetItemsAsync();
            return View(vm);
        }

        // 2. Xử lý logic cho COD (Trừ kho, Xóa giỏ)
        var orderItems = await _db.OrderItems.Where(oi => oi.OrderId == order.Id).ToListAsync();
        foreach (var item in orderItems)
        {
            var book = await _db.Books.FindAsync(item.BookId);
            if (book != null)
            {
                book.Stock -= item.Quantity; // Trừ kho
            }
        }

        order.Status = "Confirmed"; // 🟢 COD thì xác nhận luôn
        await _db.SaveChangesAsync();

        // 3. Xóa giỏ hàng
        await _cart.ClearAsync();
        TempData["Success"] = $"Đặt hàng COD thành công! Mã đơn: #{order.Id}";
        return RedirectToAction("Details", "Orders", new { id = order.Id });
    }

}
