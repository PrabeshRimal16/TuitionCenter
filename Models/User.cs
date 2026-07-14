using System;
using System.Collections.Generic;

namespace TuitionCenter.Models;

public partial class User
{
    public int UserId { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? Phone { get; set; }

    public string Role { get; set; } = null!;

    public bool? IsActive { get; set; }

    public DateTime? CreatedDate { get; set; }

    public virtual ICollection<Announcement> Announcements { get; set; } = new List<Announcement>();

    public virtual ICollection<Batch> Batches { get; set; } = new List<Batch>();

    public virtual ICollection<ClassSession> ClassSessions { get; set; } = new List<ClassSession>();

    public virtual ICollection<Enrollment> EnrollmentApprovedByNavigations { get; set; } = new List<Enrollment>();

    public virtual ICollection<Enrollment> EnrollmentStudents { get; set; } = new List<Enrollment>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual StudentProfile? StudentProfile { get; set; }
}
