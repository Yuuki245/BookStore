using BookStore.Data;
using BookStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting; // 🟢 1. THÊM USING NÀY
using System.IO; // 🟢 1. THÊM USING NÀY

namespace BookStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class BlogController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userMgr;
        private readonly IWebHostEnvironment _env; // 🟢 2. KHAI BÁO BIẾN _env

        // 🟢 3. SỬA HÀM KHỞI TẠO (Constructor)
        public BlogController(ApplicationDbContext db, UserManager<IdentityUser> userMgr, IWebHostEnvironment env)
        {
            _db = db;
            _userMgr = userMgr;
            _env = env; // Gán giá trị cho _env
        }

        // GET: /Admin/Blog
        public async Task<IActionResult> Index()
        {
            var posts = await _db.BlogPosts
                .AsNoTracking()
                .Include(p => p.User)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
            return View(posts);
        }

        // GET: /Admin/Blog/Create
        public IActionResult Create()
        {
            return View(new BlogPost());
        }

        // POST: /Admin/Blog/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BlogPost post)
        {
            if (ModelState.IsValid)
            {
                post.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                post.CreatedAt = DateTime.UtcNow;

                _db.BlogPosts.Add(post);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Đã tạo bài viết mới.";
                return RedirectToAction(nameof(Index));
            }
            return View(post);
        }

        // GET: /Admin/Blog/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var post = await _db.BlogPosts.FindAsync(id);
            if (post == null) return NotFound();
            return View(post);
        }

        // POST: /Admin/Blog/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BlogPost post)
        {
            if (id != post.Id) return BadRequest();

            if (ModelState.IsValid)
            {
                var existingPost = await _db.BlogPosts.FindAsync(id);
                if (existingPost == null) return NotFound();

                existingPost.Title = post.Title;
                existingPost.Excerpt = post.Excerpt;
                existingPost.Content = post.Content;
                existingPost.HeaderImageUrl = post.HeaderImageUrl;

                _db.Update(existingPost);
                await _db.SaveChangesAsync();

                TempData["Success"] = "Đã cập nhật bài viết.";
                return RedirectToAction(nameof(Index));
            }
            return View(post);
        }

        // POST: /Admin/Blog/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var post = await _db.BlogPosts.FindAsync(id);
            if (post != null)
            {
                _db.BlogPosts.Remove(post);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Đã xóa bài viết.";
            }
            return RedirectToAction(nameof(Index));
        }

        // ACTION XỬ LÝ TẢI ẢNH (Sử dụng _env)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("Không có tệp nào được tải lên.");
            }

            var uploadPath = Path.Combine(_env.WebRootPath, "uploads", "blog");
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            var ext = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{ext}";
            var savePath = Path.Combine(uploadPath, fileName);

            using (var fs = new FileStream(savePath, FileMode.Create))
            {
                await file.CopyToAsync(fs);
            }

            return Json(new { location = $"/uploads/blog/{fileName}" });
        }
    }
}