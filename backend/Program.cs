using SkillBridge.Api.Data;
using SkillBridge.Api.DTOs;
using SkillBridge.Api.Hubs;
using SkillBridge.Api.Models;
using SkillBridge.Api.Services;
using System.Text;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<SkillBridgeDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<SkillBridgeService>();
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()
        .SetIsOriginAllowed(_ => true));
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SkillBridgeDbContext>();
    db.Database.EnsureCreated();
    EnsureTeacherProfileSchema(db);
    NormalizeBookingStatuses(db);
    DatabaseSeeder.Seed(db);
}

static void EnsureTeacherProfileSchema(SkillBridgeDbContext db)
{
    db.Database.ExecuteSqlRaw("""
        IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL AND COL_LENGTH('dbo.Users', 'IsLocked') IS NULL
            ALTER TABLE dbo.Users ADD IsLocked bit NOT NULL CONSTRAINT DF_Users_IsLocked DEFAULT 0;
        IF COL_LENGTH('dbo.TeacherProfiles', 'Experience') IS NULL
            ALTER TABLE dbo.TeacherProfiles ADD Experience nvarchar(1200) NOT NULL CONSTRAINT DF_TeacherProfiles_Experience DEFAULT '';
        IF COL_LENGTH('dbo.TeacherProfiles', 'OfflineLocation') IS NULL
            ALTER TABLE dbo.TeacherProfiles ADD OfflineLocation nvarchar(300) NOT NULL CONSTRAINT DF_TeacherProfiles_OfflineLocation DEFAULT '';
        IF COL_LENGTH('dbo.TeacherProfiles', 'PortfolioImageUrl') IS NULL
            ALTER TABLE dbo.TeacherProfiles ADD PortfolioImageUrl nvarchar(700) NOT NULL CONSTRAINT DF_TeacherProfiles_PortfolioImageUrl DEFAULT '';
        IF COL_LENGTH('dbo.TeacherProfiles', 'Status') IS NULL
            ALTER TABLE dbo.TeacherProfiles ADD Status nvarchar(32) NOT NULL CONSTRAINT DF_TeacherProfiles_Status DEFAULT 'Draft';
        IF COL_LENGTH('dbo.TeacherProfiles', 'DefaultPayoutMethod') IS NULL
            ALTER TABLE dbo.TeacherProfiles ADD DefaultPayoutMethod nvarchar(80) NOT NULL CONSTRAINT DF_TeacherProfiles_DefaultPayoutMethod DEFAULT N'Tài khoản ngân hàng';
        IF COL_LENGTH('dbo.TeacherProfiles', 'DefaultPayoutBank') IS NULL
            ALTER TABLE dbo.TeacherProfiles ADD DefaultPayoutBank nvarchar(80) NOT NULL CONSTRAINT DF_TeacherProfiles_DefaultPayoutBank DEFAULT 'Vietcombank';
        IF OBJECT_ID('dbo.StudentProfiles', 'U') IS NULL
            CREATE TABLE dbo.StudentProfiles (
                Id uniqueidentifier NOT NULL CONSTRAINT PK_StudentProfiles PRIMARY KEY,
                UserId uniqueidentifier NOT NULL,
                DisplayName nvarchar(120) NOT NULL,
                Email nvarchar(180) NOT NULL,
                Gender nvarchar(40) NOT NULL CONSTRAINT DF_StudentProfiles_Gender DEFAULT '',
                DateOfBirth date NULL,
                Phone nvarchar(40) NOT NULL CONSTRAINT DF_StudentProfiles_Phone DEFAULT '',
                LearningGoal nvarchar(700) NOT NULL CONSTRAINT DF_StudentProfiles_LearningGoal DEFAULT '',
                Bio nvarchar(1000) NOT NULL CONSTRAINT DF_StudentProfiles_Bio DEFAULT '',
                AvatarUrl nvarchar(500) NOT NULL CONSTRAINT DF_StudentProfiles_AvatarUrl DEFAULT '',
                UpdatedAt datetimeoffset NOT NULL
            );
        IF OBJECT_ID('dbo.StudentProfiles', 'U') IS NOT NULL
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StudentProfiles_UserId' AND object_id = OBJECT_ID('dbo.StudentProfiles'))
                CREATE UNIQUE INDEX IX_StudentProfiles_UserId ON dbo.StudentProfiles(UserId);
        IF OBJECT_ID('dbo.TeacherAvailabilities', 'U') IS NULL
            CREATE TABLE dbo.TeacherAvailabilities (
                Id uniqueidentifier NOT NULL CONSTRAINT PK_TeacherAvailabilities PRIMARY KEY,
                TeacherId uniqueidentifier NOT NULL,
                DayOfWeek nvarchar(32) NOT NULL,
                StartTime time NOT NULL,
                EndTime time NOT NULL,
                IsActive bit NOT NULL,
                PlannedContent nvarchar(500) NOT NULL CONSTRAINT DF_TeacherAvailabilities_PlannedContent DEFAULT '',
                TeachingMode nvarchar(32) NOT NULL CONSTRAINT DF_TeacherAvailabilities_TeachingMode DEFAULT 'Online',
                OfflineLocation nvarchar(300) NOT NULL CONSTRAINT DF_TeacherAvailabilities_OfflineLocation DEFAULT ''
            );
        IF OBJECT_ID('dbo.TeacherAvailabilities', 'U') IS NOT NULL AND COL_LENGTH('dbo.TeacherAvailabilities', 'PlannedContent') IS NULL
            ALTER TABLE dbo.TeacherAvailabilities ADD PlannedContent nvarchar(500) NOT NULL CONSTRAINT DF_TeacherAvailabilities_PlannedContent DEFAULT '';
        IF OBJECT_ID('dbo.TeacherAvailabilities', 'U') IS NOT NULL AND COL_LENGTH('dbo.TeacherAvailabilities', 'TeachingMode') IS NULL
            ALTER TABLE dbo.TeacherAvailabilities ADD TeachingMode nvarchar(32) NOT NULL CONSTRAINT DF_TeacherAvailabilities_TeachingMode DEFAULT 'Online';
        IF OBJECT_ID('dbo.TeacherAvailabilities', 'U') IS NOT NULL AND COL_LENGTH('dbo.TeacherAvailabilities', 'OfflineLocation') IS NULL
            ALTER TABLE dbo.TeacherAvailabilities ADD OfflineLocation nvarchar(300) NOT NULL CONSTRAINT DF_TeacherAvailabilities_OfflineLocation DEFAULT '';
        IF OBJECT_ID('dbo.ScheduleChangeRequests', 'U') IS NULL
            CREATE TABLE dbo.ScheduleChangeRequests (
                Id uniqueidentifier NOT NULL CONSTRAINT PK_ScheduleChangeRequests PRIMARY KEY,
                BookingId uniqueidentifier NOT NULL,
                StudentId uniqueidentifier NOT NULL,
                TeacherId uniqueidentifier NOT NULL,
                CurrentStartTime datetimeoffset NOT NULL,
                RequestedStartTime datetimeoffset NULL,
                Type nvarchar(32) NOT NULL,
                Reason nvarchar(500) NOT NULL,
                Status nvarchar(32) NOT NULL,
                CreatedAt datetimeoffset NOT NULL,
                ResolvedAt datetimeoffset NULL
            );
        IF OBJECT_ID('dbo.Bookings', 'U') IS NOT NULL AND COL_LENGTH('dbo.Bookings', 'TeachingMode') IS NULL
            ALTER TABLE dbo.Bookings ADD TeachingMode nvarchar(32) NOT NULL CONSTRAINT DF_Bookings_TeachingMode DEFAULT 'Online';
        IF OBJECT_ID('dbo.Bookings', 'U') IS NOT NULL AND COL_LENGTH('dbo.Bookings', 'OfflineLocation') IS NULL
            ALTER TABLE dbo.Bookings ADD OfflineLocation nvarchar(300) NOT NULL CONSTRAINT DF_Bookings_OfflineLocation DEFAULT '';
        IF OBJECT_ID('dbo.Bookings', 'U') IS NOT NULL AND COL_LENGTH('dbo.Bookings', 'LessonContent') IS NULL
            ALTER TABLE dbo.Bookings ADD LessonContent nvarchar(500) NOT NULL CONSTRAINT DF_Bookings_LessonContent DEFAULT N'Ôn tập mục tiêu và luyện đề';
        IF OBJECT_ID('dbo.Bookings', 'U') IS NOT NULL AND COL_LENGTH('dbo.Bookings', 'StudentCompleted') IS NULL
            ALTER TABLE dbo.Bookings ADD StudentCompleted bit NOT NULL CONSTRAINT DF_Bookings_StudentCompleted DEFAULT 0;
        IF OBJECT_ID('dbo.Bookings', 'U') IS NOT NULL AND COL_LENGTH('dbo.Bookings', 'TeacherCompleted') IS NULL
            ALTER TABLE dbo.Bookings ADD TeacherCompleted bit NOT NULL CONSTRAINT DF_Bookings_TeacherCompleted DEFAULT 0;
        IF OBJECT_ID('dbo.Messages', 'U') IS NOT NULL AND COL_LENGTH('dbo.Messages', 'IsDeleted') IS NULL
            ALTER TABLE dbo.Messages ADD IsDeleted bit NOT NULL CONSTRAINT DF_Messages_IsDeleted DEFAULT 0;
        IF OBJECT_ID('dbo.Messages', 'U') IS NOT NULL AND COL_LENGTH('dbo.Messages', 'IsRead') IS NULL
            ALTER TABLE dbo.Messages ADD IsRead bit NOT NULL CONSTRAINT DF_Messages_IsRead DEFAULT 0;
        IF OBJECT_ID('dbo.Messages', 'U') IS NOT NULL
        BEGIN
            UPDATE dbo.Messages SET IsRead = 0 WHERE IsRead IS NULL;
            UPDATE dbo.Messages SET IsDeleted = 0 WHERE IsDeleted IS NULL;
            IF NOT EXISTS (
                SELECT 1
                FROM sys.default_constraints
                WHERE parent_object_id = OBJECT_ID('dbo.Messages')
                  AND parent_column_id = COLUMNPROPERTY(OBJECT_ID('dbo.Messages'), 'IsRead', 'ColumnId')
            )
                ALTER TABLE dbo.Messages ADD CONSTRAINT DF_Messages_IsRead DEFAULT 0 FOR IsRead;
            IF NOT EXISTS (
                SELECT 1
                FROM sys.default_constraints
                WHERE parent_object_id = OBJECT_ID('dbo.Messages')
                  AND parent_column_id = COLUMNPROPERTY(OBJECT_ID('dbo.Messages'), 'IsDeleted', 'ColumnId')
            )
                ALTER TABLE dbo.Messages ADD CONSTRAINT DF_Messages_IsDeleted DEFAULT 0 FOR IsDeleted;
        END
        IF OBJECT_ID('dbo.Notifications', 'U') IS NULL
            CREATE TABLE dbo.Notifications (
                Id uniqueidentifier NOT NULL CONSTRAINT PK_Notifications PRIMARY KEY,
                UserId uniqueidentifier NOT NULL,
                Icon nvarchar(16) NOT NULL,
                Title nvarchar(220) NOT NULL,
                Body nvarchar(600) NOT NULL,
                IsRead bit NOT NULL CONSTRAINT DF_Notifications_IsRead DEFAULT 0,
                CreatedAt datetimeoffset NOT NULL
            );
        IF OBJECT_ID('dbo.Notifications', 'U') IS NOT NULL AND COL_LENGTH('dbo.Notifications', 'IsRead') IS NULL
            ALTER TABLE dbo.Notifications ADD IsRead bit NOT NULL CONSTRAINT DF_Notifications_IsRead DEFAULT 0;
        IF OBJECT_ID('dbo.Notifications', 'U') IS NOT NULL
        BEGIN
            UPDATE dbo.Notifications SET IsRead = 0 WHERE IsRead IS NULL;
            IF NOT EXISTS (
                SELECT 1
                FROM sys.default_constraints
                WHERE parent_object_id = OBJECT_ID('dbo.Notifications')
                  AND parent_column_id = COLUMNPROPERTY(OBJECT_ID('dbo.Notifications'), 'IsRead', 'ColumnId')
            )
                ALTER TABLE dbo.Notifications ADD CONSTRAINT DF_Notifications_IsRead DEFAULT 0 FOR IsRead;
        END
        IF OBJECT_ID('dbo.TeacherReviews', 'U') IS NULL
            CREATE TABLE dbo.TeacherReviews (
                Id uniqueidentifier NOT NULL CONSTRAINT PK_TeacherReviews PRIMARY KEY,
                BookingId uniqueidentifier NOT NULL,
                TeacherId uniqueidentifier NOT NULL,
                StudentId uniqueidentifier NOT NULL,
                Stars int NOT NULL,
                Comment nvarchar(1000) NOT NULL CONSTRAINT DF_TeacherReviews_Comment DEFAULT '',
                CreatedAt datetimeoffset NOT NULL
            );
        IF OBJECT_ID('dbo.TeacherReviews', 'U') IS NOT NULL
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TeacherReviews_BookingId' AND object_id = OBJECT_ID('dbo.TeacherReviews'))
                CREATE UNIQUE INDEX IX_TeacherReviews_BookingId ON dbo.TeacherReviews(BookingId);
        IF OBJECT_ID('dbo.ConversationHides', 'U') IS NULL
            CREATE TABLE dbo.ConversationHides (
                Id uniqueidentifier NOT NULL CONSTRAINT PK_ConversationHides PRIMARY KEY,
                UserId uniqueidentifier NOT NULL,
                ContactUserId uniqueidentifier NOT NULL,
                HiddenAt datetimeoffset NOT NULL
            );
        IF OBJECT_ID('dbo.ConversationHides', 'U') IS NOT NULL
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ConversationHides_UserId_ContactUserId' AND object_id = OBJECT_ID('dbo.ConversationHides'))
                CREATE UNIQUE INDEX IX_ConversationHides_UserId_ContactUserId ON dbo.ConversationHides(UserId, ContactUserId);
        IF OBJECT_ID('dbo.Withdrawals', 'U') IS NULL
            CREATE TABLE dbo.Withdrawals (
                Id uniqueidentifier NOT NULL CONSTRAINT PK_Withdrawals PRIMARY KEY,
                TeacherId uniqueidentifier NOT NULL,
                Amount decimal(18,2) NOT NULL,
                Status nvarchar(32) NOT NULL,
                CreatedAt datetimeoffset NOT NULL,
                ProcessedAt datetimeoffset NULL,
                Method nvarchar(80) NOT NULL CONSTRAINT DF_Withdrawals_Method DEFAULT N'Tài khoản ngân hàng',
                AccountName nvarchar(120) NOT NULL CONSTRAINT DF_Withdrawals_AccountName DEFAULT '',
                AccountNumber nvarchar(120) NOT NULL CONSTRAINT DF_Withdrawals_AccountNumber DEFAULT '',
                BankName nvarchar(120) NOT NULL CONSTRAINT DF_Withdrawals_BankName DEFAULT ''
            );
        IF OBJECT_ID('dbo.Withdrawals', 'U') IS NOT NULL AND COL_LENGTH('dbo.Withdrawals', 'Method') IS NULL
            ALTER TABLE dbo.Withdrawals ADD Method nvarchar(80) NOT NULL CONSTRAINT DF_Withdrawals_Method DEFAULT N'Tài khoản ngân hàng';
        IF OBJECT_ID('dbo.Withdrawals', 'U') IS NOT NULL AND COL_LENGTH('dbo.Withdrawals', 'AccountName') IS NULL
            ALTER TABLE dbo.Withdrawals ADD AccountName nvarchar(120) NOT NULL CONSTRAINT DF_Withdrawals_AccountName DEFAULT '';
        IF OBJECT_ID('dbo.Withdrawals', 'U') IS NOT NULL AND COL_LENGTH('dbo.Withdrawals', 'AccountNumber') IS NULL
            ALTER TABLE dbo.Withdrawals ADD AccountNumber nvarchar(120) NOT NULL CONSTRAINT DF_Withdrawals_AccountNumber DEFAULT '';
        IF OBJECT_ID('dbo.Withdrawals', 'U') IS NOT NULL AND COL_LENGTH('dbo.Withdrawals', 'BankName') IS NULL
            ALTER TABLE dbo.Withdrawals ADD BankName nvarchar(120) NOT NULL CONSTRAINT DF_Withdrawals_BankName DEFAULT '';
        IF OBJECT_ID('dbo.Complaints', 'U') IS NULL
            CREATE TABLE dbo.Complaints (
                Id uniqueidentifier NOT NULL CONSTRAINT PK_Complaints PRIMARY KEY,
                BookingId uniqueidentifier NOT NULL,
                StudentId uniqueidentifier NOT NULL,
                TeacherId uniqueidentifier NOT NULL,
                Reason nvarchar(1000) NOT NULL,
                StudentEvidenceUrl nvarchar(700) NOT NULL CONSTRAINT DF_Complaints_StudentEvidenceUrl DEFAULT '',
                TeacherResponse nvarchar(1200) NOT NULL CONSTRAINT DF_Complaints_TeacherResponse DEFAULT '',
                TeacherEvidenceUrl nvarchar(700) NOT NULL CONSTRAINT DF_Complaints_TeacherEvidenceUrl DEFAULT '',
                Status nvarchar(40) NOT NULL,
                CreatedAt datetimeoffset NOT NULL,
                TeacherRespondedAt datetimeoffset NULL,
                ResolvedAt datetimeoffset NULL
            );
        IF OBJECT_ID('dbo.Complaints', 'U') IS NOT NULL
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Complaints_BookingId' AND object_id = OBJECT_ID('dbo.Complaints'))
                CREATE UNIQUE INDEX IX_Complaints_BookingId ON dbo.Complaints(BookingId);
        IF OBJECT_ID('dbo.PaymentInvoices', 'U') IS NULL
            CREATE TABLE dbo.PaymentInvoices (
                Id uniqueidentifier NOT NULL CONSTRAINT PK_PaymentInvoices PRIMARY KEY,
                InvoiceCode nvarchar(40) NOT NULL,
                BookingId uniqueidentifier NOT NULL,
                StudentId uniqueidentifier NOT NULL,
                TeacherId uniqueidentifier NOT NULL,
                LessonContent nvarchar(500) NOT NULL,
                TeachingMode nvarchar(32) NOT NULL,
                Amount decimal(18,2) NOT NULL,
                PaymentMethod nvarchar(80) NOT NULL,
                Status nvarchar(32) NOT NULL,
                PaidAt datetimeoffset NOT NULL,
                UpdatedAt datetimeoffset NULL
            );
        IF OBJECT_ID('dbo.PaymentInvoices', 'U') IS NOT NULL
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PaymentInvoices_BookingId' AND object_id = OBJECT_ID('dbo.PaymentInvoices'))
                CREATE UNIQUE INDEX IX_PaymentInvoices_BookingId ON dbo.PaymentInvoices(BookingId);
        IF OBJECT_ID('dbo.WalletTransactions', 'U') IS NULL
            CREATE TABLE dbo.WalletTransactions (
                Id uniqueidentifier NOT NULL CONSTRAINT PK_WalletTransactions PRIMARY KEY,
                UserId uniqueidentifier NOT NULL,
                Type nvarchar(32) NOT NULL,
                Method nvarchar(80) NOT NULL,
                Amount decimal(18,2) NOT NULL,
                Status nvarchar(80) NOT NULL,
                CreatedAt datetimeoffset NOT NULL,
                BookingId uniqueidentifier NULL,
                WithdrawalId uniqueidentifier NULL,
                Note nvarchar(500) NOT NULL CONSTRAINT DF_WalletTransactions_Note DEFAULT '',
                TransactionCode nvarchar(40) NOT NULL CONSTRAINT DF_WalletTransactions_TransactionCode DEFAULT ''
            );
        IF OBJECT_ID('dbo.WalletTransactions', 'U') IS NOT NULL AND COL_LENGTH('dbo.WalletTransactions', 'TransactionCode') IS NULL
            ALTER TABLE dbo.WalletTransactions ADD TransactionCode nvarchar(40) NOT NULL CONSTRAINT DF_WalletTransactions_TransactionCode DEFAULT '';
        """);
}

