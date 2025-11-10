using BookStore.Data;
using BookStore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookStore.ViewComponents
{
    public class MegaMenuViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _db;
        public MegaMenuViewComponent(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var allCategories = await _db.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync();

            // Chia danh sách thành 4 cột để hiển thị
            var columns = new List<List<Category>>();
            int colCount = 4;
            int itemsPerCol = (int)System.Math.Ceiling(allCategories.Count / (double)colCount);

            for (int i = 0; i < colCount; i++)
            {
                columns.Add(allCategories.Skip(i * itemsPerCol).Take(itemsPerCol).ToList());
            }

            return View(columns); // Trả về Default.cshtml
        }
    }
}