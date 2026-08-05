using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TuitionCenter.Models.ViewModels.Admin
{
    public class CourseManagementVM
    {
        public List<CourseTypeItemVM> CourseTypes { get; set; } = new();
        public List<TimeSlotItemVM> TimeSlots { get; set; } = new();
        public List<BatchItemVM> Batches { get; set; } = new();
        public List<TeacherAssignmentItemVM> TeacherAssignments { get; set; } = new();

        // Dropdown Lists for Forms
        public List<SelectListItem> ClassesList { get; set; } = new();
        public List<SelectListItem> SubjectsList { get; set; } = new();
        public List<SelectListItem> CourseTypesList { get; set; } = new();
        public List<SelectListItem> TimeSlotsList { get; set; } = new();
        public List<SelectListItem> TeachersList { get; set; } = new();
        public List<SelectListItem> BatchesList { get; set; } = new();
    }

    public class CourseTypeItemVM
    {
        public int CourseTypeId { get; set; }
        public string TypeName { get; set; } = string.Empty;
        public int ActiveBatchCount { get; set; }
    }

    public class TimeSlotItemVM
    {
        public int TimeSlotId { get; set; }
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        public string FormattedTime { get; set; } = string.Empty;
        public string Days { get; set; } = string.Empty;
        public int ActiveBatchCount { get; set; }
    }

    public class BatchItemVM
    {
        public int BatchId { get; set; }
        public string BatchName { get; set; } = string.Empty;
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public int CourseTypeId { get; set; }
        public string CourseTypeName { get; set; } = string.Empty;
        public int TimeSlotId { get; set; }
        public string TimeSlotName { get; set; } = string.Empty;
        public int TeacherId { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public int StudentCount { get; set; }
        public bool IsActive { get; set; }
        public DateTime? StartDate { get; set; }
        public string FormattedStartDate => StartDate.HasValue ? StartDate.Value.ToString("yyyy-MM-dd") : "N/A";
    }

    public class TeacherAssignmentItemVM
    {
        public int BatchId { get; set; }
        public int TeacherId { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public string TeacherEmail { get; set; } = string.Empty;
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public string BatchName { get; set; } = string.Empty;
        public string TimeSlotName { get; set; } = string.Empty;
    }
}
