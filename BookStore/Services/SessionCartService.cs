using System.Text.Json;
using BookStore.Data;
using BookStore.Models;

namespace BookStore.Services;

public class SessionCartService : ICartService
{
    private const string Key = "CART";
    private readonly IHttpContextAccessor _http;
    private readonly ApplicationDbContext _db;

    public SessionCartService(IHttpContextAccessor http, ApplicationDbContext db)
    { _http = http; _db = db; }

    private ISession Session => _http.HttpContext!.Session;

    public async Task<List<CartItem>> GetItemsAsync()
    {
        var json = Session.GetString(Key);
        return json is null ? new List<CartItem>()
            : (JsonSerializer.Deserialize<List<CartItem>>(json) ?? new List<CartItem>());
    }

    private void Save(List<CartItem> items) =>
        Session.SetString(Key, JsonSerializer.Serialize(items));

    public async Task AddAsync(Book book, int qty = 1)
    {
        var items = await GetItemsAsync();
        var it = items.FirstOrDefault(i => i.BookId == book.Id);
        // Sử dụng giá sau flash sale nếu có
        var unitPrice = book.IsOnFlashSale ? book.FinalPrice : book.Price;
        if (it is null)
            items.Add(new CartItem { BookId = book.Id, Title = book.Title, UnitPrice = unitPrice, Quantity = qty, CoverUrl = book.CoverUrl });
        else
            it.Quantity += qty;
        Save(items);
    }

    public async Task UpdateAsync(int bookId, int qty)
    {
        var items = await GetItemsAsync();
        var it = items.FirstOrDefault(i => i.BookId == bookId);
        if (it != null)
        {
            it.Quantity = Math.Max(0, qty);
            if (it.Quantity == 0) items.Remove(it);
            Save(items);
        }
    }

    public async Task RemoveAsync(int bookId)
    {
        var items = await GetItemsAsync();
        items.RemoveAll(i => i.BookId == bookId);
        Save(items);
    }

    public Task ClearAsync()
    {
        Session.Remove(Key);
        return Task.CompletedTask;
    }

    public decimal Total(IEnumerable<CartItem> items) => items.Sum(i => i.UnitPrice * i.Quantity);
}
