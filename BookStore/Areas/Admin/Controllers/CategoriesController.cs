using BookStore.Data;
using BookStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CategoriesController : Controller
{
    private readonly ApplicationDbContext _db;
    public CategoriesController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index() =>
        View(await _db.Categories.AsNoTracking().ToListAsync());

    public IActionResult Create() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Category model)
    {
        if (!ModelState.IsValid) return View(model);
        _db.Categories.Add(model);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await _db.Categories.FindAsync(id);
        return item == null ? NotFound() : View(item);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Category model)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid) return View(model);
        _db.Update(model);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var cat = await _db.Categories.FindAsync(id);
        if (cat == null) return NotFound();

        var hasBooks = await _db.Books.AnyAsync(b => b.CategoryId == id);
        if (hasBooks)
        {
            TempData["Warning"] = "Không thể xoá thể loại vì vẫn còn sách thuộc thể loại này. Hãy chuyển sách sang thể loại khác trước.";
            return RedirectToAction(nameof(Index));
        }

        _db.Categories.Remove(cat);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã xoá thể loại.";
        return RedirectToAction(nameof(Index));
    }

}
