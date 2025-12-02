using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace BookStore.Models;

public class WishlistItem
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public IdentityUser? User { get; set; }

    [Required]
    public int BookId { get; set; }
    public Book? Book { get; set; }

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}

