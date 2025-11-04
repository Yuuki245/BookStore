using BookStore.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookStore.ViewComponents
{
    public class CategoriesNavViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _db;
        // 1. Định nghĩa số lượng thể loại hiển thị chính
        private const int CategoriesToShow = 5;

        public CategoriesNavViewComponent(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // 2. Lấy TẤT CẢ thể loại
            var allCategories = await _db.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync();

            // 3. Chia chúng thành 2 nhóm và gửi ra View
            var viewModel = new CategoriesNavVM
            {
                FeaturedCategories = allCategories.Take(CategoriesToShow).ToList(),
                MoreCategories = allCategories.Skip(CategoriesToShow).ToList()
            };

            return View(viewModel); // Trả về Default.cshtml
        }
    }

    // Model đơn giản để truyền 2 danh sách ra View
    public class CategoriesNavVM
    {
        public List<BookStore.Models.Category> FeaturedCategories { get; set; } = new();
        public List<BookStore.Models.Category> MoreCategories { get; set; } = new();
    }
}