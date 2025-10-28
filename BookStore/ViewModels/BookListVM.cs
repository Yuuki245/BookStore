using BookStore.Models;

namespace BookStore.ViewModels;

public class BookListVM
{
    public IEnumerable<Book> Books { get; set; } = Enumerable.Empty<Book>();
    public int? CategoryId { get; set; }
    public string? Search { get; set; }
}
