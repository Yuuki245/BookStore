using BookStore.Data;
using BookStore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Controllers
{
    public class PaymentController : Controller
    {
        private readonly IConfiguration _config;
        private readonly ApplicationDbContext _db;
        private readonly ICartService _cart;

        public PaymentController(IConfiguration config, ApplicationDbContext db, ICartService cart)
        {
            _config = config;
            _db = db;
            _cart = cart;
        }

        // GET: /Payment/VnPayCallback
        public async Task<IActionResult> VnPayCallback()
        {
            var vnpayData = new VnPayLibrary();
            var vnpHashSecret = _config["VnPay:HashSecret"]!;

            // Lấy tất cả dữ liệu VNPay trả về
            foreach (var (key, value) in Request.Query)
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                {
                    vnpayData.AddResponseData(key, value.ToString());
                }
            }

            var orderIdStr = vnpayData.GetResponseData("vnp_TxnRef");
            var vnpResponseCode = vnpayData.GetResponseData("vnp_ResponseCode");
            var vnpSecureHash = vnpayData.GetResponseData("vnp_SecureHash");

            bool checkSignature = vnpayData.ValidateSignature(vnpSecureHash, vnpHashSecret);

            if (!checkSignature || !int.TryParse(orderIdStr, out var orderId))
            {
                TempData["Error"] = "Thanh toán thất bại: Chữ ký không hợp lệ.";
                return RedirectToAction("Index", "Cart");
            }

            var order = await _db.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("Index", "Cart");
            }

            // Chỉ xử lý đơn hàng đang "Pending"
            if (order.Status != "Pending")
            {
                // Đơn hàng đã được xử lý (thành công hoặc thất bại)
                return RedirectToAction("Details", "Orders", new { id = order.Id });
            }

            if (vnpResponseCode == "00") // Thanh toán thành công
            {
                // 1. Cập nhật trạng thái đơn hàng
                order.Status = "Confirmed";

                // 2. Trừ tồn kho
                foreach (var item in order.Items)
                {
                    var book = await _db.Books.FindAsync(item.BookId);
                    if (book != null)
                    {
                        book.Stock -= item.Quantity;
                    }
                }

                await _db.SaveChangesAsync();

                // 3. Xóa giỏ hàng
                await _cart.ClearAsync();

                TempData["Success"] = $"Thanh toán VNPay thành công! Mã đơn: #{order.Id}";
                return RedirectToAction("Details", "Orders", new { id = order.Id });
            }
            else
            {
                // Thanh toán thất bại (VD: hủy đơn)
                order.Status = "Canceled";
                await _db.SaveChangesAsync();

                TempData["Error"] = "Thanh toán VNPay thất bại.";
                return RedirectToAction("Details", "Orders", new { id = order.Id });
            }
        }
    }
}