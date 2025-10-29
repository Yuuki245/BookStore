namespace BookStore.Models.ViewModels
{
    public class OrderAdminRowVM
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string UserEmail { get; set; } = "";
        public string Status { get; set; } = "";
        public decimal TotalAmount { get; set; }
    }

    public class OrderAdminListVM
    {
        public IEnumerable<OrderAdminRowVM> Items { get; set; } = Enumerable.Empty<OrderAdminRowVM>();
        public int Page { get; set; }
        public int TotalPages { get; set; }
        public string? Status { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }
}
