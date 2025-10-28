using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookStore.Models;

public class OrderItem
{
    public int Id { get; set; }

    [Required]
    public int OrderId { get; set; }
    public Order? Order { get; set; }

    [Required]
    public int BookId { get; set; }
    public Book? Book { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Range(0, 1_000_000)]
    public decimal UnitPrice { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}