static void NormalizeBookingStatuses(SkillBridgeDbContext db)
{
    db.Database.ExecuteSqlRaw("""
        IF OBJECT_ID('dbo.Bookings', 'U') IS NOT NULL
        BEGIN
            UPDATE dbo.Bookings SET Status = 'Completed' WHERE Status = 'Released';
            UPDATE dbo.Bookings SET Status = 'Cancelled' WHERE Status = 'Disputed';
        END
        """);
}

app.UseCors();
app.UseStaticFiles();
app.UseRouting();

static IResult Handle(Func<IResult> action)
{
    try
    {
        return action();
    }
    catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}

app.MapGet("/api/health", () => Results.Ok(new
{
    status = "ok",
    app = "SkillBridge.Api",
    time = DateTimeOffset.UtcNow
}));

app.MapGet("/api", () => Results.Redirect("/api/docs"));

app.MapGet("/api/docs", () =>
{
    var html = """
    <!doctype html>
    <html lang="vi">
    <head>
      <meta charset="utf-8">
      <meta name="viewport" content="width=device-width, initial-scale=1">
      <title>SkillBridge API</title>
      <style>
        body{font-family:Inter,Segoe UI,Arial,sans-serif;background:#0f172a;color:#e5e7eb;margin:0;padding:32px;line-height:1.55}
        main{max-width:980px;margin:auto}
        h1{margin-top:0;font-size:36px}
        section{background:#1e293b;border:1px solid rgba(148,163,184,.22);border-radius:8px;padding:18px;margin:14px 0}
        code,pre{background:#111827;border-radius:6px}
        code{padding:2px 6px}
        pre{padding:14px;overflow:auto}
        a{color:#93c5fd}
        .method{display:inline-block;min-width:64px;font-weight:800;color:#86efac}
      </style>
    </head>
    <body>
      <main>
        <h1>SkillBridge API</h1>
        <p>Backend đang chạy. Frontend: <a href="/">/</a>. Health check: <a href="/api/health">/api/health</a>.</p>
        <section>
          <h2>Demo accounts</h2>
          <pre>student@skillbridge.dev / password
    minh@skillbridge.dev / password
    an@skillbridge.dev / password
    quyen@skillbridge.dev / password
    duy@skillbridge.dev / password</pre>
        </section>
        <section>
          <h2>Endpoints</h2>
          <p><span class="method">POST</span><code>/api/auth/login</code></p>
          <p><span class="method">POST</span><code>/api/auth/register</code></p>
          <p><span class="method">GET</span><code>/api/users</code></p>
          <p><span class="method">GET</span><code>/api/teachers?query=&amp;mode=Online</code></p>
          <p><span class="method">POST</span><code>/api/bookings</code></p>
          <p><span class="method">GET</span><code>/api/bookings?userId={userId}</code></p>
          <p><span class="method">POST</span><code>/api/bookings/{id}/confirm</code></p>
          <p><span class="method">POST</span><code>/api/bookings/{id}/pay</code></p>
          <p><span class="method">POST</span><code>/api/bookings/{id}/complete</code></p>
          <p><span class="method">GET</span><code>/api/messages?userA={id}&amp;userB={id}</code></p>
          <p><span class="method">POST</span><code>/api/messages</code></p>
          <p><span class="method">GET</span><code>/api/calls/{bookingId}/access?userId={userId}</code></p>
          <p><span class="method">WS</span><code>/hubs/skillbridge</code></p>
        </section>
        <section>
          <h2>Login example</h2>
          <pre>Invoke-RestMethod http://localhost:5123/api/auth/login `
      -Method Post `
      -ContentType "application/json" `
      -Body '{"email":"student@skillbridge.dev","password":"password"}'</pre>
        </section>
      </main>
    </body>
    </html>
    """;

    return Results.Content(html, "text/html", Encoding.UTF8);
});

