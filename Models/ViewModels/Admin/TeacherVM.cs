using System.ComponentModel.DataAnnotations;

namespace TuitionCenter.Models.ViewModels.Admin;

public class TeacherVM
{
    // Common teacher information

    public int UserId { get; set; }

    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(150)]
    public string FullName { get; set; } = null!;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = null!;

    [Phone(ErrorMessage = "Enter a valid phone number.")]
    public string? Phone { get; set; }

    public bool IsActive { get; set; }

    public DateTime? CreatedDate { get; set; }


    // Create teacher

    [DataType(DataType.Password)]
    [StringLength(
        100,
        MinimumLength = 6,
        ErrorMessage = "Password must be at least 6 characters."
    )]
    public string? Password { get; set; }


    // Teacher list

    public int AssignedBatchCount { get; set; }


    // Teacher details

    public List<BatchSummaryVM> Batches { get; set; }
        = new List<BatchSummaryVM>();
}


// Batch summary

public class BatchSummaryVM
{
    public int BatchId { get; set; }

    public string BatchName { get; set; } = null!;

    public string ClassName { get; set; } = null!;

    public string SubjectName { get; set; } = null!;

    public int StudentCount { get; set; }
}