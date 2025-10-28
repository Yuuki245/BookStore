using BookStore.Data;
using BookStore.Services;
using BookStore.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.Controllers;

public class CartController : Controller
{
    private readonly ICartService _cart;
    private readonly ApplicationDbContext _db;
    public CartController(ICartService cart, ApplicationDbContext db)
    { _cart = cart; _db = db; }

    public async Task<IActionResult> Index()
    {
        var items = await _cart.GetItemsAsync();
        return View(new CartVM { Items = items });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int id, int qty = 1)
    {
        var book = await _db.Books.FindAsync(id);
        if (book == null) return NotFound();
        await _cart.AddAsync(book, qty);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, int qty)
    {
        await _cart.UpdateAsync(id, qty);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int id)
    {
        await _cart.RemoveAsync(id);
        return RedirectToAction(nameof(Index));
    }
}
