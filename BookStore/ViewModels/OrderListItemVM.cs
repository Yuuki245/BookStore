namespace BookStore.Models.ViewModels
{
    public class OrderListItemVM
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = "";
        public decimal TotalAmount { get; set; }
    }
}
