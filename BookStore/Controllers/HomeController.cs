using BookStore.Data;
using BookStore.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<HomeController> _logger;
    public HomeController(ILogger<HomeController> logger, ApplicationDbContext db)
    { _logger = logger; _db = db; }

    public async Task<IActionResult> Index()
    {
        var vm = new HomeVM
        {
            Bestsellers = await _db.Books.AsNoTracking().OrderByDescending(b => b.Price).Take(12).ToListAsync(),
            NewReleases = await _db.Books.AsNoTracking().OrderByDescending(b => b.Id).Take(12).ToListAsync()
        };
        return View(vm);
    }

    public IActionResult Privacy() => View();
}