app.MapGet("/api/users", (SkillBridgeDbContext db) =>
    Results.Ok(db.Users.AsNoTracking().Select(user => new
    {
        user.Id,
        user.Name,
        user.Email,
        user.Role,
        user.AvatarUrl,
        user.Bio,
        user.CreatedAt
    })));

app.MapPost("/api/auth/register", (AuthRequest request, SkillBridgeService service) =>
    Handle(() => Results.Ok(service.Register(request))));

app.MapPost("/api/auth/login", (LoginRequest request, SkillBridgeService service) =>
    Handle(() => Results.Ok(service.Login(request))));

app.MapGet("/api/teachers", (string? query, TeachingMode? mode, SkillBridgeService service) =>
    Results.Ok(service.SearchTeachers(query, mode)));

app.MapGet("/api/teachers/{id:guid}", (Guid id, SkillBridgeService service) =>
    service.SearchTeachers(null, null).FirstOrDefault(t => t.Id == id) is { } teacher
        ? Results.Ok(teacher)
        : Results.NotFound());

app.MapPost("/api/bookings", (BookingRequest request, SkillBridgeService service) =>
    Handle(() => Results.Ok(service.CreateBooking(request))));

app.MapGet("/api/bookings", (Guid userId, SkillBridgeService service) =>
    Results.Ok(service.BookingsForUser(userId)));

