using System;
using System.Collections.Generic;

namespace TuitionCenter.Models.ViewModels
{
    public class TeacherScheduleViewModel
    {
        public string TeacherName { get; set; } = null!;
        public string TeacherEmail { get; set; } = null!;
        public DateTime CurrentWeekStart { get; set; }
        public DateTime CurrentWeekEnd { get; set; }
        public string WeekDateRangeDisplay { get; set; } = null!;
        public List<DayScheduleViewModel> Days { get; set; } = new();
    }

    public class DayScheduleViewModel
    {
        public DateTime Date { get; set; }
        public string DayOfWeek { get; set; } = null!;
        public int DayOfMonth { get; set; }
        public bool IsToday { get; set; }
        public List<ClassSessionViewModel> Sessions { get; set; } = new();
    }

    public class ClassSessionViewModel
    {
        public int SessionId { get; set; }
        public string Title { get; set; } = null!;
        public string SubjectName { get; set; } = null!;
        public string ClassName { get; set; } = null!;
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public string ColorTheme { get; set; } = null!;
    }
}
