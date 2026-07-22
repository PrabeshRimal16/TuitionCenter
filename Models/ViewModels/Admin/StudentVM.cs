using System.ComponentModel.DataAnnotations;

namespace TuitionCenter.Models.ViewModels.Admin;

public class StudentVM
{
    public int UserId { get; set; }

    [Required(ErrorMessage = "Full name is required.")]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Enter a valid phone number.")]
    public string? Phone { get; set; }

    [DataType(DataType.Password)]
    public string? Password { get; set; }

    public bool IsActive { get; set; }

    public DateTime? CreatedDate { get; set; }

    public int EnrollmentCount { get; set; }

    public List<StudentEnrollmentVM> Enrollments { get; set; } = new();
}