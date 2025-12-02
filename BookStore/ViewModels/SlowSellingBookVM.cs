namespace BookStore.ViewModels;

public class SlowSellingBookVM
{
    public int BookId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Author { get; set; }
    public string? CategoryName { get; set; }
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
    public int Stock { get; set; }
    public decimal Price { get; set; }
}

