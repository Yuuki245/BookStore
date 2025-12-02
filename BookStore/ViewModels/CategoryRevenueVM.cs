namespace BookStore.ViewModels;

public class CategoryRevenueVM
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int TotalBooksSold { get; set; }
    public int TotalOrders { get; set; }
}

