using BookStore.Services;
using Microsoft.AspNetCore.Mvc;
using System.Linq; // Cần thêm
using System.Threading.Tasks;

namespace BookStore.ViewComponents
{
    public class CartWidgetViewComponent : ViewComponent
    {
        private readonly ICartService _cart;

        public CartWidgetViewComponent(ICartService cart)
        {
            _cart = cart;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var items = await _cart.GetItemsAsync();

            // 🟢 Đếm tổng số lượng sản phẩm, không phải số dòng
            int totalCount = items.Sum(item => item.Quantity);

            return View(totalCount); // Trả về Default.cshtml
        }
    }
}