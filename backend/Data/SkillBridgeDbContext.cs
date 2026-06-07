using Microsoft.EntityFrameworkCore;
using SkillBridge.Api.Models;

namespace SkillBridge.Api.Data;

public sealed class SkillBridgeDbContext(DbContextOptions<SkillBridgeDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<StudentProfile> StudentProfiles => Set<StudentProfile>();
    public DbSet<TeacherProfile> TeacherProfiles => Set<TeacherProfile>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<TeacherAvailability> TeacherAvailabilities => Set<TeacherAvailability>();
    public DbSet<ScheduleChangeRequest> ScheduleChangeRequests => Set<ScheduleChangeRequest>();
    public DbSet<ConversationHide> ConversationHides => Set<ConversationHide>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Withdrawal> Withdrawals => Set<Withdrawal>();
    public DbSet<TeacherReview> TeacherReviews => Set<TeacherReview>();
    public DbSet<Complaint> Complaints => Set<Complaint>();
    public DbSet<PaymentInvoice> PaymentInvoices => Set<PaymentInvoice>();
    public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(120);
            entity.Property(x => x.Email).HasMaxLength(180);
            entity.Property(x => x.PasswordHash).HasMaxLength(128);
            entity.Property(x => x.AvatarUrl).HasMaxLength(500);
            entity.Property(x => x.Bio).HasMaxLength(1000);
            entity.Property(x => x.Role).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.IsLocked).HasDefaultValue(false);
        });

        modelBuilder.Entity<TeacherProfile>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Skill).HasMaxLength(160);
            entity.Property(x => x.Description).HasMaxLength(1200);
            entity.Property(x => x.Experience).HasMaxLength(1200);
            entity.Property(x => x.PricePerSession).HasColumnType("decimal(18,2)");
            entity.Property(x => x.OfflineLocation).HasMaxLength(300);
            entity.Property(x => x.PortfolioImageUrl).HasMaxLength(700);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.Rating).HasColumnType("decimal(3,2)");
            entity.Property(x => x.TeachingMode).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.DefaultPayoutMethod).HasMaxLength(80).HasDefaultValue("Tài khoản ngân hàng");
            entity.Property(x => x.DefaultPayoutBank).HasMaxLength(80).HasDefaultValue("Vietcombank");
        });

        modelBuilder.Entity<StudentProfile>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.UserId).IsUnique();
            entity.Property(x => x.DisplayName).HasMaxLength(120);
            entity.Property(x => x.Email).HasMaxLength(180);
            entity.Property(x => x.Gender).HasMaxLength(40);
            entity.Property(x => x.DateOfBirth).HasColumnType("date");
            entity.Property(x => x.Phone).HasMaxLength(40);
            entity.Property(x => x.LearningGoal).HasMaxLength(700);
            entity.Property(x => x.Bio).HasMaxLength(1000);
            entity.Property(x => x.AvatarUrl).HasMaxLength(500);
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.MeetingRoomId).HasMaxLength(120);
            entity.Property(x => x.TeachingMode).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.OfflineLocation).HasMaxLength(300);
            entity.Property(x => x.LessonContent).HasMaxLength(500);
        });

        modelBuilder.Entity<TeacherAvailability>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.DayOfWeek).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.StartTime).HasColumnType("time");
            entity.Property(x => x.EndTime).HasColumnType("time");
            entity.Property(x => x.PlannedContent).HasMaxLength(500);
            entity.Property(x => x.TeachingMode).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.OfflineLocation).HasMaxLength(300);
        });

        modelBuilder.Entity<ScheduleChangeRequest>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.Reason).HasMaxLength(500);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Content).HasMaxLength(2000);
            entity.Property(x => x.IsRead).HasDefaultValue(false);
            entity.Property(x => x.IsDeleted).HasDefaultValue(false);
        });

        modelBuilder.Entity<ConversationHide>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.UserId, x.ContactUserId }).IsUnique();
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Icon).HasMaxLength(16);
            entity.Property(x => x.Title).HasMaxLength(220);
            entity.Property(x => x.Body).HasMaxLength(600);
            entity.Property(x => x.IsRead).HasDefaultValue(false).ValueGeneratedNever();
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        });

        modelBuilder.Entity<Withdrawal>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.Method).HasMaxLength(80).HasDefaultValue("Tài khoản ngân hàng");
            entity.Property(x => x.AccountName).HasMaxLength(120).HasDefaultValue("");
            entity.Property(x => x.AccountNumber).HasMaxLength(120).HasDefaultValue("");
            entity.Property(x => x.BankName).HasMaxLength(120).HasDefaultValue("");
        });

        modelBuilder.Entity<TeacherReview>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.BookingId).IsUnique();
            entity.Property(x => x.Stars);
            entity.Property(x => x.Comment).HasMaxLength(1000);
        });

        modelBuilder.Entity<Complaint>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.BookingId).IsUnique();
            entity.Property(x => x.Reason).HasMaxLength(1000);
            entity.Property(x => x.StudentEvidenceUrl).HasMaxLength(700);
            entity.Property(x => x.TeacherResponse).HasMaxLength(1200);
            entity.Property(x => x.TeacherEvidenceUrl).HasMaxLength(700);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(40);
        });

        modelBuilder.Entity<PaymentInvoice>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.BookingId).IsUnique();
            entity.Property(x => x.InvoiceCode).HasMaxLength(40);
            entity.Property(x => x.LessonContent).HasMaxLength(500);
            entity.Property(x => x.TeachingMode).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            entity.Property(x => x.PaymentMethod).HasMaxLength(80);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        });

        modelBuilder.Entity<WalletTransaction>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.Method).HasMaxLength(80);
            entity.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            entity.Property(x => x.Status).HasMaxLength(80);
            entity.Property(x => x.Note).HasMaxLength(500);
            entity.Property(x => x.TransactionCode).HasMaxLength(40).HasDefaultValue("");
        });
    }
}
