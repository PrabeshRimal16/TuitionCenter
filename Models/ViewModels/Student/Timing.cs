using System.ComponentModel.DataAnnotations;

namespace Tution_Center.Models
{
    public class Timing
    {
        public int Id { get; set; }

        public string CourseName { get; set; }

        public string Day { get; set; }

        public string Time { get; set; }

        public string Teacher { get; set; }
    }
}