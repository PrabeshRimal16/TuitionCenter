using System.ComponentModel.DataAnnotations;

namespace Tution_Center.Models
{
    public class Pricing
    {
        public int Id { get; set; }

        [Required]
        public string CourseName { get; set; }

        [Required]
        public decimal Fee { get; set; }

        public string Duration { get; set; }
    }
}