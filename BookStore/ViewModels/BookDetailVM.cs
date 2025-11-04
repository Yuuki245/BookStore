using BookStore.Models;
using System.Collections.Generic;

namespace BookStore.Models.ViewModels
{
    public class BookDetailVM
    {
        public Book MainBook { get; set; } = new();
        public IEnumerable<Book> RelatedBooks { get; set; } = Enumerable.Empty<Book>();
        public IEnumerable<Review> Reviews { get; set; } = Enumerable.Empty<Review>();
        public Review NewReview { get; set; } = new(); // Dùng để binding form gửi đánh giá
        public string UserReviewStatus { get; set; } = "NotLoggedIn";
    }
}