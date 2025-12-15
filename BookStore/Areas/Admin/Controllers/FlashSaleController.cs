using BookStore.Data;
using BookStore.Helpers;
using BookStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class FlashSaleController : Controller
{
    private readonly ApplicationDbContext _db;

    public FlashSaleController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET: Admin/FlashSale
    public async Task<IActionResult> Index()
    {
        var flashSales = await _db.FlashSales
            .AsNoTracking()
            .Include(f => f.Books)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();

        // Kiểm tra xem có flash sale nào đang active hoặc sắp diễn ra không
        var now = TimeHelper.GetVietnamTime();
        var hasActiveFlashSale = false;
        string? activeFlashSaleInfo = null;

        foreach (var sale in flashSales.Where(f => f.IsActive))
        {
            var startTime = TimeHelper.ToVietnamTime(sale.StartTime);
            var endTime = TimeHelper.ToVietnamTime(sale.EndTime);
            
            bool isCurrentlyActive = now >= startTime && now <= endTime;
            bool isUpcoming = startTime > now;
            
            if (isCurrentlyActive || isUpcoming)
            {
                hasActiveFlashSale = true;
                string statusText = isCurrentlyActive ? "đang hoạt động" : "sắp diễn ra";
                activeFlashSaleInfo = $"Flash sale '{sale.Title}' {statusText} (từ {startTime:dd/MM/yyyy HH:mm} đến {endTime:dd/MM/yyyy HH:mm})";
                break; // Chỉ cần thông tin của flash sale đầu tiên
            }
        }

        ViewBag.HasActiveFlashSale = hasActiveFlashSale;
        ViewBag.ActiveFlashSaleInfo = activeFlashSaleInfo;

        return View(flashSales);
    }

    // GET: Admin/FlashSale/Create
    public async Task<IActionResult> Create()
    {
        // Kiểm tra xem có flash sale nào đang active hoặc sắp diễn ra không
        var now = TimeHelper.GetVietnamTime();
        var existingFlashSales = await _db.FlashSales
            .AsNoTracking()
            .Where(f => f.IsActive)
            .ToListAsync();
        
        // Kiểm tra xem có flash sale nào đang trong khoảng thời gian hoạt động hoặc sắp diễn ra không
        foreach (var existing in existingFlashSales)
        {
            var existingStartTime = TimeHelper.ToVietnamTime(existing.StartTime);
            var existingEndTime = TimeHelper.ToVietnamTime(existing.EndTime);
            
            // Kiểm tra xem flash sale cũ có đang diễn ra hoặc sắp diễn ra không
            bool isCurrentlyActive = now >= existingStartTime && now <= existingEndTime;
            bool isUpcoming = existingStartTime > now; // Chưa bắt đầu nhưng đã được tạo
            
            if (isCurrentlyActive || isUpcoming)
            {
                // Có flash sale đang active hoặc sắp diễn ra, không cho tạo mới
                string statusText = isCurrentlyActive ? "đang hoạt động" : "sắp diễn ra";
                
                TempData["Error"] = 
                    $"Đã có flash sale {statusText} (từ {existingStartTime:dd/MM/yyyy HH:mm} đến {existingEndTime:dd/MM/yyyy HH:mm}). " +
                    $"Vui lòng đợi flash sale này kết thúc hoặc xóa nó trước khi tạo flash sale mới.";
                
                return RedirectToAction(nameof(Index));
            }
        }

        // Chỉ lấy sách không đang sale và không đang trong flash sale khác
        ViewBag.Books = await _db.Books
            .AsNoTracking()
            .Where(b => (b.OriginalPrice == null || b.OriginalPrice == 0) && b.FlashSaleId == null)
            .Select(b => new SelectListItem
            {
                Value = b.Id.ToString(),
                Text = $"{b.Title} - {b.Price:N0} ₫"
            })
            .ToListAsync();

        return View();
    }

    // POST: Admin/FlashSale/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FlashSale flashSale, int[] selectedBookIds)
    {
        if (ModelState.IsValid)
        {
            // Xử lý timezone: datetime-local gửi VN time, convert sang UTC để lưu
            // Giả sử input từ form là VN time (GMT+7)
            var startTimeVN = flashSale.StartTime.Kind == DateTimeKind.Utc 
                ? BookStore.Helpers.TimeHelper.ToVietnamTime(flashSale.StartTime)
                : flashSale.StartTime;
            var endTimeVN = flashSale.EndTime.Kind == DateTimeKind.Utc 
                ? BookStore.Helpers.TimeHelper.ToVietnamTime(flashSale.EndTime)
                : flashSale.EndTime;

            if (startTimeVN >= endTimeVN)
            {
                ModelState.AddModelError("EndTime", "Thời gian kết thúc phải sau thời gian bắt đầu");
                ViewBag.Books = await _db.Books
                    .AsNoTracking()
                    .Select(b => new SelectListItem
                    {
                        Value = b.Id.ToString(),
                        Text = $"{b.Title} - {b.Price:N0} ₫"
                    })
                    .ToListAsync();
                return View(flashSale);
            }

            // Kiểm tra xem có flash sale nào đang active hoặc sắp diễn ra không (chỉ cho phép 1 flash sale)
            var now = BookStore.Helpers.TimeHelper.GetVietnamTime();
            var existingFlashSales = await _db.FlashSales
                .Where(f => f.IsActive)
                .ToListAsync();
            
            // Kiểm tra xem có flash sale nào đang trong khoảng thời gian hoạt động hoặc sắp diễn ra không
            foreach (var existing in existingFlashSales)
            {
                var existingStartTime = BookStore.Helpers.TimeHelper.ToVietnamTime(existing.StartTime);
                var existingEndTime = BookStore.Helpers.TimeHelper.ToVietnamTime(existing.EndTime);
                
                // Kiểm tra xem flash sale cũ có đang diễn ra hoặc sắp diễn ra không
                bool isCurrentlyActive = now >= existingStartTime && now <= existingEndTime;
                bool isUpcoming = existingStartTime > now; // Chưa bắt đầu nhưng đã được tạo
                
                // Kiểm tra xem flash sale mới có overlap với flash sale cũ không
                bool hasOverlap = (startTimeVN >= existingStartTime && startTimeVN <= existingEndTime) ||
                                  (endTimeVN >= existingStartTime && endTimeVN <= existingEndTime) ||
                                  (startTimeVN <= existingStartTime && endTimeVN >= existingEndTime);
                
                if (isCurrentlyActive || isUpcoming || hasOverlap)
                {
                    // Có flash sale đang active, sắp diễn ra, hoặc overlap, không cho tạo mới
                    string statusText = isCurrentlyActive ? "đang hoạt động" : 
                                       isUpcoming ? "sắp diễn ra" : 
                                       "có thời gian trùng lặp";
                    
                    ModelState.AddModelError(string.Empty, 
                        $"Đã có flash sale {statusText} (từ {existingStartTime:dd/MM/yyyy HH:mm} đến {existingEndTime:dd/MM/yyyy HH:mm}). " +
                        $"Vui lòng đợi flash sale này kết thúc hoặc xóa nó trước khi tạo flash sale mới.");
                    
                    ViewBag.Books = await _db.Books
                        .AsNoTracking()
                        .Where(b => (b.OriginalPrice == null || b.OriginalPrice == 0) && b.FlashSaleId == null)
                        .Select(b => new SelectListItem
                        {
                            Value = b.Id.ToString(),
                            Text = $"{b.Title} - {b.Price:N0} ₫"
                        })
                        .ToListAsync();
                    return View(flashSale);
                }
            }

            // Lưu dưới dạng UTC (giả sử input là VN time)
            flashSale.StartTime = BookStore.Helpers.TimeHelper.ToUtcTime(startTimeVN);
            flashSale.EndTime = BookStore.Helpers.TimeHelper.ToUtcTime(endTimeVN);
            flashSale.CreatedAt = DateTime.UtcNow;

            if (selectedBookIds != null && selectedBookIds.Length > 0)
            {
                var books = await _db.Books
                    .Where(b => selectedBookIds.Contains(b.Id))
                    .ToListAsync();

                foreach (var book in books)
                {
                    book.FlashSaleId = flashSale.Id; // Sẽ được set sau khi flashSale được save
                }

                flashSale.Books = books;
            }

            _db.FlashSales.Add(flashSale);
            await _db.SaveChangesAsync();

            // Cập nhật FlashSaleId cho các books
            if (selectedBookIds != null && selectedBookIds.Length > 0)
            {
                var books = await _db.Books
                    .Where(b => selectedBookIds.Contains(b.Id))
                    .ToListAsync();

                foreach (var book in books)
                {
                    book.FlashSaleId = flashSale.Id;
                }

                await _db.SaveChangesAsync();
            }

            TempData["Success"] = "Đã tạo flash sale thành công!";
            return RedirectToAction(nameof(Index));
        }

        // Chỉ lấy sách không đang sale và không đang trong flash sale khác
        ViewBag.Books = await _db.Books
            .AsNoTracking()
            .Where(b => (b.OriginalPrice == null || b.OriginalPrice == 0) && b.FlashSaleId == null)
            .Select(b => new SelectListItem
            {
                Value = b.Id.ToString(),
                Text = $"{b.Title} - {b.Price:N0} ₫"
            })
            .ToListAsync();

        return View(flashSale);
    }

    // GET: Admin/FlashSale/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var flashSale = await _db.FlashSales
            .Include(f => f.Books)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (flashSale == null) return NotFound();

        // Đảm bảo StartTime và EndTime là local time khi hiển thị trong form
        if (flashSale.StartTime.Kind == DateTimeKind.Utc)
        {
            flashSale.StartTime = flashSale.StartTime.ToLocalTime();
        }
        else if (flashSale.StartTime.Kind == DateTimeKind.Unspecified)
        {
            flashSale.StartTime = DateTime.SpecifyKind(flashSale.StartTime, DateTimeKind.Local);
        }

        if (flashSale.EndTime.Kind == DateTimeKind.Utc)
        {
            flashSale.EndTime = flashSale.EndTime.ToLocalTime();
        }
        else if (flashSale.EndTime.Kind == DateTimeKind.Unspecified)
        {
            flashSale.EndTime = DateTime.SpecifyKind(flashSale.EndTime, DateTimeKind.Local);
        }

        // Load book IDs đã được chọn trước
        var selectedBookIds = flashSale.Books.Select(b => b.Id).ToList();

        // Chỉ lấy sách không đang sale và không đang trong flash sale khác (trừ flash sale hiện tại)
        var allBooks = await _db.Books
            .AsNoTracking()
            .Where(b => (b.OriginalPrice == null || b.OriginalPrice == 0) && 
                       (b.FlashSaleId == null || b.FlashSaleId == id))
            .ToListAsync();

        ViewBag.Books = allBooks.Select(b => new SelectListItem
        {
            Value = b.Id.ToString(),
            Text = $"{b.Title} - {b.Price:N0} ₫",
            Selected = selectedBookIds.Contains(b.Id)
        }).ToList();

        // Kiểm tra xem flash sale đã bắt đầu chưa
        var now = DateTime.Now;
        var originalStartTime = flashSale.StartTime;
        if (originalStartTime.Kind == DateTimeKind.Utc)
        {
            originalStartTime = originalStartTime.ToLocalTime();
        }
        ViewBag.HasStarted = now >= originalStartTime;

        return View(flashSale);
    }

    // POST: Admin/FlashSale/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, FlashSale flashSale, int[] selectedBookIds)
    {
        if (id != flashSale.Id) return NotFound();

        if (ModelState.IsValid)
        {
            // Xử lý timezone: datetime-local gửi VN time, convert sang UTC để lưu
            var startTimeVN = flashSale.StartTime.Kind == DateTimeKind.Utc 
                ? TimeHelper.ToVietnamTime(flashSale.StartTime)
                : flashSale.StartTime;
            var endTimeVN = flashSale.EndTime.Kind == DateTimeKind.Utc 
                ? TimeHelper.ToVietnamTime(flashSale.EndTime)
                : flashSale.EndTime;

            if (startTimeVN >= endTimeVN)
            {
                ModelState.AddModelError("EndTime", "Thời gian kết thúc phải sau thời gian bắt đầu");
                var existingSelectedBookIds = await _db.FlashSales
                    .Where(f => f.Id == id)
                    .SelectMany(f => f.Books.Select(b => b.Id))
                    .ToListAsync();
                
                var allBooks = await _db.Books
                    .AsNoTracking()
                    .Where(b => (b.OriginalPrice == null || b.OriginalPrice == 0) && 
                               (b.FlashSaleId == null || b.FlashSaleId == id))
                    .ToListAsync();
                
                ViewBag.Books = allBooks.Select(b => new SelectListItem
                {
                    Value = b.Id.ToString(),
                    Text = $"{b.Title} - {b.Price:N0} ₫",
                    Selected = existingSelectedBookIds.Contains(b.Id)
                }).ToList();
                return View(flashSale);
            }

            try
            {
                var existingFlashSale = await _db.FlashSales
                    .Include(f => f.Books)
                    .FirstOrDefaultAsync(f => f.Id == id);

                if (existingFlashSale == null) return NotFound();

                // Kiểm tra xem flash sale đã bắt đầu chưa (dùng VN time)
                var now = TimeHelper.GetVietnamTime();
                var originalStartTime = TimeHelper.ToVietnamTime(existingFlashSale.StartTime);
                bool hasStarted = now >= originalStartTime;

                // Xóa quan hệ với các sách cũ
                foreach (var book in existingFlashSale.Books.ToList())
                {
                    book.FlashSaleId = null;
                }

                // Cập nhật thông tin flash sale
                existingFlashSale.Title = flashSale.Title;
                existingFlashSale.Description = flashSale.Description;
                
                // Nếu flash sale đã bắt đầu, không cho phép thay đổi StartTime
                if (hasStarted)
                {
                    // Giữ nguyên StartTime từ database
                    // Không cập nhật StartTime
                }
                else
                {
                    // Chưa bắt đầu, cho phép thay đổi - lưu dưới dạng UTC
                    existingFlashSale.StartTime = TimeHelper.ToUtcTime(startTimeVN);
                }
                
                // Luôn cho phép cập nhật EndTime - lưu dưới dạng UTC
                existingFlashSale.EndTime = TimeHelper.ToUtcTime(endTimeVN);
                
                // Nếu flash sale này được set active, tắt tất cả flash sale khác đang active
                if (flashSale.IsActive)
                {
                    var otherActiveFlashSales = await _db.FlashSales
                        .Where(f => f.IsActive && f.Id != id)
                        .ToListAsync();
                    
                    foreach (var other in otherActiveFlashSales)
                    {
                        var otherStartTime = TimeHelper.ToVietnamTime(other.StartTime);
                        var otherEndTime = TimeHelper.ToVietnamTime(other.EndTime);
                        
                        // Nếu flash sale khác đang trong khoảng thời gian hoạt động, tắt nó
                        if (now >= otherStartTime && now <= otherEndTime)
                        {
                            other.IsActive = false;
                        }
                    }
                }
                existingFlashSale.DiscountPercent = flashSale.DiscountPercent;
                existingFlashSale.IsActive = flashSale.IsActive;
                existingFlashSale.MaxQuantityPerUser = flashSale.MaxQuantityPerUser;

                // Thêm quan hệ với các sách mới
                if (selectedBookIds != null && selectedBookIds.Length > 0)
                {
                    var books = await _db.Books
                        .Where(b => selectedBookIds.Contains(b.Id))
                        .ToListAsync();

                    foreach (var book in books)
                    {
                        book.FlashSaleId = id;
                    }
                }

                await _db.SaveChangesAsync();

                TempData["Success"] = "Đã cập nhật flash sale thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await FlashSaleExists(flashSale.Id))
                    return NotFound();
                throw;
            }
        }

        // Load book IDs đã được chọn trước
        var currentSelectedBookIds = await _db.FlashSales
            .Where(f => f.Id == id)
            .SelectMany(f => f.Books.Select(b => b.Id))
            .ToListAsync();
        
        // Chỉ lấy sách không đang sale và không đang trong flash sale khác (trừ flash sale hiện tại)
        var availableBooks = await _db.Books
            .AsNoTracking()
            .Where(b => (b.OriginalPrice == null || b.OriginalPrice == 0) && 
                       (b.FlashSaleId == null || b.FlashSaleId == id))
            .ToListAsync();
        
        ViewBag.Books = availableBooks.Select(b => new SelectListItem
        {
            Value = b.Id.ToString(),
            Text = $"{b.Title} - {b.Price:N0} ₫",
            Selected = currentSelectedBookIds.Contains(b.Id)
        }).ToList();

        return View(flashSale);
    }

    // POST: Admin/FlashSale/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var flashSale = await _db.FlashSales
                .Include(f => f.Books)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (flashSale == null)
            {
                TempData["Error"] = "Không tìm thấy flash sale để xóa.";
                return RedirectToAction(nameof(Index));
            }

            // Xóa quan hệ với các sách - set FlashSaleId = null
            var books = flashSale.Books.ToList();
            foreach (var book in books)
            {
                book.FlashSaleId = null;
            }

            // Lưu thay đổi để cập nhật FlashSaleId của các books
            await _db.SaveChangesAsync();

            // Sau đó mới xóa flash sale
            _db.FlashSales.Remove(flashSale);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Đã xóa flash sale thành công!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Có lỗi xảy ra khi xóa flash sale: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    private async Task<bool> FlashSaleExists(int id)
    {
        return await _db.FlashSales.AnyAsync(e => e.Id == id);
    }
}

