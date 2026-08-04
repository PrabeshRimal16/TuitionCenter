using System;
using System.Collections.Generic;

namespace TuitionCenter.Models.ViewModels
{
    public class TeacherBatchItemViewModel
    {
        public int BatchId { get; set; }
        public string BatchName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string TimeSlot { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public int EnrolledStudentsCount { get; set; }
        public string AcademicLevel { get; set; } = string.Empty; // secondary, class10, class12, bachelors
        public string AcademicLevelBadge { get; set; } = string.Empty; // SECONDARY, HIGHER SECONDARY, UNIVERSITY
        public string Shift { get; set; } = "morning"; // morning, afternoon, evening
        public bool IsActive { get; set; } = true;
    }

    public class TeacherBatchManagementViewModel
    {
        public string TeacherName { get; set; } = string.Empty;
        public string TeacherEmail { get; set; } = string.Empty;
        public List<TeacherBatchItemViewModel> Batches { get; set; } = new();
    }

    public class TeacherStudentItemViewModel
    {
        public int StudentId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string BatchName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public DateTime EnrollmentDate { get; set; }
        public double AttendancePercentage { get; set; } = 92.5;
        public string Status { get; set; } = "Active";
    }

    public class TeacherStudentsViewModel
    {
        public string TeacherName { get; set; } = string.Empty;
        public string TeacherEmail { get; set; } = string.Empty;
        public int SelectedBatchId { get; set; }
        public List<TeacherBatchItemViewModel> TeacherBatches { get; set; } = new();
        public List<TeacherStudentItemViewModel> Students { get; set; } = new();
    }

    public class AttendanceStudentItemViewModel
    {
        public int StudentId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string RollNo { get; set; } = string.Empty;
        public bool IsPresent { get; set; } = true;
        public string Remarks { get; set; } = string.Empty;
    }

    public class TeacherAttendanceViewModel
    {
        public string TeacherName { get; set; } = string.Empty;
        public string TeacherEmail { get; set; } = string.Empty;
        public int SelectedSessionId { get; set; }
        public string SessionTitle { get; set; } = string.Empty;
        public string BatchName { get; set; } = string.Empty;
        public DateOnly SessionDate { get; set; }
        public List<UpcomingClassViewModel> TeacherSessions { get; set; } = new();
        public List<AttendanceStudentItemViewModel> EnrolledStudents { get; set; } = new();
    }

    public class LiveClassesViewModel
    {
        public string TeacherName { get; set; } = string.Empty;
        public string TeacherEmail { get; set; } = string.Empty;
        public List<UpcomingClassViewModel> ActiveLiveSessions { get; set; } = new();
        public List<UpcomingClassViewModel> UpcomingLiveSessions { get; set; } = new();
    }

    public class MarkAttendanceRequest
    {
        public int SessionId { get; set; }
        public List<StudentAttendanceItem> AttendanceList { get; set; } = new();
    }

    public class StudentAttendanceItem
    {
        public int StudentId { get; set; }
        public bool IsPresent { get; set; }
        public string Remarks { get; set; } = string.Empty;
    }

    public class UpdateMeetingLinkRequest
    {
        public int SessionId { get; set; }
        public int BatchId { get; set; }
        public string MeetingLink { get; set; } = string.Empty;
    }
}
