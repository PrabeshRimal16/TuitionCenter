using System;
using System.Collections.Generic;

namespace TuitionCenter.Models
{
    public class StudentDashboardViewModel
    {
        public string StudentName { get; set; } = "";
        public string AvatarInitial { get; set; } = "";
        public DateTime Today { get; set; } = DateTime.Now;

        public int ActiveEnrollmentCount { get; set; }
        public string ActiveCourseSummary { get; set; } = "";

        public int TodaysSessionCount { get; set; }
        public string TodaysSessionTimeRange { get; set; } = "";

        public int SessionsCompleted { get; set; }
        public int SessionsTotal { get; set; }

        public string PaymentStatus { get; set; } = "No enrollment yet";
        public decimal PaymentAmount { get; set; }

        public List<UpcomingSessionViewModel> UpcomingSessions { get; set; } = new();
        public List<DashboardAnnouncementViewModel> Announcements { get; set; } = new();
        public List<EnrolledCourseViewModel> EnrolledCourses { get; set; } = new();
    }

    public class UpcomingSessionViewModel
    {
        public string SubjectName { get; set; } = "";
        public string ClassName { get; set; } = "";
        public string Title { get; set; } = "";
        public string TeacherName { get; set; } = "";
        public string TimeRange { get; set; } = "";
        public string StartsInLabel { get; set; } = "";
        public string MeetingLink { get; set; } = "";
        public bool IsToday { get; set; }
    }

    public class DashboardAnnouncementViewModel
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string TimeAgo { get; set; } = "";
        public string TeacherName { get; set; } = "";
    }

    public class EnrolledCourseViewModel
    {
        public string ClassName { get; set; } = "";
        public string SubjectsSummary { get; set; } = "";
        public decimal Amount { get; set; }
        public string PlanLabel { get; set; } = "";
        public int SessionsCompleted { get; set; }
        public int SessionsTotal { get; set; }
        public int ProgressPercent => SessionsTotal == 0 ? 0 : (int)(SessionsCompleted * 100.0 / SessionsTotal);
        public int EnrollmentId { get; set; }
    }
}