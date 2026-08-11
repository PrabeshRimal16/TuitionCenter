using System;
using System.Collections.Generic;

namespace TuitionCenter.Models.ViewModels.Admin;

public class StudentEnrollmentVM
{
    public int EnrollmentId { get; set; }
    public string? EnrollmentNumber { get; set; }
    public string? StudentName { get; set; }
    public string? StudentEmail { get; set; }
    public string? ClassName { get; set; }
    public string? SubjectsSummary { get; set; }
    public string? SubjectName
    {
        get => SubjectsSummary;
        set => SubjectsSummary = value;
    }
    public string? CourseType { get; set; }
    public string? TimeSlotLabel { get; set; }
    public decimal Amount { get; set; }
    public string? EnrollmentStatus { get; set; }
    public string? PaymentStatus { get; set; }
    public string? PaymentMethod { get; set; }
    public string? TransactionId { get; set; }
    public string? ScreenshotPath { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime? EnrollmentDate { get; set; }
}

public class EnrollmentsListVM
{
    public List<StudentEnrollmentVM> Enrollments { get; set; } = new();
    public string ActiveFilter { get; set; } = "All";
    public int TotalCount { get; set; }
    public int PendingCount { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }
}