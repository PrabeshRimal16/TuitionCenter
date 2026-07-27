using System.Collections.Generic;

namespace TuitionCenter.Models
{
    public class SubjectPickerViewModel
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; } = "";
        public List<SubjectPickerItem> Subjects { get; set; } = new();
    }

    public class SubjectPickerItem
    {
        public int SubjectId { get; set; }
        public string Name { get; set; } = "";
        public string IconText { get; set; } = "";
        public double Rating { get; set; }
        public string TeacherName { get; set; } = "";
        public string Description { get; set; } = "";
        public int SeatsLeft { get; set; }
        public string? Badge { get; set; }
        public string BadgeStyle { get; set; } = "recommended";
        public bool IsSelected { get; set; }
    }
}