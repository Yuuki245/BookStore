using BookStore.Data;
using BookStore.Models;
using BookStore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

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

        // 1. ĐÂY LÀ HÀM CALLBACK (RETURN URL) - Xử lý khi người dùng được trả về
        public async Task<IActionResult> VnPayCallback()
        {
            var vnpayData = new VnPayLibrary();
            var vnpHashSecret = _config["VnPay:HashSecret"]!;

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
            if (order.Status == "Pending")
            {
                if (vnpResponseCode == "00") // Thanh toán thành công
                {
                    await ProcessSuccessfulPayment(order);
                    TempData["Success"] = $"Thanh toán VNPay thành công! Mã đơn: #{order.Id}";
                }
                else
                {
                    // Thanh toán thất bại (VD: hủy đơn)
                    order.Status = "Canceled";
                    await _db.SaveChangesAsync();
                    TempData["Error"] = "Thanh toán VNPay thất bại.";
                }
            }

            // Chuyển hướng người dùng về trang chi tiết đơn hàng
            return RedirectToAction("Details", "Orders", new { id = order.Id });
        }

        // 2. 🟢 HÀM MỚI: ĐÂY LÀ HÀM XỬ LÝ IPN (SERVER-TO-SERVER)
        [HttpPost]
        public async Task<IActionResult> VnPayIPN()
        {
            var vnpayData = new VnPayLibrary();
            var vnpHashSecret = _config["VnPay:HashSecret"]!;

            // Lấy dữ liệu từ VNPay
            var json = new System.IO.StreamReader(Request.Body).ReadToEndAsync().Result;
            // Chuyển đổi query string (a=1&b=2) thành Dictionary
            var queryParams = json.Split('&')
                .ToDictionary(p => p.Split('=')[0], p => System.Net.WebUtility.UrlDecode(p.Split('=')[1]));

            foreach (var (key, value) in queryParams)
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
                // Lỗi: Chữ ký không hợp lệ
                return Ok(new { RspCode = "97", Message = "Invalid Checksum" });
            }

            var order = await _db.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                // Lỗi: Không tìm thấy đơn hàng
                return Ok(new { RspCode = "01", Message = "Order not found" });
            }

            // Chỉ xử lý nếu đơn hàng đang "Pending"
            if (order.Status != "Pending")
            {
                // Đơn hàng đã được xử lý (bởi Callback)
                return Ok(new { RspCode = "02", Message = "Order already confirmed" });
            }

            if (vnpResponseCode == "00") // Thanh toán thành công
            {
                await ProcessSuccessfulPayment(order);
                // Trả về cho VNPay là đã nhận thành công
                return Ok(new { RspCode = "00", Message = "Confirm Success" });
            }
            else
            {
                // Thanh toán thất bại
                order.Status = "Canceled";
                await _db.SaveChangesAsync();
                // Trả về cho VNPay là đã nhận thất bại
                return Ok(new { RspCode = "99", Message = "Failed" });
            }
        }

        // 3. 🟢 HÀM HỖ TRỢ: Dùng chung cho cả Callback và IPN
        private async Task ProcessSuccessfulPayment(Order order)
        {
            order.Status = "Confirmed"; // Cập nhật trạng thái

            // Trừ tồn kho
            foreach (var item in order.Items)
            {
                var book = await _db.Books.FindAsync(item.BookId);
                if (book != null)
                {
                    book.Stock -= item.Quantity;
                }
            }

            await _db.SaveChangesAsync();

            // Xóa giỏ hàng (chỉ thực hiện nếu đây là Callback của người dùng)
            await _cart.ClearAsync();
        }
    }
}