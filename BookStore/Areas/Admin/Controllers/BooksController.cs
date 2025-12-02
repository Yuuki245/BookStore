using BookStore.Data;
using BookStore.Models;
using BookStore.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting; // ✅ để dùng IWebHostEnvironment
using System.IO;

namespace BookStore.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class BooksController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;

    // ✅ CHỈ 1 constructor duy nhất
    public BooksController(ApplicationDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    // Lưu ảnh bìa
    private string? SaveCover(IFormFile? file)
    {
        if (file == null || file.Length == 0) return null;

        // đảm bảo thư mục tồn tại
        var uploadRoot = Path.Combine(_env.WebRootPath, "uploads");
        if (!Directory.Exists(uploadRoot))
            Directory.CreateDirectory(uploadRoot);

        var ext = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid()}{ext}";
        var savePath = Path.Combine(uploadRoot, fileName);

        using var fs = new FileStream(savePath, FileMode.Create);
        file.CopyTo(fs);

        return $"/uploads/{fileName}";
    }

    // ====== Index ======
    public async Task<IActionResult> Index(string? q, int? categoryId, string? sort = "new", int page = 1)
    {
        const int PageSize = 9;

        var query = _db.Books
            .Include(b => b.Category)
            .AsNoTracking();

        // 🔸 Lọc theo từ khóa
        if (!string.IsNullOrWhiteSpace(q))
        {
            var keyword = q.Trim().ToLower();
            query = query.Where(b => b.Title.ToLower().Contains(keyword) || 
                                     (b.Author != null && b.Author.ToLower().Contains(keyword)));
        }

        // 🔸 Lọc theo thể loại
        if (categoryId.HasValue)
            query = query.Where(b => b.CategoryId == categoryId.Value);

        // 🔸 Sắp xếp
        query = sort switch
        {
            "title_asc" => query.OrderBy(b => b.Title),
            "title_desc" => query.OrderByDescending(b => b.Title),
            "price_asc" => query.OrderBy(b => b.Price),
            "price_desc" => query.OrderByDescending(b => b.Price),
            _ => query.OrderByDescending(b => b.Id)
        };

        // 🔸 Phân trang
        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();

        // 🔸 Gửi dữ liệu ra View
        ViewBag.Q = q;
        ViewBag.Sort = sort;
        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)PageSize);
        ViewBag.CategoryId = categoryId;

        // 🔸 Danh sách thể loại để hiển thị dropdown
        ViewBag.Categories = await _db.Categories.AsNoTracking().ToListAsync();

        return View(items);
    }


    // ====== Create ======
    public async Task<IActionResult> Create()
    {
        ViewBag.CategoryId = new SelectList(await _db.Categories.ToListAsync(), "Id", "Name");
        return View(new BookUpsertVM());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BookUpsertVM vm)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.CategoryId = new SelectList(await _db.Categories.ToListAsync(), "Id", "Name", vm.Book.CategoryId);
            return View(vm);
        }

        var url = SaveCover(vm.CoverFile);
        if (url != null) vm.Book.CoverUrl = url;

        _db.Books.Add(vm.Book);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã thêm sách.";
        return RedirectToAction(nameof(Index));
    }

    // ====== Edit ======
    public async Task<IActionResult> Edit(int id)
    {
        var book = await _db.Books.FindAsync(id);
        if (book == null) return NotFound();
        ViewBag.CategoryId = new SelectList(await _db.Categories.ToListAsync(), "Id", "Name", book.CategoryId);
        return View(new BookUpsertVM { Book = book });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BookUpsertVM vm)
    {
        if (id != vm.Book.Id) return BadRequest();
        if (!ModelState.IsValid)
        {
            ViewBag.CategoryId = new SelectList(await _db.Categories.ToListAsync(), "Id", "Name", vm.Book.CategoryId);
            return View(vm);
        }

        var book = await _db.Books.FindAsync(id);
        if (book == null) return NotFound();

        book.Title = vm.Book.Title;
        book.Author = vm.Book.Author;
        book.Price = vm.Book.Price;
        book.Stock = vm.Book.Stock;
        book.Isbn = vm.Book.Isbn;
        book.Description = vm.Book.Description;
        book.CategoryId = vm.Book.CategoryId;

        var url = SaveCover(vm.CoverFile);
        if (url != null) book.CoverUrl = url;

        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật sách.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var book = await _db.Books.FindAsync(id);
        if (book != null)
        {
            _db.Books.Remove(book);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã xoá sách.";
        }
        return RedirectToAction(nameof(Index));
    }
}
