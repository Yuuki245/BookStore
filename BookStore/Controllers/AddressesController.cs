using BookStore.Data;
using BookStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BookStore.Controllers;

[Authorize]
public class AddressesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userMgr;

    public AddressesController(ApplicationDbContext db, UserManager<IdentityUser> userMgr)
    {
        _db = db;
        _userMgr = userMgr;
    }

    // GET: /Addresses
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var addresses = await _db.Addresses
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync();

        ViewData["ActivePage"] = "Addresses";
        return View(addresses);
    }

    // GET: /Addresses/Create
    public IActionResult Create()
    {
        ViewData["ActivePage"] = "Addresses";
        return View();
    }

    // POST: /Addresses/Create
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Address address)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // Set UserId trước khi validate để tránh lỗi "UserId is required"
        address.UserId = userId;
        
        // Xóa lỗi UserId khỏi ModelState vì nó được set tự động
        ModelState.Remove("UserId");

        if (ModelState.IsValid)
        {
            address.CreatedAt = DateTime.UtcNow;

            // Nếu đặt làm mặc định, bỏ mặc định của các địa chỉ khác
            if (address.IsDefault)
            {
                await _db.Addresses
                    .Where(a => a.UserId == userId && a.IsDefault)
                    .ExecuteUpdateAsync(a => a.SetProperty(x => x.IsDefault, false));
            }

            _db.Addresses.Add(address);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Đã thêm địa chỉ mới.";
            // Redirect về Manage/Addresses nếu có returnUrl, ngược lại về Index
            var returnUrl = Request.Query["returnUrl"].ToString();
            if (!string.IsNullOrEmpty(returnUrl) && returnUrl.Contains("/Identity/Account/Manage"))
            {
                return RedirectToPage("/Identity/Account/Manage/Addresses");
            }
            return RedirectToAction(nameof(Index));
        }

        return View(address);
    }

    // GET: /Addresses/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var address = await _db.Addresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
        if (address == null)
            return NotFound();

        ViewData["ActivePage"] = "Addresses";
        return View(address);
    }

    // POST: /Addresses/Edit/5
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Address address)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        if (id != address.Id)
            return NotFound();

        var existingAddress = await _db.Addresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
        if (existingAddress == null)
            return NotFound();

        if (ModelState.IsValid)
        {
            // Nếu đặt làm mặc định, bỏ mặc định của các địa chỉ khác
            if (address.IsDefault && !existingAddress.IsDefault)
            {
                await _db.Addresses
                    .Where(a => a.UserId == userId && a.IsDefault && a.Id != id)
                    .ExecuteUpdateAsync(a => a.SetProperty(x => x.IsDefault, false));
            }

            existingAddress.PhoneNumber = address.PhoneNumber;
            existingAddress.StreetAddress = address.StreetAddress;
            existingAddress.IsDefault = address.IsDefault;
            existingAddress.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            TempData["Success"] = "Đã cập nhật địa chỉ.";
            // Redirect về Manage/Addresses nếu có returnUrl, ngược lại về Index
            var returnUrl = Request.Query["returnUrl"].ToString();
            if (!string.IsNullOrEmpty(returnUrl) && returnUrl.Contains("/Identity/Account/Manage"))
            {
                return RedirectToPage("/Identity/Account/Manage/Addresses");
            }
            return RedirectToAction(nameof(Index));
        }

        return View(address);
    }

    // POST: /Addresses/Delete/5
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var address = await _db.Addresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
        if (address == null)
            return NotFound();

        _db.Addresses.Remove(address);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Đã xóa địa chỉ.";
        // Redirect về Manage/Addresses nếu có returnUrl, ngược lại về Index
        var returnUrl = Request.Query["returnUrl"].ToString();
        if (!string.IsNullOrEmpty(returnUrl) && returnUrl.Contains("/Identity/Account/Manage"))
        {
            return RedirectToPage("/Identity/Account/Manage/Addresses");
        }
        return RedirectToAction(nameof(Index));
    }

    // POST: /Addresses/SetDefault/5
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetDefault(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var address = await _db.Addresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
        if (address == null)
            return NotFound();

        // Bỏ mặc định của tất cả địa chỉ
        await _db.Addresses
            .Where(a => a.UserId == userId)
            .ExecuteUpdateAsync(a => a.SetProperty(x => x.IsDefault, false));

        // Đặt địa chỉ này làm mặc định
        address.IsDefault = true;
        address.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Đã đặt địa chỉ làm mặc định.";
        // Redirect về Manage/Addresses nếu có returnUrl, ngược lại về Index
        var returnUrl = Request.Query["returnUrl"].ToString();
        if (!string.IsNullOrEmpty(returnUrl) && returnUrl.Contains("/Identity/Account/Manage"))
        {
            return RedirectToPage("/Identity/Account/Manage/Addresses");
        }
        return RedirectToAction(nameof(Index));
    }

    // API: GET /Addresses/GetUserAddresses (JSON)
    [HttpGet]
    public async Task<IActionResult> GetUserAddresses()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Json(new { success = false, message = "Unauthorized" });

        var addresses = await _db.Addresses
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.CreatedAt)
            .Select(a => new
            {
                a.Id,
                a.PhoneNumber,
                a.StreetAddress,
                a.IsDefault,
                Address = a.StreetAddress
            })
            .ToListAsync();

        return Json(new { success = true, addresses });
    }
}

