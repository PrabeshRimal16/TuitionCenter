using System.Collections.Generic;

namespace TuitionCenter.Models
{
    public class ClassOverviewViewModel
    {
        public int MyEnrolledClassCount { get; set; }
        public int OverallCompletionPercent { get; set; }
        public int SessionsCompleted { get; set; }
        public int SessionsTotal { get; set; }
        public int UpcomingSessionCount { get; set; }
        public string UpcomingSessionLabel { get; set; } = "";
        public List<MyClassItem> MyClasses { get; set; } = new();
    }

    public class MyClassItem
    {
        public string ClassName { get; set; } = "";
        public string StudentName { get; set; } = "";
        public string SubjectsSummary { get; set; } = "";
        public string Status { get; set; } = "";
        public string EnrolledDateLabel { get; set; } = "";
        public decimal Amount { get; set; }
        public int SessionsCompleted { get; set; }
        public int SessionsTotal { get; set; }
        public int ProgressPercent => SessionsTotal == 0 ? 0 : (int)(SessionsCompleted * 100.0 / SessionsTotal);
    }

    public class ClassPickerItem
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; } = "";
        public string Label { get; set; } = "";
        public string Subtitle { get; set; } = "";
        public bool IsEnrolled { get; set; }
        public string? EnrollmentStatusLabel { get; set; }
        public int EnrolledStudentCount { get; set; }
    }
}