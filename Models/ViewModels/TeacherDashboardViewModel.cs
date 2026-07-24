using System;
using System.Collections.Generic;

namespace TuitionCenter.Models.ViewModels
{
    public class TeacherDashboardViewModel
    {
        public string TeacherName { get; set; } = null!;
        public string TeacherEmail { get; set; } = null!;
        public int TotalStudents { get; set; }
        public int ActiveClasses { get; set; }
        public double AverageAttendance { get; set; }
        public string CurrentAcademicYear { get; set; } = null!;
        
        public UpcomingClassViewModel? NextClass { get; set; }
        public List<UpcomingClassViewModel> UpcomingClasses { get; set; } = new();
    }

    public class UpcomingClassViewModel
    {
        public int SessionId { get; set; }
        public string Title { get; set; } = null!;
        public string BatchName { get; set; } = null!;
        public string SubjectName { get; set; } = null!;
        public string ClassName { get; set; } = null!;
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public int DurationMinutes => (int)(EndTime - StartTime).TotalMinutes;
        public int EnrolledStudents { get; set; }
        public string MeetingLink { get; set; } = null!;
    }
}
