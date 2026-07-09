using System;
using System.Collections.Generic;

namespace TuitionCenter.Models;

public partial class User
{
    public int Id { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public int RoleId { get; set; }

    public string? ProfilePicturePath { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedDate { get; set; }

    public int? CreatedByAdminId { get; set; }

    public virtual ICollection<ClassSchedule> ClassSchedules { get; set; } = new List<ClassSchedule>();

    public virtual User? CreatedByAdmin { get; set; }

    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

    public virtual ICollection<User> InverseCreatedByAdmin { get; set; } = new List<User>();

    public virtual ICollection<OnlineClassLink> OnlineClassLinks { get; set; } = new List<OnlineClassLink>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual Role Role { get; set; } = null!;

    public virtual ICollection<TeacherSubject> TeacherSubjects { get; set; } = new List<TeacherSubject>();
}
