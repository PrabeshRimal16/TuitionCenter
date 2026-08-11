using System;

namespace TuitionCenter.Models
{
    public class PaymentsViewModel
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; } = "";
        public string CourseTypeName { get; set; } = "";
        public string SubjectsSummary { get; set; } = "";
        public decimal Total { get; set; }
    }

    public class EnrollmentConfirmationViewModel
    {
        public string EnrollmentNumber { get; set; } = "";
        public string ClassName { get; set; } = "";
        public string Total { get; set; } = "0";
    }
}
