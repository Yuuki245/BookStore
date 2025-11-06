using BookStore.Models;
using System.Collections.Generic;

namespace BookStore.Models.ViewModels
{
    public class BlogListVM
    {
        public IEnumerable<BlogPost> Posts { get; set; } = Enumerable.Empty<BlogPost>();
        public int Page { get; set; }
        public int TotalPages { get; set; }
    }
}