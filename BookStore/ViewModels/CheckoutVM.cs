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
