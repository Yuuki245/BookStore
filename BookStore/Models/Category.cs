using System.ComponentModel.DataAnnotations;

namespace BookStore.Models;

public class Category
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(255)]
    public string? Description { get; set; }

    public ICollection<Book> Books { get; set; } = new List<Book>();
}
