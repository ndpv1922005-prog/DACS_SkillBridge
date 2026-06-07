using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SkillBridge.Api.DTOs;
using SkillBridge.Api.Data;
using SkillBridge.Api.Models;
using SkillBridge.Api.ViewModels;

namespace SkillBridge.Api.Services;

public sealed class SkillBridgeService(SkillBridgeDbContext db)
{
    public AuthResponse Register(AuthRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ArgumentException("ERR_REQUIRED_REGISTER");
        }

        var email = request.Email.Trim().ToLowerInvariant();
        if (db.Users.Any(u => u.Email == email))
        {
            throw new InvalidOperationException("ERR_EMAIL_EXISTS");
        }

        if (request.Role == UserRole.Admin)
        {
            throw new InvalidOperationException("Không thể đăng ký tài khoản Admin từ giao diện.");
        }

        if (request.Password.Length < 6)
        {
            throw new ArgumentException("Mật khẩu phải có ít nhất 6 ký tự.");
        }

        var user = new User(
            Guid.NewGuid(),
            request.Name.Trim(),
            email,
            HashPassword(request.Password),
            request.Role,
            $"https://api.dicebear.com/8.x/initials/svg?seed={Uri.EscapeDataString(request.Name.Trim())}",
            request.Role == UserRole.Teacher ? "Giáo viên có kinh nghiệm, tập trung vào kết quả thực tế." : "Học viên đang xây dựng kỹ năng mới.",
            DateTimeOffset.UtcNow);

        db.Users.Add(user);

