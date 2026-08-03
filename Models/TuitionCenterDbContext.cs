using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace TuitionCenter.Models;

public partial class TuitionCenterDbContext : DbContext
{
    public TuitionCenterDbContext()
    {
    }

    public TuitionCenterDbContext(DbContextOptions<TuitionCenterDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Announcement> Announcements { get; set; }

    public virtual DbSet<Batch> Batches { get; set; }

    public virtual DbSet<Class> Classes { get; set; }

    public virtual DbSet<ClassSession> ClassSessions { get; set; }

    public virtual DbSet<CourseFee> CourseFees { get; set; }

    public virtual DbSet<CourseType> CourseTypes { get; set; }

    public virtual DbSet<Enrollment> Enrollments { get; set; }

    public virtual DbSet<EnrollmentSubject> EnrollmentSubjects { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<StudentProfile> StudentProfiles { get; set; }

    public virtual DbSet<Subject> Subjects { get; set; }

    public virtual DbSet<TimeSlot> TimeSlots { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Name=DbConn");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Announcement>(entity =>
        {
            entity.HasKey(e => e.AnnouncementId).HasName("PK__Announce__9DE44574E23C00CA");

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Title).HasMaxLength(150);

            entity.HasOne(d => d.Batch).WithMany(p => p.Announcements)
                .HasForeignKey(d => d.BatchId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Announcem__Batch__0D7A0286");

            entity.HasOne(d => d.Teacher).WithMany(p => p.Announcements)
                .HasForeignKey(d => d.TeacherId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Announcem__Teach__0E6E26BF");
        });

        modelBuilder.Entity<Batch>(entity =>
        {
            entity.HasKey(e => e.BatchId).HasName("PK__Batches__5D55CE5842BBA9AE");

            entity.Property(e => e.BatchName).HasMaxLength(100);
            entity.Property(e => e.Capacity).HasDefaultValue(10);
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.Class).WithMany(p => p.Batches)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Batches__ClassId__6754599E");

            entity.HasOne(d => d.CourseType).WithMany(p => p.Batches)
                .HasForeignKey(d => d.CourseTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Batches__CourseT__6B24EA82");

            entity.HasOne(d => d.Subject).WithMany(p => p.Batches)
                .HasForeignKey(d => d.SubjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Batches__Subject__68487DD7");

            entity.HasOne(d => d.Teacher).WithMany(p => p.Batches)
                .HasForeignKey(d => d.TeacherId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Batches__Teacher__693CA210");

            entity.HasOne(d => d.TimeSlot).WithMany(p => p.Batches)
                .HasForeignKey(d => d.TimeSlotId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Batches__TimeSlo__6A30C649");
        });

        modelBuilder.Entity<Class>(entity =>
        {
            entity.HasKey(e => e.ClassId).HasName("PK__Classes__CB1927C0CECB38F5");

            entity.Property(e => e.ClassName).HasMaxLength(50);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<ClassSession>(entity =>
        {
            entity.HasKey(e => e.SessionId).HasName("PK__ClassSes__C9F49290F7589519");

            entity.Property(e => e.MeetingLink).HasMaxLength(255);
            entity.Property(e => e.Notes).HasMaxLength(255);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Upcoming");
            entity.Property(e => e.Title).HasMaxLength(150);

            entity.HasOne(d => d.Batch).WithMany(p => p.ClassSessions)
                .HasForeignKey(d => d.BatchId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ClassSess__Batch__07C12930");

            entity.HasOne(d => d.Teacher).WithMany(p => p.ClassSessions)
                .HasForeignKey(d => d.TeacherId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ClassSess__Teach__08B54D69");
        });

        modelBuilder.Entity<CourseFee>(entity =>
        {
            entity.HasKey(e => e.FeeId).HasName("PK__CourseFe__B387B229CC3AE129");

            entity.HasIndex(e => new { e.ClassId, e.SubjectId, e.CourseTypeId }, "UQ_CourseFees").IsUnique();

            entity.Property(e => e.Amount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.Class).WithMany(p => p.CourseFees)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CourseFee__Class__5FB337D6");

            entity.HasOne(d => d.CourseType).WithMany(p => p.CourseFees)
                .HasForeignKey(d => d.CourseTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CourseFee__Cours__619B8048");

            entity.HasOne(d => d.Subject).WithMany(p => p.CourseFees)
                .HasForeignKey(d => d.SubjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CourseFee__Subje__60A75C0F");
        });

        modelBuilder.Entity<CourseType>(entity =>
        {
            entity.HasKey(e => e.CourseTypeId).HasName("PK__CourseTy__8173697234F32148");

            entity.Property(e => e.TypeName).HasMaxLength(50);
        });

        modelBuilder.Entity<Enrollment>(entity =>
        {
            entity.HasKey(e => e.EnrollmentId).HasName("PK__Enrollme__7F68771BFC928554");

            entity.HasIndex(e => e.EnrollmentNumber, "UQ__Enrollme__30BA515B36376B43").IsUnique();

            entity.Property(e => e.ApprovalDate).HasColumnType("datetime");
            entity.Property(e => e.EnrolledDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.EnrollmentNumber).HasMaxLength(20);
            entity.Property(e => e.ExpectedAmount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.RejectionReason).HasMaxLength(255);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Pending");

            entity.HasOne(d => d.ApprovedByNavigation).WithMany(p => p.EnrollmentApprovedByNavigations)
                .HasForeignKey(d => d.ApprovedBy)
                .HasConstraintName("FK__Enrollmen__Appro__76969D2E");

            entity.HasOne(d => d.Class).WithMany(p => p.Enrollments)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Enrollmen__Class__71D1E811");

            entity.HasOne(d => d.CourseType).WithMany(p => p.Enrollments)
                .HasForeignKey(d => d.CourseTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Enrollmen__Cours__72C60C4A");

            entity.HasOne(d => d.PreferredTimeSlot).WithMany(p => p.Enrollments)
                .HasForeignKey(d => d.PreferredTimeSlotId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Enrollmen__Prefe__73BA3083");

            entity.HasOne(d => d.Student).WithMany(p => p.EnrollmentStudents)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Enrollmen__Stude__70DDC3D8");
        });

        modelBuilder.Entity<EnrollmentSubject>(entity =>
        {
            entity.HasKey(e => e.EnrollmentSubjectId).HasName("PK__Enrollme__7BC2D37E5F541AED");

            entity.HasIndex(e => new { e.EnrollmentId, e.SubjectId }, "UQ_EnrollmentSubject").IsUnique();

            entity.HasOne(d => d.AssignedBatch).WithMany(p => p.EnrollmentSubjects)
                .HasForeignKey(d => d.AssignedBatchId)
                .HasConstraintName("FK__Enrollmen__Assig__7D439ABD");

            entity.HasOne(d => d.Enrollment).WithMany(p => p.EnrollmentSubjects)
                .HasForeignKey(d => d.EnrollmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Enrollmen__Enrol__7B5B524B");

            entity.HasOne(d => d.Subject).WithMany(p => p.EnrollmentSubjects)
                .HasForeignKey(d => d.SubjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Enrollmen__Subje__7C4F7684");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__Notifica__20CF2E1218E107FF");

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsRead).HasDefaultValue(false);
            entity.Property(e => e.Message).HasMaxLength(255);
            entity.Property(e => e.Type).HasMaxLength(50);

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Notificat__UserI__123EB7A3");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payments__9B556A38086926D1");

            entity.Property(e => e.Amount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.ApprovalDate).HasColumnType("datetime");
            entity.Property(e => e.Method).HasMaxLength(20);
            entity.Property(e => e.PaymentDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Remarks).HasMaxLength(255);
            entity.Property(e => e.ScreenshotPath).HasMaxLength(255);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Pending");
            entity.Property(e => e.TransactionId).HasMaxLength(100);

            entity.HasOne(d => d.ApprovedByNavigation).WithMany(p => p.Payments)
                .HasForeignKey(d => d.ApprovedBy)
                .HasConstraintName("FK__Payments__Approv__04E4BC85");

            entity.HasOne(d => d.Enrollment).WithMany(p => p.Payments)
                .HasForeignKey(d => d.EnrollmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Payments__Enroll__00200768");
        });

        modelBuilder.Entity<StudentProfile>(entity =>
        {
            entity.HasKey(e => e.StudentProfileId).HasName("PK__StudentP__222BD0B0E6498C6A");

            entity.HasIndex(e => e.UserId, "UQ__StudentP__1788CC4DDB7B743E").IsUnique();

            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.EmergencyContact).HasMaxLength(20);
            entity.Property(e => e.ParentContact).HasMaxLength(20);
            entity.Property(e => e.ParentName).HasMaxLength(150);
            entity.Property(e => e.RegisteredDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.SchoolName).HasMaxLength(150);

            entity.HasOne(d => d.User).WithOne(p => p.StudentProfile)
                .HasForeignKey<StudentProfile>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__StudentPr__UserI__5165187F");
        });

        modelBuilder.Entity<Subject>(entity =>
        {
            entity.HasKey(e => e.SubjectId).HasName("PK__Subjects__AC1BA3A827029A6D");

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.SubjectName).HasMaxLength(100);

            entity.HasOne(d => d.Class).WithMany(p => p.Subjects)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Subjects__ClassI__59063A47");
        });

        modelBuilder.Entity<TimeSlot>(entity =>
        {
            entity.HasKey(e => e.TimeSlotId).HasName("PK__TimeSlot__41CC1F32632EF260");

            entity.Property(e => e.Days).HasMaxLength(50);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4C36DFA1CD");

            entity.HasIndex(e => e.Email, "UQ__Users__A9D105340CDDDBF7").IsUnique();

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.FullName).HasMaxLength(150);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Role).HasMaxLength(20);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
