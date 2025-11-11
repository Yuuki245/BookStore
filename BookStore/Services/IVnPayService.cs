using BookStore.Models;

namespace BookStore.Services
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(Order order, HttpContext context);
    }
}