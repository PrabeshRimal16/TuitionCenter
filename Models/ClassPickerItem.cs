namespace TuitionCenter.Models
{
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