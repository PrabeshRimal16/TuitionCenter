using System;
using System.Collections.Generic;

namespace TuitionCenter.Models;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int EnrollmentId { get; set; }

    public decimal Amount { get; set; }

    public DateTime? PaymentDate { get; set; }

    public string? Method { get; set; }

    public string? TransactionId { get; set; }

    public string? ScreenshotPath { get; set; }

    public string? Status { get; set; }

    public DateTime? ApprovalDate { get; set; }

    public int? ApprovedBy { get; set; }

    public string? Remarks { get; set; }

    public virtual Enrollment Enrollment { get; set; } = null!;

    public virtual User? ApprovedByNavigation { get; set; }
}