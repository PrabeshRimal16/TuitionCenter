using System;
using System.Collections.Generic;

namespace TuitionCenter.Models;

public partial class Payment
{
    public int Id { get; set; }

    public int EnrollmentId { get; set; }

    public decimal Amount { get; set; }

    public string PaymentReference { get; set; } = null!;

    public string ScreenshotPath { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime SubmittedDate { get; set; }

    public int? ReviewedByAdminId { get; set; }

    public DateTime? ReviewedDate { get; set; }

    public string? AdminRemarks { get; set; }

    public virtual Enrollment Enrollment { get; set; } = null!;

    public virtual User? ReviewedByAdmin { get; set; }
}
