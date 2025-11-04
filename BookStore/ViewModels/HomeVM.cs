namespace BookStore.Models.ViewModels
{
    public class HomeVM
    {
        public IEnumerable<Book> Bestsellers { get; set; } = Enumerable.Empty<Book>();
        public IEnumerable<Book> NewReleases { get; set; } = Enumerable.Empty<Book>();
    }
}
