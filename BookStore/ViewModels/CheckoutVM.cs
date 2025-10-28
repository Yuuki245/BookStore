using System.ComponentModel.DataAnnotations;

namespace BookStore.ViewModels;

public class CheckoutVM
{
    [Required, StringLength(200)]
    public string ShippingAddress { get; set; } = string.Empty;

    [Required, Phone, StringLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;
}
