using BookStore.Models;

namespace BookStore.ViewModels;

public class CartVM
{
    public List<CartItem> Items { get; set; } = new();
    public decimal Total => Items.Sum(i => i.UnitPrice * i.Quantity);
}
