namespace TuitionCenter.Models.ViewModels.Admin;

public class AdminDashboardVM
{
    public string AdminName { get; set; } = "Admin";
    public string Greeting { get; set; } = "Good Morning";
    public string FormattedDate { get; set; } = string.Empty;
    public string FormattedTime { get; set; } = string.Empty;

    public int ActiveSessionsCount { get; set; } = 1284;
    public int SystemLoadPercent { get; set; } = 24;
    public int NewTodayCount { get; set; } = 42;

    public int TotalStudents { get; set; }
    public int TotalTeachers { get; set; }
    public int TotalCourses { get; set; }
    public decimal MonthlyRevenue { get; set; }

    public List<DashboardEnrollmentItemVM> RecentEnrollments { get; set; } = new();
}

public class DashboardEnrollmentItemVM
{
    public int EnrollmentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? EnrolledDate { get; set; }
}
