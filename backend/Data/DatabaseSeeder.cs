using Microsoft.EntityFrameworkCore;
using SkillBridge.Api.Models;
using SkillBridge.Api.Services;

namespace SkillBridge.Api.Data;

public static class DatabaseSeeder
{
    public static void Seed(SkillBridgeDbContext db)
    {
        var student = AddUser(db, "Linh Tran", "student@skillbridge.dev", UserRole.Student);
        var minh = AddUser(db, "Minh Pham", "minh@skillbridge.dev", UserRole.Teacher);
        var an = AddUser(db, "An Nguyen", "an@skillbridge.dev", UserRole.Teacher);
        var quyen = AddUser(db, "Quyen Le", "quyen@skillbridge.dev", UserRole.Teacher);
        var duy = AddUser(db, "Duy Hoang", "duy@skillbridge.dev", UserRole.Teacher);
        AddUser(db, "Admin SkillBridge", "admin1@skillbridge.com", UserRole.Admin, "333333");

        AddTeacherProfile(db, minh, "Giao tiếp tiếng Anh", "Luyện nói tiếng Anh cho học tập, công việc và phỏng vấn.", "5 năm dạy giao tiếp tiếng Anh và luyện nói IELTS.", 200000, TeachingMode.Online, "", true, 4.9m);
        AddTeacherProfile(db, an, "Lập trình web C#", "Xây dựng ứng dụng web thực tế với mô hình MVC và cơ sở dữ liệu.", "Lập trình viên backend, từng hỗ trợ giảng dạy các khóa .NET.", 250000, TeachingMode.Hybrid, "Quận 1, TP. Hồ Chí Minh", true, 4.8m);
        AddTeacherProfile(db, quyen, "Cơ bản về thiết kế giao diện", "Học bố cục, chữ viết và cách thiết kế giao diện web dễ dùng.", "Nhà thiết kế sản phẩm hướng dẫn học viên làm hồ sơ năng lực.", 180000, TeachingMode.Online, "", false, 4.7m);
        AddTeacherProfile(db, duy, "Excel cho công việc", "Dùng Excel để làm báo cáo, công thức và bảng điều khiển đơn giản.", "Chuyên viên tài chính có kinh nghiệm làm báo cáo doanh nghiệp.", 150000, TeachingMode.Offline, "Cầu Giấy, Hà Nội", true, 4.8m);

        AddStudentWalletSeed(db, student.Id);

        db.SaveChanges();
    }

    private static User AddUser(SkillBridgeDbContext db, string name, string email, UserRole role, string password = "password")
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var existing = db.Users.FirstOrDefault(user => user.Email == normalizedEmail);
        if (existing is not null)
        {
            db.Entry(existing).State = EntityState.Detached;
            db.Users.Update(existing with
            {
                Name = name,
                PasswordHash = SkillBridgeService.HashPassword(password),
                Bio = role == UserRole.Teacher ? "Giáo viên có kinh nghiệm, tập trung vào kết quả thực tế." : "Học viên đang xây dựng kỹ năng mới."
            });
            return existing;
        }

        var user = new User(
            Guid.NewGuid(),
            name,
            normalizedEmail,
            SkillBridgeService.HashPassword(password),
            role,
            $"https://api.dicebear.com/8.x/initials/svg?seed={Uri.EscapeDataString(name)}",
            role == UserRole.Teacher ? "Giáo viên có kinh nghiệm, tập trung vào kết quả thực tế." : "Học viên đang xây dựng kỹ năng mới.",
            DateTimeOffset.UtcNow);

        db.Users.Add(user);
        return user;
    }

    private static void AddTeacherProfile(
        SkillBridgeDbContext db,
        User teacher,
        string skill,
        string description,
        string experience,
        decimal pricePerSession,
        TeachingMode teachingMode,
        string offlineLocation,
        bool isOnline,
        decimal rating)
    {
        var existing = db.TeacherProfiles.FirstOrDefault(profile => profile.UserId == teacher.Id);
        if (existing is null)
        {
            db.TeacherProfiles.Add(new TeacherProfile(
                Guid.NewGuid(),
                teacher.Id,
                skill,
                description,
                experience,
                pricePerSession,
                teachingMode,
                offlineLocation,
                "",
                TeacherProfileStatus.Active,
                rating,
                isOnline));
        }
        else
        {
            db.Entry(existing).State = EntityState.Detached;
            db.TeacherProfiles.Update(existing with
            {
                Skill = skill,
                Description = description,
                Experience = experience,
                PricePerSession = pricePerSession,
                TeachingMode = teachingMode,
                OfflineLocation = offlineLocation,
                Status = TeacherProfileStatus.Active,
                Rating = rating,
                IsOnline = isOnline
            });
        }

        AddDefaultAvailability(db, teacher.Id, DayOfWeek.Monday, new TimeSpan(8, 0, 0), new TimeSpan(11, 0, 0));
        AddDefaultAvailability(db, teacher.Id, DayOfWeek.Wednesday, new TimeSpan(14, 0, 0), new TimeSpan(17, 0, 0));
        AddDefaultAvailability(db, teacher.Id, DayOfWeek.Saturday, new TimeSpan(9, 0, 0), new TimeSpan(12, 0, 0));
    }

    private static void AddDefaultAvailability(SkillBridgeDbContext db, Guid teacherId, DayOfWeek dayOfWeek, TimeSpan startTime, TimeSpan endTime)
    {
        var exists = db.TeacherAvailabilities.Any(slot =>
            slot.TeacherId == teacherId &&
            slot.DayOfWeek == dayOfWeek &&
            slot.StartTime == startTime &&
            slot.EndTime == endTime);

        if (exists)
        {
            return;
        }

        var profile = db.TeacherProfiles.AsNoTracking().FirstOrDefault(item => item.UserId == teacherId);
        db.TeacherAvailabilities.Add(new TeacherAvailability(
            Guid.NewGuid(),
            teacherId,
            dayOfWeek,
            startTime,
            endTime,
            true,
            "Ôn tập mục tiêu cá nhân và luyện bài thực hành.",
            profile?.TeachingMode ?? TeachingMode.Online,
            profile?.TeachingMode == TeachingMode.Online ? "" : profile?.OfflineLocation ?? ""));
    }

    private static void AddStudentWalletSeed(SkillBridgeDbContext db, Guid studentId)
    {
        if (db.WalletTransactions.Any(transaction => transaction.UserId == studentId && transaction.Type == WalletTransactionType.TopUp))
        {
            return;
        }

        db.WalletTransactions.Add(new WalletTransaction(
            Guid.NewGuid(),
            studentId,
            WalletTransactionType.TopUp,
            "Số dư demo",
            1000000m,
            "Hoàn thành",
            DateTimeOffset.UtcNow,
            null,
            null,
            "Số dư ví demo ban đầu",
            "DEP-DEMO-0001"));
    }
}
