namespace BookStore.Models.ViewModels
{
    public class HomeVM
    {
        public IEnumerable<Book> Bestsellers { get; set; } = Enumerable.Empty<Book>();
        public IEnumerable<Book> NewReleases { get; set; } = Enumerable.Empty<Book>();
        public IEnumerable<Book> DailySales { get; set; } = Enumerable.Empty<Book>();
        public IEnumerable<Book> FlashSaleBooks { get; set; } = Enumerable.Empty<Book>();
        public BookStore.Models.FlashSale? ActiveFlashSale { get; set; }
    }
}