app.MapPost("/api/bookings/{id:guid}/confirm", (Guid id, BookingDecisionRequest request, SkillBridgeService service) =>
    Handle(() => Results.Ok(service.ConfirmBooking(id, request))));

app.MapPost("/api/bookings/{id:guid}/reject", (Guid id, BookingDecisionRequest request, SkillBridgeService service) =>
    Handle(() => Results.Ok(service.RejectBooking(id, request))));

app.MapPost("/api/bookings/{id:guid}/pay", (Guid id, PaymentRequest request, SkillBridgeService service) =>
    Handle(() => Results.Ok(service.Pay(id, request))));

app.MapPost("/api/bookings/{id:guid}/complete", (Guid id, CompleteBookingRequest request, SkillBridgeService service) =>
    Handle(() => Results.Ok(service.Complete(id, request))));

app.MapPost("/api/bookings/{id:guid}/teacher-complete", (Guid id, TeacherCompleteBookingRequest request, SkillBridgeService service) =>
    Handle(() => Results.Ok(service.TeacherComplete(id, request))));

app.MapGet("/api/bookings/{id:guid}/transactions", (Guid id, SkillBridgeService service) =>
    Results.Ok(service.TransactionsForBooking(id)));

app.MapGet("/api/messages", (Guid userA, Guid userB, SkillBridgeService service) =>
{
    service.MarkConversationRead(userA, userB);
    return Results.Ok(service.ConversationForViewer(userA, userB));
});

