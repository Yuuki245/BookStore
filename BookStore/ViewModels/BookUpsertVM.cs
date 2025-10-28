namespace BookStore.Models.ViewModels
{
    public class BookUpsertVM
    {
        public Book Book { get; set; } = new();
        public IFormFile? CoverFile { get; set; } // file ảnh upload
    }
}
