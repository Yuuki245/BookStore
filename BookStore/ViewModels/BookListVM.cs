namespace BookStore.Models.ViewModels
{
    public class BookListVM
    {
        public IEnumerable<Book> Books { get; set; } = Enumerable.Empty<Book>();
        public IEnumerable<Category> Categories { get; set; } = Enumerable.Empty<Category>();
        
        // Basic filters
        public int? CategoryId { get; set; }
        public string? Search { get; set; }
        public string? Sort { get; set; } // price_asc, price_desc, title_asc, title_desc, rating_desc, bestseller
        public int Page { get; set; } = 1;
        public int TotalPages { get; set; }
        
        // Advanced filters
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int? MinRating { get; set; } // 1-5 stars
    }
}
