using BookStore.Data;
using BookStore.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class BooksController : Controller
{
    private readonly ApplicationDbContext _db;
    private const int PageSize = 8;

    public BooksController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index(int? categoryId, string? search, string? sort, int page = 1)
    {
        var q = _db.Books.Include(b => b.Category).AsQueryable();

        if (categoryId.HasValue) q = q.Where(b => b.CategoryId == categoryId.Value);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(b => b.Title.Contains(search) || b.Author.Contains(search));

        q = sort switch
        {
            "price_asc" => q.OrderBy(b => b.Price),
            "price_desc" => q.OrderByDescending(b => b.Price),
            "title_asc" => q.OrderBy(b => b.Title),
            "title_desc" => q.OrderByDescending(b => b.Title),
            _ => q.OrderByDescending(b => b.Id)
        };

        var total = await q.CountAsync();
        var items = await q.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();

        var vm = new BookListVM
        {
            Books = items,
            Categories = await _db.Categories.AsNoTracking().ToListAsync(),
            CategoryId = categoryId,
            Search = search,
            Sort = sort,
            Page = page,
            TotalPages = (int)Math.Ceiling(total / (double)PageSize)
        };
        return View(vm);
    }

    public async Task<IActionResult> Details(int id)
    {
        var book = await _db.Books.Include(b => b.Category).FirstOrDefaultAsync(b => b.Id == id);
        return book == null ? NotFound() : View(book);
    }
}
