using System.Collections.Generic;

namespace TuitionCenter.Models
{
    public class PricingViewModel
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; } = "";
        public string CourseTypeName { get; set; } = "";
        public List<PricingLineItem> Items { get; set; } = new();
        public decimal Total { get; set; }
        public bool HasMissingFees { get; set; }
    }

    public class PricingLineItem
    {
        public string SubjectName { get; set; } = "";
        public decimal? Amount { get; set; }
    }
}