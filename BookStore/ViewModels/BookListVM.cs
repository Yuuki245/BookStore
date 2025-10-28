namespace BookStore.Models.ViewModels
{
    public class BookListVM
    {
        public IEnumerable<Book> Books { get; set; } = Enumerable.Empty<Book>();
        public IEnumerable<Category> Categories { get; set; } = Enumerable.Empty<Category>();
        public int? CategoryId { get; set; }
        public string? Search { get; set; }
        public string? Sort { get; set; } // price_asc, price_desc, title_asc, title_desc
        public int Page { get; set; } = 1;
        public int TotalPages { get; set; }
    }
}
