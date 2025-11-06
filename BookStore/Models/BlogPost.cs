using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace BookStore.Models
{
    public class BlogPost
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề")]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập nội dung")]
        public string Content { get; set; } = string.Empty;

        // Mô tả ngắn/Tóm tắt
        [StringLength(500)]
        public string? Excerpt { get; set; }

        // Ảnh bìa
        [StringLength(500)]
        public string? HeaderImageUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Liên kết với người đăng bài (Admin)
        public string UserId { get; set; } = string.Empty;
        public IdentityUser? User { get; set; }
    }
}