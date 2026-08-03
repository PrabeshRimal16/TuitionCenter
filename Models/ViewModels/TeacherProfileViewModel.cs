using System.Collections.Generic;

namespace TuitionCenter.Models.ViewModels
{
    public class TeacherProfileViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string DateOfBirth { get; set; } = string.Empty;
        public string HighestDegree { get; set; } = string.Empty;
        public string Institution { get; set; } = string.Empty;
        public string YearOfGraduation { get; set; } = string.Empty;
        public string SubjectSpecialization { get; set; } = string.Empty;
        public string YearsOfExperience { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;

        public string PhotoPath { get; set; } = string.Empty;

        // Mocked data for display
        public int CurrentLoad { get; set; } = 5;
        public int TotalStudents { get; set; } = 142;
        public decimal Rating { get; set; } = 4.9m;
        public bool IsVerified { get; set; } = true;

        public List<string> TeachingLevels { get; set; } = new List<string> { "Grade 10", "Grade 12", "Bachelor's" };
        public List<AssignedSubjectGroup> AssignedSubjects { get; set; } = new List<AssignedSubjectGroup>
        {
            new AssignedSubjectGroup { ClassName = "Grade 10", Subjects = "Mathematics, Optional Mathematics" },
            new AssignedSubjectGroup { ClassName = "Grade 12", Subjects = "Advanced Calculus, Statistics" }
        };
    }

    public class AssignedSubjectGroup
    {
        public string ClassName { get; set; } = string.Empty;
        public string Subjects { get; set; } = string.Empty;
    }
}