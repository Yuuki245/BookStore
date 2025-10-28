using BookStore.Models;

namespace BookStore.Services;

public interface ICartService
{
    Task<List<CartItem>> GetItemsAsync();
    Task AddAsync(Book book, int qty = 1);
    Task UpdateAsync(int bookId, int qty);
    Task RemoveAsync(int bookId);
    Task ClearAsync();
    decimal Total(IEnumerable<CartItem> items);
}
