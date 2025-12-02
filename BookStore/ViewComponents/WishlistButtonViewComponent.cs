using BookStore.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookStore.ViewComponents;

public class WishlistButtonViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userMgr;

    public WishlistButtonViewComponent(ApplicationDbContext db, UserManager<IdentityUser> userMgr)
    {
        _db = db;
        _userMgr = userMgr;
    }

    public async Task<IViewComponentResult> InvokeAsync(int bookId)
    {
        bool isInWishlist = false;

        if (User?.Identity?.IsAuthenticated == true)
        {
            var user = await _userMgr.GetUserAsync(UserClaimsPrincipal);
            if (user != null)
            {
                isInWishlist = await _db.WishlistItems
                    .AnyAsync(w => w.UserId == user.Id && w.BookId == bookId);
            }
        }

        ViewBag.BookId = bookId;
        ViewBag.IsInWishlist = isInWishlist;
        ViewBag.IsAuthenticated = User?.Identity?.IsAuthenticated == true;

        return View();
    }
}

