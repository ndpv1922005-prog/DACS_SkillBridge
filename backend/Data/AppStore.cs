using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using SkillBridge.Api.Models;

namespace SkillBridge.Api.Data;

public sealed class AppStore
{
    private readonly ConcurrentDictionary<Guid, User> _users = new();
    private readonly ConcurrentDictionary<Guid, TeacherProfile> _teachers = new();
    private readonly ConcurrentDictionary<Guid, Booking> _bookings = new();
    private readonly ConcurrentDictionary<Guid, Message> _messages = new();
    private readonly ConcurrentDictionary<Guid, Transaction> _transactions = new();

    public AppStore()
    {
        Seed();
    }

    public IEnumerable<User> Users => _users.Values;
    public IEnumerable<TeacherProfile> Teachers => _teachers.Values;
    public IEnumerable<Booking> Bookings => _bookings.Values;
    public IEnumerable<Message> Messages => _messages.Values;
    public IEnumerable<Transaction> Transactions => _transactions.Values;

    public User AddUser(string name, string email, string password, UserRole role)
    {
        if (_users.Values.Any(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Email already exists.");
        }

        var user = new User(
            Guid.NewGuid(),
            name.Trim(),
            email.Trim().ToLowerInvariant(),
            HashPassword(password),
            role,
            $"https://api.dicebear.com/8.x/initials/svg?seed={Uri.EscapeDataString(name)}",
            role == UserRole.Teacher ? "Experienced mentor focused on practical outcomes." : "Curious learner building real skills.",
            DateTimeOffset.UtcNow);

        _users[user.Id] = user;
        return user;
    }

    public User? FindUserByEmail(string email) =>
        _users.Values.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));

    public User? FindUser(Guid id) => _users.GetValueOrDefault(id);
    public TeacherProfile? FindTeacher(Guid id) => _teachers.GetValueOrDefault(id);
    public Booking? FindBooking(Guid id) => _bookings.GetValueOrDefault(id);

    public TeacherProfile AddTeacherProfile(Guid userId, string skill, string description, decimal price, TeachingMode mode)
    {
        var profile = new TeacherProfile(Guid.NewGuid(), userId, skill, description, "", price, mode, "", "", TeacherProfileStatus.Active, 4.8m, true, "Tài khoản ngân hàng", "Vietcombank");
        _teachers[profile.Id] = profile;
        return profile;
    }

    public Booking UpsertBooking(Booking booking)
    {
        _bookings[booking.Id] = booking;
        return booking;
    }

    public Transaction AddTransaction(Guid bookingId, decimal amount, TransactionType type, TransactionStatus status)
    {
        var transaction = new Transaction(Guid.NewGuid(), bookingId, amount, type, status, DateTimeOffset.UtcNow);
        _transactions[transaction.Id] = transaction;
        return transaction;
    }

    public Message AddMessage(Guid senderId, Guid receiverId, string content)
    {
        var message = new Message(Guid.NewGuid(), senderId, receiverId, content.Trim(), DateTimeOffset.UtcNow, false, false);
        _messages[message.Id] = message;
        return message;
    }

    public static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }

    private void Seed()
    {
        var student = AddUser("Linh Tran", "student@skillbridge.dev", "password", UserRole.Student);
        var teachers = new[]
        {
            AddUser("Minh Pham", "minh@skillbridge.dev", "password", UserRole.Teacher),
            AddUser("An Nguyen", "an@skillbridge.dev", "password", UserRole.Teacher),
            AddUser("Quyen Le", "quyen@skillbridge.dev", "password", UserRole.Teacher),
            AddUser("Duy Hoang", "duy@skillbridge.dev", "password", UserRole.Teacher)
        };

        AddTeacherProfile(teachers[0].Id, "IELTS Speaking", "Mock interviews, pronunciation correction, and band-score feedback.", 180000, TeachingMode.Online);
        AddTeacherProfile(teachers[1].Id, "React TypeScript", "Build production-style interfaces with clean components and state management.", 250000, TeachingMode.Hybrid);
        AddTeacherProfile(teachers[2].Id, "Guitar Acoustic", "Beginner-friendly offline sessions focused on chords, rhythm, and songs.", 150000, TeachingMode.Offline);
        AddTeacherProfile(teachers[3].Id, "Data Analytics", "Excel, SQL, dashboards, and business case practice for entry-level analysts.", 220000, TeachingMode.Online);

        AddMessage(student.Id, teachers[1].Id, "Em muốn học React theo project thực tế.");
        AddMessage(teachers[1].Id, student.Id, "Được nhé, mình có thể bắt đầu bằng component architecture.");
    }
}
