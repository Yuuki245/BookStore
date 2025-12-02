using BookStore.Models;

namespace BookStore.Services
{
    public class QKTPaymentService : IQKTPaymentService
    {
        private readonly IConfiguration _config;

        public QKTPaymentService(IConfiguration config)
        {
            _config = config;
        }

        public string CreatePaymentUrl(Order order, HttpContext context)
        {
            // Lấy base URL từ config hoặc tự động detect từ request
            var baseUrl = _config["QKTPayment:BaseUrl"];
            
            if (string.IsNullOrEmpty(baseUrl))
            {
                // Tự động detect URL từ request (hoạt động tốt trên cả local và production)
                var scheme = context.Request.Scheme;
                var host = context.Request.Host;
                baseUrl = $"{scheme}://{host}";
            }
            
            // Format amount với InvariantCulture để tránh vấn đề locale (dấu phẩy/chấm)
            var amountStr = order.TotalAmount.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var paymentUrl = $"{baseUrl}/QKTPayment/Payment?orderId={order.Id}&amount={amountStr}";
            return paymentUrl;
        }
    }
}

