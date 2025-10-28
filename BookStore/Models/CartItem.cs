namespace BookStore.Models;

public class CartItem
{
    public int BookId { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public string? CoverUrl { get; set; }
}
