using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using DNTU_JOBS.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DNTU_JOBS.ViewModel
{
    public class UserViewModel
    {
        public string? Id { get; set; }

        [Required] // Essential fields for user creation
        public string UserName { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        // Optional fields
        public string? Address { get; set; }      // Nullable, so it can be null
        public string? Phone { get; set; }        // Nullable, so it can be null
        public string? Nationality { get; set; }  // Nullable, so it can be null
        public DateOnly? DateOfBirth { get; set; }
        public Gender? Gender { get; set; }

        // Roles and RolesList for assigning roles
        public IList<string> Roles { get; set; } = new List<string>();
        public List<SelectListItem> RolesList { get; set; } = new List<SelectListItem>();

        public bool IsLockedOut { get; set; } // Not required, defaults to false
        
        public bool CanManageAccount { get; set; }
        public bool CanManageJobs { get; set; }
        public bool CanManageChats { get; set; }
    }
}
