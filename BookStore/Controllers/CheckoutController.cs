using BookStore.Data;
using BookStore.Models;
using BookStore.Services;
using BookStore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Controllers;

[Authorize]
public class CheckoutController : Controller
{
    private readonly ICartService _cart;
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userMgr;

    public CheckoutController(ICartService cart, ApplicationDbContext db, UserManager<IdentityUser> userMgr)
    {
        _cart = cart; _db = db; _userMgr = userMgr;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var items = await _cart.GetItemsAsync();
        if (!items.Any()) return RedirectToAction("Index", "Cart");
        return View(new CheckoutVM());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(CheckoutVM vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var items = await _cart.GetItemsAsync();
        if (!items.Any())
        {
            ModelState.AddModelError(string.Empty, "Giỏ hàng trống.");
            return View(vm);
        }

        var user = await _userMgr.GetUserAsync(User);
        if (user == null) return Challenge();

        // kiểm tra tồn kho
        var ids = items.Select(i => i.BookId).ToList();
        var books = await _db.Books.Where(b => ids.Contains(b.Id)).ToListAsync();
        foreach (var it in items)
        {
            var b = books.First(x => x.Id == it.BookId);
            if (b.Stock < it.Quantity)
            {
                ModelState.AddModelError(string.Empty, $"Sách '{b.Title}' không đủ tồn kho.");
                return View(vm);
            }
        }

        // tạo order
        var order = new Order
        {
            UserId = user.Id,
            ShippingAddress = vm.ShippingAddress,
            PhoneNumber = vm.PhoneNumber,
            CreatedAt = DateTime.UtcNow,
            Status = "Pending"
        };
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        // tạo order items + trừ tồn
        decimal total = 0;
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
            b.Stock -= it.Quantity;
            total += b.Price * it.Quantity;
        }
        order.TotalAmount = total;
        await _db.SaveChangesAsync();

        await _cart.ClearAsync();
        TempData["CheckoutSuccess"] = $"Đặt hàng thành công! Mã đơn: #{order.Id}";
        return RedirectToAction("Index", "Books");
    }
}
