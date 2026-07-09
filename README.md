# 🎓 Tuition Center Management System

A modern **Tuition Center Management System** built using **ASP.NET Core MVC** that streamlines the management of students, teachers, classes, courses, schedules, and online learning.

## 📌 Overview

This project is designed to simplify the daily operations of a tuition center by providing dedicated dashboards for **Admin**, **Teachers**, and **Students**. It enables course management, class scheduling, student enrollment, attendance tracking, and online class integration.

---

## 🚀 Features

### 👨‍💼 Admin
- Secure Login
- Dashboard Overview
- Manage Teachers
- Manage Students
- Manage Courses & Subjects
- Manage Classes
- Create Timetables
- Assign Teachers to Classes
- Track Student Enrollments
- View Reports
- Manage User Accounts

### 👨‍🏫 Teacher
- Secure Login
- View Assigned Classes
- View Student List
- Share Online Class Links
- Upload Study Materials
- Mark Attendance
- View Teaching Schedule

### 👨‍🎓 Student
- Register & Login
- Browse Available Courses
- Enroll in Courses
- View Timetable
- View Course Details
- Access Online Class Links
- Download Study Materials
- Track Attendance

---

## 🛠️ Tech Stack

### Frontend
- HTML5
- CSS3
- Bootstrap 5
- JavaScript
- Razor Views

### Backend
- ASP.NET Core MVC
- C#
- Entity Framework Core

### Database
- Microsoft SQL Server

### Authentication
- ASP.NET Core Identity
- Role-Based Authorization

---

## 📂 Project Structure

```
TuitionCenter/
│
├── Controllers/
├── Models/
├── Views/
├── wwwroot/
│   ├── css/
│   ├── js/
│   └── images/
├── Data/
├── Security/
├── Properties/
├── appsettings.json
├── Program.cs
└── README.md
```

---

## 👥 User Roles

| Role | Permissions |
|------|-------------|
| Admin | Full system control |
| Teacher | Manage assigned classes, attendance, materials, and online classes |
| Student | Register, enroll, view schedules, attend online classes |

---

## 💾 Database

- SQL Server
- Entity Framework Core
- Database-First Approach
- Scaffolded Models

---

## 🔑 Main Modules

- Authentication & Authorization
- Student Management
- Teacher Management
- Course Management
- Subject Management
- Class Management
- Timetable Management
- Attendance Management
- Online Class Management
- Dashboard & Reports

---

## ⚙️ Installation

### Clone Repository

```bash
git clone https://github.com/yourusername/TuitionCenter.git
```

### Navigate to Project

```bash
cd TuitionCenter
```

### Restore Packages

```bash
dotnet restore
```

### Update Connection String

Modify the `appsettings.json` file:

```json
"ConnectionStrings": {
    "DbConn": "Your SQL Server Connection String"
}
```

### Run the Project

```bash
dotnet run
```

---

## 📸 Screenshots

Coming Soon...

---

## 🔮 Future Enhancements

- Online Payment Integration
- Email Notifications
- SMS Notifications
- Assignment Submission
- Quiz & Exam Module
- Student Performance Analytics
- Certificate Generation
- Parent Portal
- Mobile Responsive Dashboard

