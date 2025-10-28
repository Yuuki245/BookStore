using BookStore.Models;

namespace BookStore.Services;

public interface ICartService
{
    Task<List<CartItem>> GetItemsAsync();          // ✅ khớp SessionCartService
    Task AddAsync(Book book, int qty = 1);         // ✅ khớp
    Task UpdateAsync(int bookId, int qty);
    Task RemoveAsync(int bookId);
    Task ClearAsync();
    decimal Total(IEnumerable<CartItem> items);
}
