using BookStore.Data;
using BookStore.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly UserManager<IdentityUser> _userMgr;
        private readonly RoleManager<IdentityRole> _roleMgr;

        // 1. Inject UserManager và RoleManager
        public UsersController(UserManager<IdentityUser> userMgr, RoleManager<IdentityRole> roleMgr)
        {
            _userMgr = userMgr;
            _roleMgr = roleMgr;
        }

        // 2. Action INDEX (Danh sách User)
        // GET: /Admin/Users
        public async Task<IActionResult> Index()
        {
            var users = await _userMgr.Users.AsNoTracking().ToListAsync();
            var userListVM = new List<UserVM>();

            foreach (var user in users)
            {
                userListVM.Add(new UserVM
                {
                    UserId = user.Id,
                    Email = user.Email ?? "N/A",
                    Roles = await _userMgr.GetRolesAsync(user)
                });
            }
            return View(userListVM);
        }

        // 3. Action MANAGEROLES (GET) (Hiển thị form sửa quyền)
        // GET: /Admin/Users/ManageRoles/userid...
        public async Task<IActionResult> ManageRoles(string id)
        {
            var user = await _userMgr.FindByIdAsync(id);
            if (user == null) return NotFound();

            var vm = new ManageRolesVM
            {
                UserId = user.Id,
                Email = user.Email ?? "N/A"
            };

            // Lấy tất cả các quyền (Roles) trong hệ thống
            var allRoles = await _roleMgr.Roles.ToListAsync();

            // Kiểm tra xem user hiện tại có những quyền nào
            foreach (var role in allRoles)
            {
                vm.Roles.Add(new RoleCheckbox
                {
                    Name = role.Name!,
                    IsSelected = await _userMgr.IsInRoleAsync(user, role.Name!)
                });
            }

            return View(vm);
        }

        // 4. Action MANAGEROLES (POST) (Lưu lại quyền)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageRoles(ManageRolesVM vm)
        {
            var user = await _userMgr.FindByIdAsync(vm.UserId);
            if (user == null) return NotFound();

            // Lấy các quyền mà user hiện đang có
            var currentRoles = await _userMgr.GetRolesAsync(user);

            // Lấy các quyền MỚI được check
            var newRoles = vm.Roles.Where(r => r.IsSelected).Select(r => r.Name).ToList();

            // Tính toán:
            // 1. Các quyền cần XÓA (có trong current, không có trong new)
            var rolesToRemove = currentRoles.Except(newRoles);
            await _userMgr.RemoveFromRolesAsync(user, rolesToRemove);

            // 2. Các quyền cần THÊM (có trong new, không có trong current)
            var rolesToAdd = newRoles.Except(currentRoles);
            await _userMgr.AddToRolesAsync(user, rolesToAdd);

            TempData["Success"] = "Cập nhật quyền thành công.";
            return RedirectToAction(nameof(Index));
        }
    }
}