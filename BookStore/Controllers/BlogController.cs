using BookStore.Data;
using BookStore.Models.ViewModels; // 🟢 1. THÊM USING NÀY
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq; // 🟢 2. THÊM USING NÀY
using System.Threading.Tasks;

namespace BookStore.Controllers
{
    public class BlogController : Controller
    {
        private readonly ApplicationDbContext _db;

        public BlogController(ApplicationDbContext db)
        {
            _db = db;
        }

        // 🟢 3. SỬA LẠI ACTION INDEX
        // GET: /Blog
        public async Task<IActionResult> Index(int page = 1)
        {
            const int PageSize = 6; // Đặt số lượng bài viết mỗi trang (ví dụ: 6)

            var query = _db.BlogPosts
                .AsNoTracking()
                .Include(p => p.User)
                .OrderByDescending(p => p.CreatedAt);

            var totalPosts = await query.CountAsync();
            var posts = await query
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            var vm = new BlogListVM
            {
                Posts = posts,
                Page = page,
                TotalPages = (int)Math.Ceiling(totalPosts / (double)PageSize)
            };

            return View(vm);
        }

        // GET: /Blog/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var post = await _db.BlogPosts
                .AsNoTracking()
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null) return NotFound();

            return View(post);
        }
    }
}