        db.SaveChanges();
        return ToAuth(user);
    }

    public AuthResponse Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = db.Users.FirstOrDefault(u => u.Email == email)
            ?? throw new InvalidOperationException("ERR_INVALID_LOGIN");

        if (user.PasswordHash != HashPassword(request.Password))
        {
            throw new InvalidOperationException("ERR_INVALID_LOGIN");
        }

        if (user.IsLocked)
        {
            throw new InvalidOperationException("Tài khoản của bạn đang bị khóa. Vui lòng liên hệ hỗ trợ.");
        }

        return ToAuth(user);
    }

    public IEnumerable<TeacherResponse> SearchTeachers(string? query, TeachingMode? mode)
    {
        var teachers = db.TeacherProfiles
            .AsNoTracking()
            .Where(t => t.Status == TeacherProfileStatus.Active)
            .AsEnumerable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            teachers = teachers.Where(t =>
                t.Skill.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                t.Description.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        if (mode is not null)
        {
            teachers = teachers.Where(t => t.TeachingMode == mode.Value);
        }

        var users = db.Users.AsNoTracking().ToDictionary(u => u.Id);
        return teachers
            .OrderByDescending(t => t.Rating)
            .Select(t => ToTeacherResponse(t, users[t.UserId]))
            .ToList();
    }

    public Booking CreateBooking(BookingRequest request)
    {
        var student = db.Users.AsNoTracking().FirstOrDefault(u => u.Id == request.StudentId)
            ?? throw new InvalidOperationException("Vui lòng đăng nhập bằng tài khoản học viên để đặt lịch.");
        if (student.Role != UserRole.Student)
        {
            throw new InvalidOperationException("Chỉ học viên mới được tạo lịch học.");
        }

        var teacherProfile = db.TeacherProfiles.FirstOrDefault(t => t.Id == request.TeacherProfileId)
            ?? throw new InvalidOperationException("Không tìm thấy hồ sơ giáo viên.");

        if (teacherProfile.Status != TeacherProfileStatus.Active)
        {
            throw new InvalidOperationException("Hồ sơ giáo viên này chưa hoạt động.");
        }

        if (request.StudentId == teacherProfile.UserId)
        {
            throw new InvalidOperationException("Bạn không thể đặt lịch với chính mình.");
        }

        if (!IsTeachingModeAllowed(teacherProfile.TeachingMode, request.TeachingMode))
        {
            throw new InvalidOperationException("Hình thức học đã chọn không phù hợp với hồ sơ giáo viên.");
        }

        if (request.StartTime <= DateTimeOffset.Now)
        {
            throw new InvalidOperationException("Không thể đặt lịch trong quá khứ.");
        }

        var endTime = request.StartTime.AddMinutes(Math.Clamp(request.DurationMinutes, 30, 240));
        if (!IsWithinAvailability(teacherProfile.UserId, request.StartTime, endTime))
        {
            throw new InvalidOperationException("Thời gian đã chọn nằm ngoài lịch rảnh của giáo viên.");
        }

        var overlaps = db.Bookings.Any(b =>
            b.TeacherId == teacherProfile.UserId &&
            (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Paid || b.Status == BookingStatus.InProgress) &&
            request.StartTime < b.EndTime &&
            endTime > b.StartTime);

        if (overlaps)
        {
            throw new InvalidOperationException("Khung giờ này bị trùng với lịch học khác.");
        }

        var selectedAvailability = AvailabilityForSlot(teacherProfile.UserId, request.StartTime, endTime);
        var bookingMode = selectedAvailability?.TeachingMode ?? request.TeachingMode;
        var bookingLocation = bookingMode == TeachingMode.Online
            ? ""
            : !string.IsNullOrWhiteSpace(selectedAvailability?.OfflineLocation)
                ? selectedAvailability.OfflineLocation
                : teacherProfile.OfflineLocation;

        var booking = new Booking(
            Guid.NewGuid(),
            request.StudentId,
            teacherProfile.UserId,
            request.StartTime,
            endTime,
            BookingStatus.Pending,
            $"room-{Guid.NewGuid():N}",
            bookingMode,
            bookingLocation,
            string.IsNullOrWhiteSpace(selectedAvailability?.PlannedContent)
                ? "Ôn tập mục tiêu và luyện đề"
                : selectedAvailability.PlannedContent.Trim());
        db.Bookings.Add(booking);
        AddNotification(
            teacherProfile.UserId,
            "📚",
            $"{student.Name} vừa đặt lịch học",
            $"{booking.LessonContent} - {booking.StartTime.ToLocalTime():dd/MM HH:mm}");
        db.SaveChanges();
        return booking;
    }

    public IEnumerable<TeacherAvailability> AvailabilitiesForTeacher(Guid teacherId) =>
        db.TeacherAvailabilities
            .AsNoTracking()
            .Where(a => a.TeacherId == teacherId)
            .OrderBy(a => a.DayOfWeek)
            .ThenBy(a => a.StartTime)
            .ToList();

    public TeacherAvailability SaveAvailability(Guid teacherId, DayOfWeek dayOfWeek, TimeSpan startTime, TimeSpan endTime, bool isActive, string plannedContent, TeachingMode teachingMode, string offlineLocation, Guid? id = null)
    {
        if (startTime >= endTime)
        {
            throw new InvalidOperationException("Giờ bắt đầu phải trước giờ kết thúc.");
        }

        var normalizedLocation = teachingMode == TeachingMode.Online ? "" : offlineLocation.Trim();
        var availability = new TeacherAvailability(id ?? Guid.NewGuid(), teacherId, dayOfWeek, startTime, endTime, isActive, plannedContent.Trim(), teachingMode, normalizedLocation);
        if (id is not null && db.TeacherAvailabilities.AsNoTracking().Any(a => a.Id == id.Value && a.TeacherId == teacherId))
        {
            db.TeacherAvailabilities.Update(availability);
        }
        else
        {
            db.TeacherAvailabilities.Add(availability);
        }

        db.SaveChanges();
        return availability;
    }

    public void DeleteAvailability(Guid teacherId, Guid availabilityId)
    {
        var availability = db.TeacherAvailabilities.FirstOrDefault(a => a.Id == availabilityId && a.TeacherId == teacherId)
            ?? throw new InvalidOperationException("Không tìm thấy khung giờ dạy.");
        db.TeacherAvailabilities.Remove(availability);
        db.SaveChanges();
    }

    public void SetDayAvailability(Guid teacherId, DayOfWeek dayOfWeek, bool isActive)
    {
        var slots = db.TeacherAvailabilities.Where(a => a.TeacherId == teacherId && a.DayOfWeek == dayOfWeek).ToList();
        foreach (var slot in slots)
        {
            db.TeacherAvailabilities.Update(slot with { IsActive = isActive });
        }

        db.SaveChanges();
    }

    public IEnumerable<ScheduleChangeRequest> ScheduleChangeRequestsForTeacher(Guid teacherId) =>
        db.ScheduleChangeRequests
            .AsNoTracking()
            .Where(request => request.TeacherId == teacherId)
            .OrderByDescending(request => request.CreatedAt)
            .ToList();

    public IEnumerable<ScheduleChangeRequest> ScheduleChangeRequestsForStudent(Guid studentId) =>
        db.ScheduleChangeRequests
            .AsNoTracking()
            .Where(request => request.StudentId == studentId)
            .OrderByDescending(request => request.CreatedAt)
            .ToList();

    public ScheduleChangeRequest RequestScheduleChange(Guid bookingId, Guid studentId, ScheduleChangeType type, DateTimeOffset? requestedStartTime, string reason)
    {
        var booking = FindBooking(bookingId);
        if (booking.StudentId != studentId)
        {
            throw new InvalidOperationException("Chỉ học viên sở hữu lịch học mới được gửi yêu cầu.");
        }

        if (booking.StartTime - DateTimeOffset.Now < TimeSpan.FromDays(2))
        {
            throw new InvalidOperationException("Chỉ được yêu cầu đổi lịch hoặc hủy buổi khi còn ít nhất 2 ngày trước giờ học.");
        }

        if (booking.Status is BookingStatus.Cancelled or BookingStatus.Rejected or BookingStatus.Refunded or BookingStatus.Completed)
        {
            throw new InvalidOperationException("Lịch học này không còn đủ điều kiện để yêu cầu thay đổi.");
        }

        if (type == ScheduleChangeType.Reschedule)
        {
            if (requestedStartTime is null)
            {
                throw new InvalidOperationException("Vui lòng chọn thời gian học mới.");
            }

            var duration = booking.EndTime - booking.StartTime;
            ValidateBookingTime(booking.TeacherId, requestedStartTime.Value, requestedStartTime.Value.Add(duration), booking.Id);
        }

        var request = new ScheduleChangeRequest(
            Guid.NewGuid(),
            booking.Id,
            booking.StudentId,
            booking.TeacherId,
            booking.StartTime,
            type == ScheduleChangeType.Reschedule ? requestedStartTime : null,
            type,
            string.IsNullOrWhiteSpace(reason) ? "Học viên yêu cầu cập nhật lịch học." : reason.Trim(),
            ScheduleChangeStatus.Pending,
            DateTimeOffset.UtcNow,
            null);

        db.ScheduleChangeRequests.Add(request);
        db.SaveChanges();
        return request;
    }

    public ScheduleChangeRequest AcceptScheduleChange(Guid requestId, Guid teacherId)
    {
        var request = db.ScheduleChangeRequests.AsNoTracking().FirstOrDefault(item => item.Id == requestId)
            ?? throw new InvalidOperationException("Không tìm thấy yêu cầu đổi lịch.");
        if (request.TeacherId != teacherId)
        {
            throw new InvalidOperationException("Chỉ giáo viên của lịch học này mới được xử lý yêu cầu.");
        }

        if (request.Status != ScheduleChangeStatus.Pending)
        {
            throw new InvalidOperationException("Yêu cầu này đã được xử lý.");
        }

        var booking = db.Bookings.AsNoTracking().FirstOrDefault(item => item.Id == request.BookingId)
            ?? throw new InvalidOperationException("Không tìm thấy lịch học.");
        if (booking.StartTime - DateTimeOffset.Now < TimeSpan.FromDays(2))
        {
            throw new InvalidOperationException("Yêu cầu đã quá hạn xử lý vì còn dưới 2 ngày trước giờ học.");
        }

        if (request.Type == ScheduleChangeType.Cancel)
        {
            var shouldRefund = booking.Status is BookingStatus.Paid or BookingStatus.InProgress;
            db.Bookings.Update(booking with { Status = shouldRefund ? BookingStatus.Refunded : BookingStatus.Cancelled });
            if (shouldRefund && !db.Transactions.Any(transaction => transaction.BookingId == booking.Id && transaction.Type == TransactionType.Refund))
            {
                var teacherProfile = db.TeacherProfiles.AsNoTracking().FirstOrDefault(profile => profile.UserId == booking.TeacherId);
                db.Transactions.Add(new Transaction(
                    Guid.NewGuid(),
                    booking.Id,
                    teacherProfile?.PricePerSession ?? 0,
                    TransactionType.Refund,
                    TransactionStatus.Refunded,
                    DateTimeOffset.UtcNow));
                UpdateInvoiceStatus(booking.Id, InvoiceStatus.Refunded);
                UpdateStudentPaymentWalletStatus(booking.Id, "Hoàn tiền");
                AddWalletTransactionIfMissing(booking.StudentId, WalletTransactionType.Refund, "Hệ thống SkillBridge", teacherProfile?.PricePerSession ?? 0, "Hoàn tiền", booking.Id, null, booking.LessonContent);
            }
        }
        else if (request.RequestedStartTime is { } requestedStart)
        {
            var duration = booking.EndTime - booking.StartTime;
            var requestedEnd = requestedStart.Add(duration);
            ValidateBookingTime(booking.TeacherId, requestedStart, requestedEnd, booking.Id);
            db.Bookings.Update(booking with { StartTime = requestedStart, EndTime = requestedEnd });
        }

        var updated = request with { Status = ScheduleChangeStatus.Accepted, ResolvedAt = DateTimeOffset.UtcNow };
        db.ScheduleChangeRequests.Update(updated);
        AddNotification(
            request.StudentId,
            "📅",
            "Giáo viên đã chấp nhận yêu cầu đổi lịch",
            request.Type == ScheduleChangeType.Cancel ? "Buổi học đã được hủy." : "Lịch học đã được cập nhật.");
        db.SaveChanges();
        return updated;
    }

    public ScheduleChangeRequest RejectScheduleChange(Guid requestId, Guid teacherId)
    {
        var request = db.ScheduleChangeRequests.AsNoTracking().FirstOrDefault(item => item.Id == requestId)
            ?? throw new InvalidOperationException("Không tìm thấy yêu cầu đổi lịch.");
        if (request.TeacherId != teacherId)
        {
            throw new InvalidOperationException("Chỉ giáo viên của lịch học này mới được xử lý yêu cầu.");
        }

        if (request.Status != ScheduleChangeStatus.Pending)
        {
            throw new InvalidOperationException("Yêu cầu này đã được xử lý.");
        }

        var updated = request with { Status = ScheduleChangeStatus.Rejected, ResolvedAt = DateTimeOffset.UtcNow };
        db.ScheduleChangeRequests.Update(updated);
        AddNotification(
            request.StudentId,
            "📅",
            "Giáo viên đã từ chối yêu cầu đổi lịch",
            request.Reason);
        db.SaveChanges();
        return updated;
    }

    public IEnumerable<(DateTimeOffset Start, DateTimeOffset End, bool IsBooked, TeachingMode TeachingMode, string OfflineLocation, string LessonContent)> BookingSlotsForTeacher(Guid teacherId, int daysAhead = 14)
    {
        var now = DateTimeOffset.Now;
        var availabilities = db.TeacherAvailabilities
            .AsNoTracking()
            .Where(a => a.TeacherId == teacherId && a.IsActive)
            .ToList();
        var bookings = db.Bookings
            .AsNoTracking()
            .Where(b => b.TeacherId == teacherId &&
                (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Paid || b.Status == BookingStatus.InProgress))
            .ToList();
        var profile = db.TeacherProfiles.AsNoTracking().FirstOrDefault(t => t.UserId == teacherId);
        var slots = new List<(DateTimeOffset Start, DateTimeOffset End, bool IsBooked, TeachingMode TeachingMode, string OfflineLocation, string LessonContent)>();

        for (var dayOffset = 0; dayOffset <= daysAhead; dayOffset++)
        {
            var date = now.Date.AddDays(dayOffset);
            foreach (var availability in availabilities.Where(a => a.DayOfWeek == date.DayOfWeek))
            {
                var cursor = new DateTimeOffset(date.Add(availability.StartTime), now.Offset);
                var rangeEnd = new DateTimeOffset(date.Add(availability.EndTime), now.Offset);
                while (cursor.AddHours(1) <= rangeEnd)
                {
                    var slotEnd = cursor.AddHours(1);
                    if (cursor > now)
                    {
                        var isBooked = bookings.Any(b => cursor < b.EndTime && slotEnd > b.StartTime);
                        var location = availability.TeachingMode == TeachingMode.Online
                            ? ""
                            : !string.IsNullOrWhiteSpace(availability.OfflineLocation)
                                ? availability.OfflineLocation
                                : profile?.OfflineLocation ?? "";
                        slots.Add((
                            cursor,
                            slotEnd,
                            isBooked,
                            availability.TeachingMode,
                            location,
                            string.IsNullOrWhiteSpace(availability.PlannedContent) ? "Ôn tập mục tiêu và luyện đề" : availability.PlannedContent));
                    }

                    cursor = slotEnd;
                }
            }
        }

        return slots.OrderBy(s => s.Start).ToList();
    }

    public Booking ConfirmBooking(Guid bookingId, BookingDecisionRequest request)
    {
        var booking = FindBooking(bookingId);
        if (booking.TeacherId != request.TeacherId)
        {
            throw new InvalidOperationException("Chỉ giáo viên của lịch học này mới được xác nhận.");
        }

        if (booking.Status != BookingStatus.Pending)
        {
            throw new InvalidOperationException("Chỉ lịch đang chờ mới có thể xác nhận.");
        }

        return UpdateBooking(booking with { Status = BookingStatus.Confirmed });
    }

    public Booking RejectBooking(Guid bookingId, BookingDecisionRequest request)
    {
        var booking = FindBooking(bookingId);
        if (booking.TeacherId != request.TeacherId)
        {
            throw new InvalidOperationException("Chỉ giáo viên của lịch học này mới được từ chối.");
        }

        if (booking.Status != BookingStatus.Pending)
        {
            throw new InvalidOperationException("Chỉ lịch đang chờ mới có thể từ chối.");
        }

        return UpdateBooking(booking with { Status = BookingStatus.Rejected });
    }

    public Booking Pay(Guid bookingId, PaymentRequest request)
    {
        var booking = FindBooking(bookingId);
        if (booking.StudentId != request.StudentId)
        {
            throw new InvalidOperationException("Chỉ học viên sở hữu lịch học mới được thanh toán.");
        }

        if (booking.Status != BookingStatus.Confirmed)
        {
            throw new InvalidOperationException("Chỉ lịch đã xác nhận mới được thanh toán.");
        }

        var teacherProfile = db.TeacherProfiles.First(t => t.UserId == booking.TeacherId);
        if (StudentWalletBalance(booking.StudentId) < teacherProfile.PricePerSession)
        {
            throw new InvalidOperationException("Số dư tài khoản không đủ. Vui lòng nạp tiền trước khi thanh toán.");
        }

        if (booking.StudentCompleted)
        {
            throw new InvalidOperationException("Hoc vien da xac nhan buoi hoc nay.");
        }

        var student = db.Users.AsNoTracking().FirstOrDefault(user => user.Id == booking.StudentId);
        db.Transactions.Add(new Transaction(Guid.NewGuid(), booking.Id, teacherProfile.PricePerSession, TransactionType.Hold, TransactionStatus.Held, DateTimeOffset.UtcNow));
        db.WalletTransactions.Add(new WalletTransaction(
            Guid.NewGuid(),
            booking.StudentId,
            WalletTransactionType.Payment,
            string.IsNullOrWhiteSpace(request.Method) ? "Ví SkillBridge" : request.Method.Trim(),
            teacherProfile.PricePerSession,
            "Đang giữ tiền",
            DateTimeOffset.UtcNow,
            booking.Id,
            null,
            booking.LessonContent,
            NextWalletTransactionCode("PAY")));
        db.PaymentInvoices.Add(new PaymentInvoice(
            Guid.NewGuid(),
            NextInvoiceCode(),
            booking.Id,
            booking.StudentId,
            booking.TeacherId,
            booking.LessonContent,
            booking.TeachingMode,
            teacherProfile.PricePerSession,
            string.IsNullOrWhiteSpace(request.Method) ? "Ví SkillBridge" : request.Method.Trim(),
            InvoiceStatus.Held,
            DateTimeOffset.UtcNow));
        AddNotification(
            booking.TeacherId,
            "💰",
            $"{student?.Name ?? "Học viên"} đã thanh toán {teacherProfile.PricePerSession:N0}đ",
            booking.LessonContent);
        return UpdateBooking(booking with { Status = BookingStatus.Paid });
    }

    public Booking Complete(Guid bookingId, CompleteBookingRequest request)
    {
        var booking = FindBooking(bookingId);
        if (booking.StudentId != request.StudentId || booking.Status is not (BookingStatus.Paid or BookingStatus.InProgress))
        {
            throw new InvalidOperationException("Chỉ học viên sở hữu lịch đã thanh toán mới được xác nhận đã học.");
        }

        var student = db.Users.AsNoTracking().FirstOrDefault(user => user.Id == booking.StudentId);
        AddNotification(
            booking.TeacherId,
            "✅",
            $"{student?.Name ?? "Học viên"} đã xác nhận hoàn thành buổi học",
            booking.LessonContent);
        return ConfirmCompletion(booking with { StudentCompleted = true });
    }

    public Booking TeacherComplete(Guid bookingId, TeacherCompleteBookingRequest request)
    {
        var booking = FindBooking(bookingId);
        if (booking.TeacherId != request.TeacherId || booking.Status is not (BookingStatus.Paid or BookingStatus.InProgress))
        {
            throw new InvalidOperationException("Chỉ giáo viên của lịch đã thanh toán mới được xác nhận đã dạy.");
        }

        if (booking.TeacherCompleted)
        {
            throw new InvalidOperationException("Giao vien da xac nhan buoi hoc nay.");
        }

        var teacher = db.Users.AsNoTracking().FirstOrDefault(user => user.Id == booking.TeacherId);
        AddNotification(
            booking.StudentId,
            "✅",
            $"{teacher?.Name ?? "Giáo viên"} đã xác nhận đã dạy",
            booking.LessonContent);
        return ConfirmCompletion(booking with { TeacherCompleted = true });
    }

    private Booking ConfirmCompletion(Booking booking)
    {
        var latestTransaction = db.Transactions
            .AsNoTracking()
            .Where(t => t.BookingId == booking.Id)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefault();
        if (latestTransaction?.Status == TransactionStatus.UnderReview)
        {
            throw new InvalidOperationException("Lịch học đang được xem xét khiếu nại nên chưa thể trả tiền cho giáo viên.");
        }

        if (booking.StudentCompleted && booking.TeacherCompleted)
        {
            var hasRelease = db.Transactions.AsNoTracking().Any(t =>
                t.BookingId == booking.Id &&
                t.Type == TransactionType.Release &&
                t.Status == TransactionStatus.Released);
            if (!hasRelease)
            {
                var teacherProfile = db.TeacherProfiles.First(t => t.UserId == booking.TeacherId);
                db.Transactions.Add(new Transaction(Guid.NewGuid(), booking.Id, teacherProfile.PricePerSession, TransactionType.Release, TransactionStatus.Released, DateTimeOffset.UtcNow));
                AddWalletTransactionIfMissing(booking.TeacherId, WalletTransactionType.Payment, "Hệ thống SkillBridge", teacherProfile.PricePerSession * 0.90m, "Hoàn thành", booking.Id, null, booking.LessonContent);
            }

            UpdateInvoiceStatus(booking.Id, InvoiceStatus.Completed);
            UpdateStudentPaymentWalletStatus(booking.Id, "Hoàn thành");

            return UpdateBooking(booking with { Status = BookingStatus.Completed });
        }

        return UpdateBooking(booking with { Status = BookingStatus.InProgress });
    }

    public void DisputePayment(Guid bookingId, Guid studentId, string reason) =>
        DisputePayment(bookingId, new ComplaintRequest(studentId, reason, ""));

    public void DisputePayment(Guid bookingId, ComplaintRequest request)
    {
        var booking = FindBooking(bookingId);
        if (booking.StudentId != request.StudentId || booking.Status is not (BookingStatus.Paid or BookingStatus.InProgress))
        {
            throw new InvalidOperationException("Chỉ học viên sở hữu lịch đã thanh toán mới được gửi khiếu nại.");
        }

        var existingComplaint = db.Complaints.AsNoTracking().FirstOrDefault(complaint => complaint.BookingId == booking.Id);
        if (existingComplaint is null)
        {
            db.Complaints.Add(new Complaint(
                Guid.NewGuid(),
                booking.Id,
                booking.StudentId,
                booking.TeacherId,
                string.IsNullOrWhiteSpace(request.Reason) ? "Khiếu nại buổi học" : request.Reason.Trim(),
                string.IsNullOrWhiteSpace(request.EvidenceUrl) ? "" : request.EvidenceUrl.Trim(),
                "",
                "",
                ComplaintStatus.WaitingTeacherResponse,
                DateTimeOffset.UtcNow));
        }

        if (!db.Transactions.Any(transaction => transaction.BookingId == booking.Id && transaction.Status == TransactionStatus.UnderReview))
        {
            var teacherProfile = db.TeacherProfiles.First(t => t.UserId == booking.TeacherId);
            db.Transactions.Add(new Transaction(Guid.NewGuid(), booking.Id, teacherProfile.PricePerSession, TransactionType.Hold, TransactionStatus.UnderReview, DateTimeOffset.UtcNow));
        }

        var student = db.Users.AsNoTracking().FirstOrDefault(user => user.Id == booking.StudentId);
        AddNotification(
            booking.TeacherId,
            "!",
            $"{student?.Name ?? "Hoc vien"} da gui khieu nai",
            string.IsNullOrWhiteSpace(request.Reason) ? booking.LessonContent : request.Reason.Trim());
        db.SaveChanges();
    }

    public Complaint RespondToComplaint(Guid complaintId, TeacherComplaintResponseRequest request)
    {
        var complaint = db.Complaints.AsNoTracking().FirstOrDefault(item => item.Id == complaintId)
            ?? throw new InvalidOperationException("Không tìm thấy khiếu nại.");
        if (complaint.TeacherId != request.TeacherId)
        {
            throw new InvalidOperationException("Chỉ giáo viên liên quan mới được phản hồi khiếu nại.");
        }

        var updated = complaint with
        {
            TeacherResponse = string.IsNullOrWhiteSpace(request.Response) ? "Giáo viên đã gửi phản hồi." : request.Response.Trim(),
            TeacherEvidenceUrl = string.IsNullOrWhiteSpace(request.EvidenceUrl) ? "" : request.EvidenceUrl.Trim(),
            Status = ComplaintStatus.InReview,
            TeacherRespondedAt = DateTimeOffset.UtcNow
        };
        db.Complaints.Update(updated);
        AddNotification(complaint.StudentId, "!", "Giáo viên đã phản hồi khiếu nại", updated.TeacherResponse);
        db.SaveChanges();
        return updated;
    }

    public IEnumerable<Complaint> ComplaintsForTeacher(Guid teacherId) =>
        db.Complaints
            .AsNoTracking()
            .Where(complaint => complaint.TeacherId == teacherId)
            .OrderByDescending(complaint => complaint.CreatedAt)
            .ToList();

    public TeacherReview ReviewTeacher(Guid bookingId, ReviewTeacherRequest request)
    {
        var booking = FindBooking(bookingId);
        if (booking.StudentId != request.StudentId)
        {
            throw new InvalidOperationException("Chỉ học viên sở hữu buổi học mới được đánh giá.");
        }

        if (booking.Status != BookingStatus.Completed && !(booking.StudentCompleted && booking.TeacherCompleted))
        {
            throw new InvalidOperationException("Chỉ được đánh giá sau khi buổi học hoàn thành.");
        }

        if (db.TeacherReviews.Any(review => review.BookingId == booking.Id))
        {
            throw new InvalidOperationException("Buổi học này đã được đánh giá.");
        }

        var review = new TeacherReview(
            Guid.NewGuid(),
            booking.Id,
            booking.TeacherId,
            booking.StudentId,
            Math.Clamp(request.Stars, 1, 5),
            string.IsNullOrWhiteSpace(request.Comment) ? "" : request.Comment.Trim(),
            DateTimeOffset.UtcNow);

        db.TeacherReviews.Add(review);
        UpdateTeacherRating(booking.TeacherId);
        db.SaveChanges();
        return review;
    }

    public IEnumerable<TeacherReview> ReviewsForTeacher(Guid teacherId) =>
        db.TeacherReviews
            .AsNoTracking()
            .Where(review => review.TeacherId == teacherId)
            .OrderByDescending(review => review.CreatedAt)
            .ToList();

    public IEnumerable<Guid> ReviewedBookingIdsForStudent(Guid studentId) =>
        db.TeacherReviews
            .AsNoTracking()
            .Where(review => review.StudentId == studentId)
            .Select(review => review.BookingId)
            .ToList();

    public IEnumerable<Booking> BookingsForUser(Guid userId) =>
        db.Bookings
            .AsNoTracking()
            .Where(b => b.StudentId == userId || b.TeacherId == userId)
            .OrderByDescending(b => b.StartTime)
            .ThenBy(b => b.Id)
            .AsEnumerable()
            .GroupBy(b => b.Id)
            .Select(group => group.First())
            .ToList();

    public Booking BookingForParticipant(Guid bookingId, Guid userId) =>
        db.Bookings
            .AsNoTracking()
            .FirstOrDefault(b => b.Id == bookingId && (b.StudentId == userId || b.TeacherId == userId))
        ?? throw new InvalidOperationException("Không tìm thấy lịch học.");

    public User? UserById(Guid userId) =>
        db.Users.AsNoTracking().FirstOrDefault(u => u.Id == userId);

    public StudentProfile? StudentProfileForUser(Guid userId) =>
        db.StudentProfiles.AsNoTracking().FirstOrDefault(profile => profile.UserId == userId);

    public User SaveStudentProfile(
        Guid userId,
        string? displayName,
        string? email,
        string? avatarUrl,
        string? bio,
        string? gender = null,
        string? dateOfBirth = null,
        string? phone = null,
        string? learningGoal = null)
    {
        var user = db.Users.AsNoTracking().FirstOrDefault(u => u.Id == userId)
            ?? throw new InvalidOperationException("ERR_USER_NOT_FOUND");
        if (user.Role != UserRole.Student)
        {
            throw new InvalidOperationException("Chỉ học viên mới được cập nhật hồ sơ học viên.");
        }

        var normalizedEmail = string.IsNullOrWhiteSpace(email) ? user.Email : email.Trim().ToLowerInvariant();
        if (db.Users.AsNoTracking().Any(item => item.Id != userId && item.Email == normalizedEmail))
        {
            throw new InvalidOperationException("ERR_EMAIL_EXISTS");
        }

        var existingProfile = db.StudentProfiles.AsNoTracking().FirstOrDefault(profile => profile.UserId == userId);
        var normalizedName = string.IsNullOrWhiteSpace(displayName)
            ? existingProfile?.DisplayName ?? user.Name
            : displayName.Trim();
        var normalizedAvatar = string.IsNullOrWhiteSpace(avatarUrl)
            ? existingProfile?.AvatarUrl ?? user.AvatarUrl
            : avatarUrl.Trim();
        var normalizedBio = bio is null ? existingProfile?.Bio ?? user.Bio : bio.Trim();
        var normalizedGender = gender is null ? existingProfile?.Gender ?? "" : gender.Trim();
        var normalizedPhone = phone is null ? existingProfile?.Phone ?? "" : phone.Trim();
        var normalizedLearningGoal = learningGoal is null ? existingProfile?.LearningGoal ?? "" : learningGoal.Trim();
        var normalizedBirthDate = ParseDateOfBirth(dateOfBirth, existingProfile?.DateOfBirth);
        var updated = user with
        {
            Name = normalizedName,
            Email = normalizedEmail,
            AvatarUrl = normalizedAvatar,
            Bio = normalizedBio
        };

        db.Users.Update(updated);
        var profile = new StudentProfile(
            existingProfile?.Id ?? Guid.NewGuid(),
            userId,
            normalizedName,
            normalizedEmail,
            normalizedGender,
            normalizedBirthDate,
            normalizedPhone,
            normalizedLearningGoal,
            normalizedBio,
            normalizedAvatar,
            DateTimeOffset.UtcNow);

        if (existingProfile is null)
        {
            db.StudentProfiles.Add(profile);
        }
        else
        {
            db.StudentProfiles.Update(profile);
        }

        db.SaveChanges();
        return updated;
    }

    public TeacherProfile? TeacherProfileForUser(Guid userId) =>
        db.TeacherProfiles.AsNoTracking().FirstOrDefault(t => t.UserId == userId);

    public TeacherProfile SaveTeacherProfile(
        Guid userId,
        string displayName,
        string avatarUrl,
        string skill,
        string description,
        string experience,
        decimal pricePerSession,
        TeachingMode teachingMode,
        string offlineLocation,
        string portfolioImageUrl,
        TeacherProfileStatus status,
        string defaultPayoutMethod = "Tài khoản ngân hàng",
        string defaultPayoutBank = "Vietcombank")
    {
        var user = db.Users.AsNoTracking().FirstOrDefault(u => u.Id == userId)
            ?? throw new InvalidOperationException("ERR_USER_NOT_FOUND");

        if (user.Role != UserRole.Teacher)
        {
            throw new InvalidOperationException("Chỉ giáo viên mới được cập nhật hồ sơ giáo viên.");
        }

        if (string.IsNullOrWhiteSpace(displayName) || string.IsNullOrWhiteSpace(skill) || string.IsNullOrWhiteSpace(description))
        {
            throw new InvalidOperationException("Vui lòng nhập tên hiển thị, kỹ năng giảng dạy và phần giới thiệu.");
        }

        var normalizedPrice = Math.Max(0, pricePerSession);
        db.Users.Update(user with
        {
            Name = displayName.Trim(),
            AvatarUrl = string.IsNullOrWhiteSpace(avatarUrl)
                ? $"https://api.dicebear.com/8.x/initials/svg?seed={Uri.EscapeDataString(displayName.Trim())}"
                : avatarUrl.Trim(),
            Bio = description.Trim()
        });

        var existing = db.TeacherProfiles.AsNoTracking().FirstOrDefault(t => t.UserId == userId);
        var profile = new TeacherProfile(
            existing?.Id ?? Guid.NewGuid(),
            userId,
            skill.Trim(),
            description.Trim(),
            experience.Trim(),
            normalizedPrice,
            teachingMode,
            offlineLocation.Trim(),
            portfolioImageUrl.Trim(),
            status,
            existing?.Rating ?? 4.8m,
            true,
            string.IsNullOrWhiteSpace(defaultPayoutMethod) ? existing?.DefaultPayoutMethod ?? "Tài khoản ngân hàng" : defaultPayoutMethod.Trim(),
            string.IsNullOrWhiteSpace(defaultPayoutBank) ? existing?.DefaultPayoutBank ?? "Vietcombank" : defaultPayoutBank.Trim());

        if (existing is null)
        {
            db.TeacherProfiles.Add(profile);
        }
        else
        {
            db.TeacherProfiles.Update(profile);
        }

        db.SaveChanges();
        return profile;
    }

    public IEnumerable<(Booking Booking, User Student, TeacherProfile? Profile)> TeacherBookingRows(Guid teacherId)
    {
        var bookings = db.Bookings
            .AsNoTracking()
            .Where(b => b.TeacherId == teacherId)
            .OrderByDescending(b => b.StartTime)
            .ToList();
        var studentIds = bookings.Select(b => b.StudentId).Distinct().ToList();
        var students = db.Users.AsNoTracking().Where(u => studentIds.Contains(u.Id)).ToDictionary(u => u.Id);
        var profile = db.TeacherProfiles.AsNoTracking().FirstOrDefault(t => t.UserId == teacherId);

        return bookings
            .Where(b => students.ContainsKey(b.StudentId))
            .Select(b => (b, students[b.StudentId], profile))
            .GroupBy(row => row.b.Id)
            .Select(group => group.First())
            .ToList();
    }

    private bool IsWithinAvailability(Guid teacherId, DateTimeOffset startTime, DateTimeOffset endTime)
    {
        var localStart = startTime.ToLocalTime();
        var localEnd = endTime.ToLocalTime();
        if (localStart.Date != localEnd.Date)
        {
            return false;
        }

        return db.TeacherAvailabilities.Any(a =>
            a.TeacherId == teacherId &&
            a.IsActive &&
            a.DayOfWeek == localStart.DayOfWeek &&
            a.StartTime <= localStart.TimeOfDay &&
            a.EndTime >= localEnd.TimeOfDay);
    }

    private string LessonContentForSlot(Guid teacherId, DateTimeOffset startTime, DateTimeOffset endTime)
    {
        var content = AvailabilityForSlot(teacherId, startTime, endTime)?.PlannedContent;

        return string.IsNullOrWhiteSpace(content)
            ? "Ôn tập mục tiêu và luyện đề"
            : content.Trim();
    }

    private TeacherAvailability? AvailabilityForSlot(Guid teacherId, DateTimeOffset startTime, DateTimeOffset endTime)
    {
        var localStart = startTime.ToLocalTime();
        var localEnd = endTime.ToLocalTime();
        return db.TeacherAvailabilities
            .AsNoTracking()
            .Where(a =>
                a.TeacherId == teacherId &&
                a.IsActive &&
                a.DayOfWeek == localStart.DayOfWeek &&
                a.StartTime <= localStart.TimeOfDay &&
                a.EndTime >= localEnd.TimeOfDay)
            .OrderBy(a => a.StartTime)
            .FirstOrDefault();
    }

    private void ValidateBookingTime(Guid teacherId, DateTimeOffset startTime, DateTimeOffset endTime, Guid? ignoredBookingId = null)
    {
        if (startTime <= DateTimeOffset.Now)
        {
            throw new InvalidOperationException("Không thể đặt lịch trong quá khứ.");
        }

        if (!IsWithinAvailability(teacherId, startTime, endTime))
        {
            throw new InvalidOperationException("Thời gian đã chọn nằm ngoài lịch dạy đang bật của giáo viên.");
        }

        var overlaps = db.Bookings.Any(b =>
            b.TeacherId == teacherId &&
            b.Id != ignoredBookingId &&
            (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Paid || b.Status == BookingStatus.InProgress) &&
            startTime < b.EndTime &&
            endTime > b.StartTime);

        if (overlaps)
        {
            throw new InvalidOperationException("Khung giờ này bị trùng với lịch học khác.");
        }
    }

    public IEnumerable<Transaction> TransactionsForBooking(Guid bookingId) =>
        db.Transactions
            .AsNoTracking()
            .Where(t => t.BookingId == bookingId)
            .OrderByDescending(t => t.CreatedAt)
            .ToList();

    public IEnumerable<Withdrawal> WithdrawalsForTeacher(Guid teacherId) =>
        db.Withdrawals
            .AsNoTracking()
            .Where(withdrawal => withdrawal.TeacherId == teacherId)
            .OrderByDescending(withdrawal => withdrawal.CreatedAt)
            .ToList();

    public Withdrawal RequestWithdrawal(Guid teacherId, decimal amount, string method = "Tài khoản ngân hàng", string accountName = "", string accountNumber = "", string bankName = "")
    {
        if (amount <= 0)
        {
            throw new InvalidOperationException("Số tiền rút phải lớn hơn 0.");
        }

        var balance = WithdrawableBalance(teacherId);
        if (amount > balance)
        {
            throw new InvalidOperationException("Số dư có thể rút không đủ.");
        }

        var withdrawal = new Withdrawal(
            Guid.NewGuid(),
            teacherId,
            amount,
            WithdrawalStatus.Pending,
            DateTimeOffset.UtcNow,
            null,
            string.IsNullOrWhiteSpace(method) ? "Tài khoản ngân hàng" : method.Trim(),
            accountName.Trim(),
            accountNumber.Trim(),
            bankName.Trim());
        db.Withdrawals.Add(withdrawal);
        db.WalletTransactions.Add(new WalletTransaction(
            Guid.NewGuid(),
            teacherId,
            WalletTransactionType.Withdrawal,
            withdrawal.Method,
            amount,
            "Đang xử lý",
            DateTimeOffset.UtcNow,
            null,
            withdrawal.Id,
            string.IsNullOrWhiteSpace(withdrawal.BankName) ? withdrawal.AccountNumber : withdrawal.BankName,
            NextWalletTransactionCode("WDR")));
        db.SaveChanges();
        return withdrawal;
    }

    public Withdrawal MarkWithdrawalPaid(Guid teacherId, Guid withdrawalId)
    {
        var withdrawal = db.Withdrawals.AsNoTracking().FirstOrDefault(item => item.Id == withdrawalId && item.TeacherId == teacherId)
            ?? throw new InvalidOperationException("Không tìm thấy yêu cầu rút tiền.");
        if (withdrawal.Status == WithdrawalStatus.Paid)
        {
            return withdrawal;
        }

        var updated = withdrawal with { Status = WithdrawalStatus.Paid, ProcessedAt = DateTimeOffset.UtcNow };
        db.Withdrawals.Update(updated);
        var walletRows = db.WalletTransactions
            .Where(transaction => transaction.WithdrawalId == withdrawal.Id && transaction.Type == WalletTransactionType.Withdrawal)
            .ToList();
        foreach (var row in walletRows)
        {
            db.WalletTransactions.Update(row with { Status = "Đã rút" });
        }

        db.SaveChanges();
        return updated;
    }

    public decimal WithdrawableBalance(Guid teacherId)
    {
        var releasedNet = ReleasedTeacherNet(teacherId);
        var pending = db.Withdrawals.AsNoTracking()
            .Where(withdrawal => withdrawal.TeacherId == teacherId && withdrawal.Status == WithdrawalStatus.Pending)
            .Sum(withdrawal => withdrawal.Amount);
        var withdrawn = db.Withdrawals.AsNoTracking()
            .Where(withdrawal => withdrawal.TeacherId == teacherId && withdrawal.Status == WithdrawalStatus.Paid)
            .Sum(withdrawal => withdrawal.Amount);
        return Math.Max(0, releasedNet - pending - withdrawn);
    }

    public WalletTransaction TopUpWallet(Guid studentId, decimal amount, string method)
    {
        var user = db.Users.AsNoTracking().FirstOrDefault(item => item.Id == studentId)
            ?? throw new InvalidOperationException("Không tìm thấy học viên.");
        if (user.Role != UserRole.Student)
        {
            throw new InvalidOperationException("Chỉ học viên mới được nạp tiền vào ví học tập.");
        }

        if (amount <= 0)
        {
            throw new InvalidOperationException("Số tiền nạp phải lớn hơn 0.");
        }

        var transaction = new WalletTransaction(
            Guid.NewGuid(),
            studentId,
            WalletTransactionType.TopUp,
            string.IsNullOrWhiteSpace(method) ? "Momo" : method.Trim(),
            amount,
            "Hoàn thành",
            DateTimeOffset.UtcNow,
            null,
            null,
            "Nạp tiền",
            NextWalletTransactionCode("DEP"));
        db.WalletTransactions.Add(transaction);
        db.SaveChanges();
        return transaction;
    }

    public decimal StudentWalletBalance(Guid studentId)
    {
        var topUps = db.WalletTransactions.AsNoTracking()
            .Where(transaction => transaction.UserId == studentId && transaction.Type == WalletTransactionType.TopUp)
            .Sum(transaction => transaction.Amount);
        var payments = db.WalletTransactions.AsNoTracking()
            .Where(transaction => transaction.UserId == studentId && transaction.Type == WalletTransactionType.Payment)
            .Sum(transaction => transaction.Amount);
        var refunds = db.WalletTransactions.AsNoTracking()
            .Where(transaction => transaction.UserId == studentId && transaction.Type == WalletTransactionType.Refund)
            .Sum(transaction => transaction.Amount);
        return Math.Max(0, topUps + refunds - payments);
    }

    public IEnumerable<WalletTransaction> WalletTransactionsForUser(Guid userId) =>
        db.WalletTransactions
            .AsNoTracking()
            .Where(transaction => transaction.UserId == userId)
            .OrderByDescending(transaction => transaction.CreatedAt)
            .ToList();

    public IEnumerable<PaymentInvoice> InvoicesForUser(Guid userId) =>
        db.PaymentInvoices
            .AsNoTracking()
            .Where(invoice => invoice.StudentId == userId || invoice.TeacherId == userId)
            .OrderByDescending(invoice => invoice.PaidAt)
            .ToList();

    public PaymentInvoice? InvoiceForBooking(Guid bookingId) =>
        db.PaymentInvoices.AsNoTracking().FirstOrDefault(invoice => invoice.BookingId == bookingId);

    public IEnumerable<LayoutNotificationViewModel> NotificationsForUser(Guid userId)
    {
        var stored = db.Notifications
            .AsNoTracking()
            .Where(notification => notification.UserId == userId)
            .OrderByDescending(notification => notification.CreatedAt)
            .Take(12)
            .Select(notification => new LayoutNotificationViewModel(
                notification.Id,
                notification.Icon,
                notification.Title,
                notification.Body,
                notification.CreatedAt,
                notification.IsRead))
            .ToList();

        var upcomingLessons = db.Bookings
            .AsNoTracking()
            .Where(booking =>
                (booking.StudentId == userId || booking.TeacherId == userId) &&
                booking.StartTime >= DateTimeOffset.Now &&
                booking.StartTime <= DateTimeOffset.Now.AddDays(1) &&
                booking.Status != BookingStatus.Cancelled &&
                booking.Status != BookingStatus.Rejected &&
                booking.Status != BookingStatus.Refunded)
            .OrderBy(booking => booking.StartTime)
            .Take(2)
            .Select(booking => new LayoutNotificationViewModel(
                booking.Id,
                "📚",
                booking.StartTime <= DateTimeOffset.Now.AddMinutes(30)
                    ? "30 phút nữa sẽ bắt đầu buổi học"
                    : "Hôm nay có buổi học",
                $"{booking.LessonContent} - {booking.StartTime.ToLocalTime():HH:mm} - {booking.EndTime.ToLocalTime():HH:mm}",
                booking.StartTime,
                false,
                true))
            .ToList();

        return stored
            .Concat(upcomingLessons)
            .OrderByDescending(notification => notification.CreatedAt)
            .Take(14)
            .ToList();
    }

    public int UnreadNotificationCount(Guid userId) =>
        db.Notifications.AsNoTracking().Count(notification => notification.UserId == userId && !notification.IsRead);

    public int UnreadChatCount(Guid userId) =>
        db.Messages.AsNoTracking().Count(message => message.ReceiverId == userId && !message.IsRead && !message.IsDeleted);

    public IEnumerable<Message> Conversation(Guid userA, Guid userB) =>
        db.Messages
            .AsNoTracking()
            .Where(m => (m.SenderId == userA && m.ReceiverId == userB) || (m.SenderId == userB && m.ReceiverId == userA))
            .OrderBy(m => m.CreatedAt)
            .ToList();

    public IEnumerable<Message> ConversationForViewer(Guid viewerId, Guid contactUserId)
    {
        var hiddenAt = ConversationHiddenAt(viewerId, contactUserId);
        return db.Messages
            .AsNoTracking()
            .Where(m =>
                ((m.SenderId == viewerId && m.ReceiverId == contactUserId) || (m.SenderId == contactUserId && m.ReceiverId == viewerId)) &&
                (hiddenAt == null || m.CreatedAt > hiddenAt.Value))
            .OrderBy(m => m.CreatedAt)
            .ToList();
    }

    public int UnreadMessagesFromContact(Guid userId, Guid contactUserId) =>
        db.Messages
            .AsNoTracking()
            .Count(message =>
                message.SenderId == contactUserId &&
                message.ReceiverId == userId &&
                !message.IsRead &&
                !message.IsDeleted);

    public void MarkConversationRead(Guid userId, Guid contactUserId)
    {
        db.Messages
            .Where(message =>
                message.SenderId == contactUserId &&
                message.ReceiverId == userId &&
                !message.IsRead &&
                !message.IsDeleted)
            .ExecuteUpdate(setters => setters.SetProperty(message => message.IsRead, true));
    }

    public IEnumerable<User> ChatContactsForUser(Guid userId, UserRole role)
    {
        if (role == UserRole.Admin)
        {
            var supportContactIds = db.Messages
                .AsNoTracking()
                .Where(message => message.SenderId == userId || message.ReceiverId == userId)
                .Select(message => message.SenderId == userId ? message.ReceiverId : message.SenderId)
                .Distinct()
                .ToList();

            return db.Users
                .AsNoTracking()
                .Where(user => user.Id != userId && (supportContactIds.Contains(user.Id) || user.Role != UserRole.Admin))
                .OrderBy(user => user.Name)
                .ToList();
        }

        var bookingContactIds = role == UserRole.Teacher
            ? db.Bookings.AsNoTracking().Where(b => b.TeacherId == userId).Select(b => b.StudentId)
            : db.Bookings.AsNoTracking().Where(b => b.StudentId == userId).Select(b => b.TeacherId);

        var messageContactIds = db.Messages
            .AsNoTracking()
            .Where(message => message.SenderId == userId || message.ReceiverId == userId)
            .Select(message => message.SenderId == userId ? message.ReceiverId : message.SenderId);

        var contactIds = bookingContactIds
            .Concat(messageContactIds)
            .Distinct()
            .ToList();
        var supportAdminIds = db.Users
            .AsNoTracking()
            .Where(user => user.Role == UserRole.Admin)
            .Select(user => user.Id)
            .ToList();

        return db.Users
            .AsNoTracking()
            .Where(user =>
                contactIds.Contains(user.Id) &&
                (user.Role == (role == UserRole.Teacher ? UserRole.Student : UserRole.Teacher) ||
                 supportAdminIds.Contains(user.Id)))
            .OrderBy(user => user.Name)
            .ToList();
    }

    public User? DefaultAdminUser() =>
        db.Users.AsNoTracking().OrderBy(user => user.CreatedAt).FirstOrDefault(user => user.Role == UserRole.Admin);

    public DateTimeOffset? ConversationHiddenAt(Guid userId, Guid contactUserId) =>
        db.ConversationHides
            .AsNoTracking()
            .Where(hide => hide.UserId == userId && hide.ContactUserId == contactUserId)
            .Select(hide => (DateTimeOffset?)hide.HiddenAt)
            .FirstOrDefault();

    public ConversationHide HideConversation(Guid userId, Guid contactUserId)
    {
        if (userId == contactUserId)
        {
            throw new InvalidOperationException("Không thể xóa cuộc trò chuyện với chính mình.");
        }

        if (!db.Users.Any(user => user.Id == userId) || !db.Users.Any(user => user.Id == contactUserId))
        {
            throw new InvalidOperationException("Không tìm thấy cuộc trò chuyện.");
        }

        var existing = db.ConversationHides.AsNoTracking().FirstOrDefault(hide =>
            hide.UserId == userId &&
            hide.ContactUserId == contactUserId);
        var hidden = new ConversationHide(existing?.Id ?? Guid.NewGuid(), userId, contactUserId, DateTimeOffset.UtcNow);
        if (existing is null)
        {
            db.ConversationHides.Add(hidden);
        }
        else
        {
            db.ConversationHides.Update(hidden);
        }

        db.SaveChanges();
        return hidden;
    }

    public Message SaveMessage(SendMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            throw new ArgumentException("Vui lòng nhập nội dung tin nhắn.");
        }

        if (!db.Users.Any(user => user.Id == request.SenderId) || !db.Users.Any(user => user.Id == request.ReceiverId))
        {
            throw new InvalidOperationException("Không tìm thấy người gửi hoặc người nhận.");
        }

        if (request.SenderId == request.ReceiverId)
        {
            throw new InvalidOperationException("Không thể gửi tin nhắn cho chính mình.");
        }

        var message = new Message(Guid.NewGuid(), request.SenderId, request.ReceiverId, request.Content.Trim(), DateTimeOffset.UtcNow, false, false);
        db.Messages.Add(message);
        db.SaveChanges();
        return message;
    }

    public Message DeleteMessage(Guid messageId, Guid userId)
    {
        var message = db.Messages.AsNoTracking().FirstOrDefault(m => m.Id == messageId)
            ?? throw new InvalidOperationException("Không tìm thấy tin nhắn.");
        if (message.SenderId != userId)
        {
            throw new InvalidOperationException("Bạn chỉ được xóa tin nhắn do chính mình gửi.");
        }

        var updated = message with { Content = "Tin nhắn đã được xóa", IsDeleted = true };
        db.Messages.Update(updated);
        db.SaveChanges();
        return updated;
    }

    public bool CanJoinCall(Guid bookingId, Guid userId)
    {
        var booking = db.Bookings.AsNoTracking().FirstOrDefault(b => b.Id == bookingId);
        if (booking is null ||
            booking.TeachingMode is not (TeachingMode.Online or TeachingMode.Hybrid) ||
            booking.Status is not (BookingStatus.Paid or BookingStatus.InProgress) ||
            (booking.StudentId != userId && booking.TeacherId != userId))
        {
            return false;
        }

        if (booking.TeacherId == userId && booking.TeacherCompleted)
        {
            return false;
        }

        if (booking.StudentId == userId && booking.StudentCompleted)
        {
            return false;
        }

        return true;
    }

    public bool IsJoinWindowOpen(Booking booking)
    {
        return booking.TeachingMode is TeachingMode.Online or TeachingMode.Hybrid &&
            booking.Status is BookingStatus.Paid or BookingStatus.InProgress;
    }

    public void ChangePassword(Guid userId, string currentPassword, string newPassword, string confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(currentPassword))
        {
            throw new InvalidOperationException("ERR_CURRENT_REQUIRED");
        }

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
        {
            throw new InvalidOperationException("ERR_NEW_MIN");
        }

        if (newPassword != confirmPassword)
        {
            throw new InvalidOperationException("ERR_CONFIRM_MISMATCH");
        }

        var user = db.Users.AsNoTracking().FirstOrDefault(u => u.Id == userId)
            ?? throw new InvalidOperationException("ERR_USER_NOT_FOUND");

        if (user.PasswordHash != HashPassword(currentPassword))
        {
            throw new InvalidOperationException("ERR_CURRENT_WRONG");
        }

        db.Users.Update(user with { PasswordHash = HashPassword(newPassword) });
        db.SaveChanges();
    }

    public IEnumerable<User> UsersByRole(UserRole role) =>
        db.Users
            .AsNoTracking()
            .Where(user => user.Role == role)
            .OrderBy(user => user.Name)
            .ToList();

    public IEnumerable<Booking> AllBookings() =>
        db.Bookings
            .AsNoTracking()
            .OrderByDescending(booking => booking.StartTime)
            .ToList();

    public IEnumerable<Transaction> AllTransactions() =>
        db.Transactions
            .AsNoTracking()
            .OrderByDescending(transaction => transaction.CreatedAt)
            .ToList();

    public IEnumerable<Complaint> AllComplaints()
    {
        ApplyComplaintAutoRefunds();
        return db.Complaints
            .AsNoTracking()
            .OrderByDescending(complaint => complaint.CreatedAt)
            .ToList();
    }

    public void SetUserLocked(Guid userId, bool isLocked)
    {
        var user = db.Users.AsNoTracking().FirstOrDefault(item => item.Id == userId)
            ?? throw new InvalidOperationException("Không tìm thấy người dùng.");
        if (user.Role == UserRole.Admin)
        {
            throw new InvalidOperationException("Không khóa tài khoản Admin mặc định từ dashboard demo.");
        }

        db.Users.Update(user with { IsLocked = isLocked });
        db.SaveChanges();
    }

    public Complaint ResolveComplaint(Guid complaintId, string decision)
    {
        var complaint = db.Complaints.AsNoTracking().FirstOrDefault(item => item.Id == complaintId)
            ?? throw new InvalidOperationException("Không tìm thấy khiếu nại.");
        return decision switch
        {
            "Refund" => RefundComplaint(complaint),
            "Release" => ReleaseComplaint(complaint),
            _ => MarkComplaintInReview(complaint)
        };
    }

    private void ApplyComplaintAutoRefunds()
    {
        var expired = db.Complaints
            .AsNoTracking()
            .Where(complaint =>
                complaint.Status == ComplaintStatus.WaitingTeacherResponse &&
                complaint.TeacherRespondedAt == null &&
                complaint.CreatedAt <= DateTimeOffset.UtcNow.AddDays(-7))
            .ToList();
        foreach (var complaint in expired)
        {
            RefundComplaint(complaint);
        }
    }

    private Complaint MarkComplaintInReview(Complaint complaint)
    {
        var updated = complaint with { Status = ComplaintStatus.InReview };
        db.Complaints.Update(updated);
        db.SaveChanges();
        return updated;
    }

    private Complaint RefundComplaint(Complaint complaint)
    {
        var booking = FindBooking(complaint.BookingId);
        var amount = db.TeacherProfiles.AsNoTracking().FirstOrDefault(profile => profile.UserId == booking.TeacherId)?.PricePerSession ?? 0;
        if (!db.Transactions.Any(transaction => transaction.BookingId == booking.Id && transaction.Type == TransactionType.Refund && transaction.Status == TransactionStatus.Refunded))
        {
            db.Transactions.Add(new Transaction(Guid.NewGuid(), booking.Id, amount, TransactionType.Refund, TransactionStatus.Refunded, DateTimeOffset.UtcNow));
        }
        UpdateInvoiceStatus(booking.Id, InvoiceStatus.Refunded);
        UpdateStudentPaymentWalletStatus(booking.Id, "Hoàn tiền");
        AddWalletTransactionIfMissing(booking.StudentId, WalletTransactionType.Refund, "Hệ thống SkillBridge", amount, "Hoàn tiền", booking.Id, null, booking.LessonContent);

        db.Bookings.Update(booking with { Status = BookingStatus.Refunded });
        var updated = complaint with { Status = ComplaintStatus.Refunded, ResolvedAt = DateTimeOffset.UtcNow };
        db.Complaints.Update(updated);
        AddNotification(complaint.StudentId, "💰", "Khiếu nại đã được hoàn tiền", $"Buổi học: {booking.LessonContent}");
        AddNotification(complaint.TeacherId, "!", "Khiếu nại đã được hoàn tiền cho học viên", $"Buổi học: {booking.LessonContent}");
        db.SaveChanges();
        return updated;
    }

    private Complaint ReleaseComplaint(Complaint complaint)
    {
        var booking = FindBooking(complaint.BookingId);
        var amount = db.TeacherProfiles.AsNoTracking().FirstOrDefault(profile => profile.UserId == booking.TeacherId)?.PricePerSession ?? 0;
        if (!db.Transactions.Any(transaction => transaction.BookingId == booking.Id && transaction.Type == TransactionType.Release && transaction.Status == TransactionStatus.Released))
        {
            db.Transactions.Add(new Transaction(Guid.NewGuid(), booking.Id, amount, TransactionType.Release, TransactionStatus.Released, DateTimeOffset.UtcNow));
        }
        UpdateInvoiceStatus(booking.Id, InvoiceStatus.Completed);
        UpdateStudentPaymentWalletStatus(booking.Id, "Hoàn thành");
        AddWalletTransactionIfMissing(booking.TeacherId, WalletTransactionType.Payment, "Hệ thống SkillBridge", amount * 0.90m, "Hoàn thành", booking.Id, null, booking.LessonContent);

        db.Bookings.Update(booking with { Status = BookingStatus.Completed, StudentCompleted = true, TeacherCompleted = true });
        var updated = complaint with { Status = ComplaintStatus.ReleasedToTeacher, ResolvedAt = DateTimeOffset.UtcNow };
        db.Complaints.Update(updated);
        AddNotification(complaint.StudentId, "✅", "Khiếu nại đã được xử lý", $"Buổi học: {booking.LessonContent}");
        AddNotification(complaint.TeacherId, "💰", "Khiếu nại đã được trả tiền cho giáo viên", $"Buổi học: {booking.LessonContent}");
        db.SaveChanges();
        return updated;
    }

    private decimal ReleasedTeacherNet(Guid teacherId)
    {
        var released = db.Transactions
            .AsNoTracking()
            .Where(transaction =>
                transaction.Type == TransactionType.Release &&
                transaction.Status == TransactionStatus.Released &&
                db.Bookings.Any(booking => booking.Id == transaction.BookingId && booking.TeacherId == teacherId))
            .Sum(transaction => transaction.Amount);
        return released * 0.90m;
    }

    private static bool IsTeachingModeAllowed(TeachingMode profileMode, TeachingMode requestedMode) =>
        profileMode == TeachingMode.Hybrid ||
        profileMode == requestedMode;

    private static DateTime? ParseDateOfBirth(string? dateOfBirth, DateTime? currentValue)
    {
        if (dateOfBirth is null)
        {
            return currentValue;
        }

        if (string.IsNullOrWhiteSpace(dateOfBirth))
        {
            return null;
        }

        return DateTime.TryParse(dateOfBirth, out var parsed)
            ? parsed.Date
            : currentValue;
    }

    private void UpdateTeacherRating(Guid teacherId)
    {
        var profile = db.TeacherProfiles.AsNoTracking().FirstOrDefault(item => item.UserId == teacherId);
        if (profile is null)
        {
            return;
        }

        var average = db.TeacherReviews
            .AsNoTracking()
            .Where(review => review.TeacherId == teacherId)
            .Select(review => (decimal)review.Stars)
            .DefaultIfEmpty(profile.Rating)
            .Average();

        db.TeacherProfiles.Update(profile with { Rating = Math.Round(average, 1) });
    }

    private string NextInvoiceCode()
    {
        var suffix = db.PaymentInvoices.Count() + 1;
        return $"SB-{DateTimeOffset.UtcNow:yyyyMMdd}-{suffix:0000}";
    }

    private string NextWalletTransactionCode(string prefix)
    {
        var suffix = db.WalletTransactions.Count() + 1;
        return $"{prefix}-{DateTimeOffset.UtcNow:yyyyMMdd}-{suffix:0000}";
    }

    private void UpdateInvoiceStatus(Guid bookingId, InvoiceStatus status)
    {
        var invoice = db.PaymentInvoices.AsNoTracking().FirstOrDefault(item => item.BookingId == bookingId);
        if (invoice is not null && invoice.Status != status)
        {
            db.PaymentInvoices.Update(invoice with { Status = status, UpdatedAt = DateTimeOffset.UtcNow });
        }
    }

    private void UpdateStudentPaymentWalletStatus(Guid bookingId, string status)
    {
        var rows = db.WalletTransactions
            .Where(transaction => transaction.BookingId == bookingId && transaction.Type == WalletTransactionType.Payment)
            .ToList();
        foreach (var row in rows)
        {
            db.WalletTransactions.Update(row with { Status = status });
        }
    }

    private void AddWalletTransactionIfMissing(Guid userId, WalletTransactionType type, string method, decimal amount, string status, Guid? bookingId, Guid? withdrawalId, string note)
    {
        var exists = db.WalletTransactions.Any(transaction =>
            transaction.UserId == userId &&
            transaction.Type == type &&
            transaction.BookingId == bookingId &&
            transaction.WithdrawalId == withdrawalId &&
            transaction.Status == status);
        if (exists)
        {
            return;
        }

        db.WalletTransactions.Add(new WalletTransaction(
            Guid.NewGuid(),
            userId,
            type,
            method,
            amount,
            status,
            DateTimeOffset.UtcNow,
            bookingId,
            withdrawalId,
            note,
            NextWalletTransactionCode(type == WalletTransactionType.Refund ? "REF" : type == WalletTransactionType.Withdrawal ? "WDR" : type == WalletTransactionType.TopUp ? "DEP" : "PAY")));
    }

    private void AddNotification(Guid userId, string icon, string title, string body)
    {
        db.Notifications.Add(new Notification(
            Guid.NewGuid(),
            userId,
            icon,
            title.Trim(),
            string.IsNullOrWhiteSpace(body) ? "" : body.Trim(),
            false,
            DateTimeOffset.UtcNow));
    }

    public static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }

    private Booking FindBooking(Guid bookingId) =>
        db.Bookings.AsNoTracking().FirstOrDefault(b => b.Id == bookingId)
        ?? throw new InvalidOperationException("Không tìm thấy lịch học.");

    private Booking UpdateBooking(Booking booking)
    {
        db.Bookings.Update(booking);
        db.SaveChanges();
        return booking;
    }

    private AuthResponse ToAuth(User user) =>
        new(user.Id, user.Name, user.Email, user.Role, user.AvatarUrl, user.Bio, Convert.ToBase64String(Guid.NewGuid().ToByteArray()));

    private static TeacherResponse ToTeacherResponse(TeacherProfile teacher, User user) =>
        new(
            teacher.Id,
            teacher.UserId,
            user.Name,
            user.AvatarUrl,
            user.Bio,
            teacher.Skill,
            teacher.Description,
            teacher.Experience,
            teacher.PricePerSession,
            teacher.TeachingMode,
            teacher.OfflineLocation,
            teacher.PortfolioImageUrl,
            teacher.Status,
            teacher.Rating,
            teacher.IsOnline);
}
