using System;
using System.Collections.Generic;

namespace TuitionCenter.Models;

public partial class StudentProfile
{
    public int StudentProfileId { get; set; }

    public int UserId { get; set; }

    public string? Address { get; set; }

    public string? SchoolName { get; set; }

    public string? ParentName { get; set; }

    public string? ParentContact { get; set; }

    public string? EmergencyContact { get; set; }

    public bool IsProfileComplete { get; set; }

    public DateTime? RegisteredDate { get; set; }

    public virtual User User { get; set; } = null!;
}
