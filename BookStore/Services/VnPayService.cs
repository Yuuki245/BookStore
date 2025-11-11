using BookStore.Models;

namespace BookStore.Services
{
    public class VnPayService : IVnPayService
    {
        private readonly IConfiguration _config;

        public VnPayService(IConfiguration config)
        {
            _config = config;
        }

        public string CreatePaymentUrl(Order order, HttpContext context)
        {
            var vnpay = new VnPayLibrary();
            var vnpUrl = _config["VnPay:BaseUrl"]!;
            var vnpTmnCode = _config["VnPay:TmnCode"]!;
            var vnpHashSecret = _config["VnPay:HashSecret"]!;
            var vnpReturnUrl = _config["VnPay:ReturnUrl"]!;

            vnpay.AddRequestData("vnp_Version", "2.1.0");
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", vnpTmnCode);
            vnpay.AddRequestData("vnp_Amount", (order.TotalAmount * 100).ToString("F0")); // Số tiền * 100
            vnpay.AddRequestData("vnp_CreateDate", order.CreatedAt.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", context.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1");
            vnpay.AddRequestData("vnp_Locale", "vn");
            vnpay.AddRequestData("vnp_OrderInfo", $"Thanh toan don hang #{order.Id}");
            vnpay.AddRequestData("vnp_OrderType", "other"); // Ghi "other" cho an toàn
            vnpay.AddRequestData("vnp_ReturnUrl", vnpReturnUrl);
            vnpay.AddRequestData("vnp_TxnRef", order.Id.ToString()); // Mã tham chiếu (Mã đơn hàng)

            var paymentUrl = vnpay.CreateRequestUrl(vnpUrl, vnpHashSecret);
            return paymentUrl;
        }
    }
}