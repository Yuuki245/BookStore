using System.Collections.Generic;

namespace BookStore.Models.ViewModels
{
    public class ManageRolesVM
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<RoleCheckbox> Roles { get; set; } = new();
    }

    public class RoleCheckbox
    {
        public string Name { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }
}