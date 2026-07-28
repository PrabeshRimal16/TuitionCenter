using System;
using System.Collections.Generic;

namespace TuitionCenter.Models
{
    public class IntakeViewModel
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; } = "";
        public List<CourseTypeOption> CourseTypes { get; set; } = new();
        public List<TimeSlotOption> TimeSlots { get; set; } = new();
        public List<IntakeSubjectViewModel> Subjects { get; set; } = new();
        public int? SelectedCourseTypeId { get; set; }
        public int? SelectedTimeSlotId { get; set; }

        public List<IntakeMonthOption> IntakeMonths { get; set; } = new();
        public string? SelectedIntakeMonth { get; set; }
    }

    public class CourseTypeOption
    {
        public int CourseTypeId { get; set; }
        public string TypeName { get; set; } = "";
    }

    public class TimeSlotOption
    {
        public int TimeSlotId { get; set; }
        public string Label { get; set; } = "";
    }

    public class IntakeSubjectViewModel
    {
        public int SubjectId { get; set; }
        public string Name { get; set; } = "";
        public int? SelectedBatchId { get; set; }
        public List<IntakeBatchOption> Batches { get; set; } = new();
    }

    public class IntakeBatchOption
    {
        public int BatchId { get; set; }
        public int CourseTypeId { get; set; }
        public string TeacherName { get; set; } = "";
        public string TimeLabel { get; set; } = "";
        public int SeatsLeft { get; set; }
    }

    public class IntakeMonthOption
    {
        public string Value { get; set; } = "";
        public string MonthName { get; set; } = "";
        public string Year { get; set; } = "";
        public string StartDateLabel { get; set; } = "";
        public DateTime StartDate { get; set; }
        public int DurationMonths { get; set; }
        public bool IsPopular { get; set; }
    }
}