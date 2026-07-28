using System.Collections.Generic;

namespace TuitionCenter.Models
{
    public class ClassmatesViewModel
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; } = "";
        public List<ClassmateItem> Classmates { get; set; } = new();
    }

    public class ClassmateItem
    {
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string EnrolledDateLabel { get; set; } = "";
        public string Status { get; set; } = "";
    }
}