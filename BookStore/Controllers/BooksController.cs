using BookStore.Data;
using BookStore.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Controllers;

public class BooksController : Controller
{
    private readonly ApplicationDbContext _db;
    public BooksController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index(int? categoryId, string? search)
    {
        var q = _db.Books.Include(b => b.Category).AsQueryable();
        if (categoryId.HasValue) q = q.Where(b => b.CategoryId == categoryId);
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(b => b.Title.Contains(search));
        var vm = new BookListVM { Books = await q.AsNoTracking().ToListAsync(), CategoryId = categoryId, Search = search };
        return View(vm);
    }

    public async Task<IActionResult> Details(int id)
    {
        var book = await _db.Books.Include(b => b.Category).FirstOrDefaultAsync(b => b.Id == id);
        if (book == null) return NotFound();
        return View(book);
    }
}
