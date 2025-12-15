using BookStore.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BookStore.Areas.Identity.Pages.Account.Manage
{
    [Authorize]
    public class PointsModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public PointsModel(ApplicationDbContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public IList<BookStore.Models.PointTransaction> Transactions { get; set; } = new List<BookStore.Models.PointTransaction>();
        public int TotalPoints { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;

        public async Task<IActionResult> OnGetAsync(int? p = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            const int PageSize = 20;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            // Lấy page từ query string (dùng "p" thay vì "page" để tránh conflict với Razor Pages)
            var pageFromQuery = Request.Query["p"].ToString();
            var currentPage = 1;
            if (!string.IsNullOrEmpty(pageFromQuery) && int.TryParse(pageFromQuery, out var parsedPage))
            {
                currentPage = parsedPage;
            }
            else if (p.HasValue)
            {
                currentPage = p.Value;
            }
            
            if (currentPage < 1) currentPage = 1;

            // Tính tổng điểm hiện có
            TotalPoints = await _db.PointTransactions
                .Where(pt => pt.UserId == userId)
                .SumAsync(pt => pt.Points);

            // Lấy lịch sử giao dịch điểm
            var query = _db.PointTransactions
                .AsNoTracking()
                .Where(pt => pt.UserId == userId)
                .OrderByDescending(pt => pt.CreatedAt);

            var total = await query.CountAsync();
            Transactions = await query
                .Skip((currentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            CurrentPage = currentPage;
            TotalPages = (int)Math.Ceiling(total / (double)PageSize);
            ViewData["ActivePage"] = ManageNavPages.Points;

            return Page();
        }
    }
}

