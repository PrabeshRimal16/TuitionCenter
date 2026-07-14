using System;
using System.Collections.Generic;

namespace TuitionCenter.Models;

public partial class Batch
{
    public int BatchId { get; set; }

    public string BatchName { get; set; } = null!;

    public int ClassId { get; set; }

    public int SubjectId { get; set; }

    public int TeacherId { get; set; }

    public int TimeSlotId { get; set; }

    public int CourseTypeId { get; set; }

    public int Capacity { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<Announcement> Announcements { get; set; } = new List<Announcement>();

    public virtual Class Class { get; set; } = null!;

    public virtual ICollection<ClassSession> ClassSessions { get; set; } = new List<ClassSession>();

    public virtual CourseType CourseType { get; set; } = null!;

    public virtual ICollection<EnrollmentSubject> EnrollmentSubjects { get; set; } = new List<EnrollmentSubject>();

    public virtual Subject Subject { get; set; } = null!;

    public virtual User Teacher { get; set; } = null!;

    public virtual TimeSlot TimeSlot { get; set; } = null!;
}
