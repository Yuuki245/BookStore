using System.ComponentModel.DataAnnotations;

namespace BookStore.Models.ViewModels
{
    public class CheckoutVM
    {
        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng")]
        [StringLength(200)]
        public string ShippingAddress { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [RegularExpression(@"^(0|\+84)(\d{9,10})$", ErrorMessage = "Số điện thoại không hợp lệ")]
        public string PhoneNumber { get; set; } = "";

        public IEnumerable<CartItem> Items { get; set; } = new List<CartItem>();
        public decimal Subtotal => Items.Sum(i => i.UnitPrice * i.Quantity);
        
        // Danh sách địa chỉ đã sử dụng trước đó
        public List<AddressOption> SavedAddresses { get; set; } = new List<AddressOption>();

        // Mã giảm giá
        public string? CouponCode { get; set; }
        public decimal CouponDiscount { get; set; } = 0;
        public string? CouponMessage { get; set; }

        // Phương thức thanh toán: "COD" hoặc "QKT"
        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
        public string PaymentMethod { get; set; } = "COD";
    }

    public class AddressOption
    {
        public string Address { get; set; } = "";
        public string Phone { get; set; } = "";
        public string DisplayText => $"{Address} - {Phone}";
    }

    public class CartItemVM
    {
        public int BookId { get; set; }
        public string Title { get; set; } = "";
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public string? CoverUrl { get; set; }
    }
}
