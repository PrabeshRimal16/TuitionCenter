using System;
using System.Collections.Generic;

namespace TuitionCenter.Models;

public partial class Enrollment
{
    public int EnrollmentId { get; set; }

    public string EnrollmentNumber { get; set; } = null!;

    public int StudentId { get; set; }

    public int ClassId { get; set; }

    public int CourseTypeId { get; set; }

    public int PreferredTimeSlotId { get; set; }

    public decimal ExpectedAmount { get; set; }

    public string Status { get; set; } = null!;

    public string? RejectionReason { get; set; }

    public int? ApprovedBy { get; set; }

    public DateTime? ApprovalDate { get; set; }

    public DateTime? EnrolledDate { get; set; }

    public virtual User? ApprovedByNavigation { get; set; }

    public virtual Class Class { get; set; } = null!;

    public virtual CourseType CourseType { get; set; } = null!;

    public virtual ICollection<EnrollmentSubject> EnrollmentSubjects { get; set; } = new List<EnrollmentSubject>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual TimeSlot PreferredTimeSlot { get; set; } = null!;

    public virtual User Student { get; set; } = null!;
}
