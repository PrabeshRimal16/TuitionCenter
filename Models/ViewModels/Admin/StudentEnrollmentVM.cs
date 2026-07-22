namespace TuitionCenter.Models.ViewModels.Admin;

public class StudentEnrollmentVM
{
    public int EnrollmentId { get; set; }

    public string? ClassName { get; set; }

    public string? SubjectName { get; set; }

    public string? CourseType { get; set; }

    public string? EnrollmentStatus { get; set; }

    public string? PaymentStatus { get; set; }

    public string? BatchName { get; set; }

    public DateTime? EnrollmentDate { get; set; }
}