app.MapPost("/api/messages", (SendMessageRequest request, SkillBridgeService service) =>
    Handle(() => Results.Ok(service.SaveMessage(request))));

app.MapPost("/api/messages/{id:guid}/delete", (Guid id, DeleteMessageRequest request, SkillBridgeService service) =>
    Handle(() => Results.Ok(service.DeleteMessage(id, request.UserId))));

app.MapPost("/api/messages/read", (MarkConversationReadRequest request, SkillBridgeService service) =>
    Handle(() =>
    {
        service.MarkConversationRead(request.UserId, request.ContactUserId);
        return Results.Ok(new { success = true, unreadChat = service.UnreadChatCount(request.UserId) });
    }));

app.MapGet("/api/messages/unread-count", (Guid userId, SkillBridgeService service) =>
    Results.Ok(new { unreadChat = service.UnreadChatCount(userId) }));

app.MapPost("/api/conversations/hide", (HideConversationRequest request, SkillBridgeService service) =>
    Handle(() => Results.Ok(service.HideConversation(request.UserId, request.ContactUserId))));

app.MapGet("/api/calls/{bookingId:guid}/access", (Guid bookingId, Guid userId, SkillBridgeService service) =>
    Results.Ok(new { canJoin = service.CanJoinCall(bookingId, userId) }));

app.MapHub<SkillBridgeHub>("/hubs/skillbridge");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
