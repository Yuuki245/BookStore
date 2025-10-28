using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookStore.Models;

public class Book
{
    public int Id { get; set; }

    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(150)]
    public string? Author { get; set; }

    // ISBN-13 tối đa 13 ký tự; để nullable vì có sách không ISBN
    [StringLength(13)]
    public string? Isbn { get; set; }

    [StringLength(500)]
    public string? CoverUrl { get; set; }

    // Giá & tồn kho là value types => mặc định NOT NULL (không cần [Required])
    [Column(TypeName = "decimal(18,2)")]
    [Range(0, 1_000_000)]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue)]
    public int Stock { get; set; }

    // FK bắt buộc
    [Required]
    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public string? Description { get; set; }
}
