using System.ComponentModel.DataAnnotations;

namespace TuitionCenter.Models
{
    public class UserListEdit
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(150)]
        public string FullName { get; set; } = null!;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
        public string PasswordHash { get; set; } = null!;

        [Phone(ErrorMessage = "Enter a valid phone number.")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Role is required.")]
        public string Role { get; set; } = null!;

        public bool? IsActive { get; set; }
    }
}