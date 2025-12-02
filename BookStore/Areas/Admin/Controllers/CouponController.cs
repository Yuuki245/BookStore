using BookStore.Data;
using BookStore.Helpers;
using BookStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CouponController : Controller
{
    private readonly ApplicationDbContext _db;

    public CouponController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET: Admin/Coupon
    public async Task<IActionResult> Index()
    {
        var coupons = await _db.Coupons
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return View(coupons);
    }

    // GET: Admin/Coupon/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Admin/Coupon/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Coupon coupon)
    {
        if (ModelState.IsValid)
        {
            // Kiểm tra code đã tồn tại chưa
            if (await _db.Coupons.AnyAsync(c => c.Code == coupon.Code))
            {
                ModelState.AddModelError("Code", "Mã giảm giá này đã tồn tại.");
                return View(coupon);
            }

            // Validate dates
            if (coupon.StartDate >= coupon.EndDate)
            {
                ModelState.AddModelError("EndDate", "Ngày kết thúc phải sau ngày bắt đầu.");
                return View(coupon);
            }

            // Validate discount value
            if (coupon.DiscountType == "Percentage" && coupon.DiscountValue > 100)
            {
                ModelState.AddModelError("DiscountValue", "Phần trăm giảm giá không được vượt quá 100%.");
                return View(coupon);
            }

            // Convert VN time sang UTC trước khi lưu
            coupon.StartDate = TimeHelper.ToUtcTime(coupon.StartDate);
            coupon.EndDate = TimeHelper.ToUtcTime(coupon.EndDate);
            coupon.CreatedAt = DateTime.UtcNow;
            _db.Coupons.Add(coupon);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Đã tạo mã giảm giá thành công!";
            return RedirectToAction(nameof(Index));
        }

        return View(coupon);
    }

    // GET: Admin/Coupon/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var coupon = await _db.Coupons.FindAsync(id);
        if (coupon == null) return NotFound();

        return View(coupon);
    }

    // POST: Admin/Coupon/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Coupon coupon)
    {
        if (id != coupon.Id) return NotFound();

        if (ModelState.IsValid)
        {
            // Kiểm tra code đã tồn tại chưa (trừ chính nó)
            if (await _db.Coupons.AnyAsync(c => c.Code == coupon.Code && c.Id != id))
            {
                ModelState.AddModelError("Code", "Mã giảm giá này đã tồn tại.");
                return View(coupon);
            }

            // Validate dates
            if (coupon.StartDate >= coupon.EndDate)
            {
                ModelState.AddModelError("EndDate", "Ngày kết thúc phải sau ngày bắt đầu.");
                return View(coupon);
            }

            // Validate discount value
            if (coupon.DiscountType == "Percentage" && coupon.DiscountValue > 100)
            {
                ModelState.AddModelError("DiscountValue", "Phần trăm giảm giá không được vượt quá 100%.");
                return View(coupon);
            }

            try
            {
                var existingCoupon = await _db.Coupons.FindAsync(id);
                if (existingCoupon == null) return NotFound();

                existingCoupon.Code = coupon.Code;
                existingCoupon.DiscountType = coupon.DiscountType;
                existingCoupon.DiscountValue = coupon.DiscountValue;
                existingCoupon.MinOrderAmount = coupon.MinOrderAmount;
                existingCoupon.MaxDiscount = coupon.MaxDiscount;
                existingCoupon.UsageLimit = coupon.UsageLimit;
                existingCoupon.MaxUsagePerUser = coupon.MaxUsagePerUser;
                // Convert VN time sang UTC trước khi lưu
                existingCoupon.StartDate = TimeHelper.ToUtcTime(coupon.StartDate);
                existingCoupon.EndDate = TimeHelper.ToUtcTime(coupon.EndDate);
                existingCoupon.IsActive = coupon.IsActive;
                existingCoupon.Description = coupon.Description;
                // Không cập nhật UsedCount và CreatedAt

                await _db.SaveChangesAsync();

                TempData["Success"] = "Đã cập nhật mã giảm giá thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await CouponExists(coupon.Id))
                    return NotFound();
                throw;
            }
        }

        return View(coupon);
    }

    // POST: Admin/Coupon/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var coupon = await _db.Coupons.FindAsync(id);
        if (coupon != null)
        {
            _db.Coupons.Remove(coupon);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã xóa mã giảm giá thành công!";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> CouponExists(int id)
    {
        return await _db.Coupons.AnyAsync(e => e.Id == id);
    }
}

