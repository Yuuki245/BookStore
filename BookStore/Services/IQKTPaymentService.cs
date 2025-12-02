using BookStore.Models;

namespace BookStore.Services
{
    public interface IQKTPaymentService
    {
        string CreatePaymentUrl(Order order, HttpContext context);
    }
}

