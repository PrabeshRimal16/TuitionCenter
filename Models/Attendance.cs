using System;

namespace TuitionCenter.Models;

public partial class Attendance
{
    public int AttendanceId { get; set; }
    public int SessionId { get; set; }
    public int StudentId { get; set; }
    public bool IsPresent { get; set; }
    public DateTime MarkedDate { get; set; }

    public virtual ClassSession Session { get; set; } = null!;
    public virtual User Student { get; set; } = null!;
}