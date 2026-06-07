# SkillBridge Development

## Stack hiện tại

- Ngôn ngữ: C#
- Framework: ASP.NET Core MVC
- Cơ sở dữ liệu: SQL Server ready qua EF Core SQL Server
- Frontend: Razor Views, HTML, CSS, JavaScript, Bootstrap

## Run MVC app

```powershell
dotnet run --project backend\SkillBridge.Api.csproj --urls http://localhost:5123
```

Open:

```text
http://localhost:5123
```

Các trang MVC chính:

```text
http://localhost:5123/
http://localhost:5123/Teachers
http://localhost:5123/Account/Login
http://localhost:5123/Dashboard
http://localhost:5123/Bookings
http://localhost:5123/Chat
```

## SQL Server

Connection string nằm trong:

```text
backend/appsettings.json
```

Schema SQL Server nằm trong:

```text
backend/Database/schema.sql
```

Mặc định connection string dùng LocalDB:

```text
Server=(localdb)\MSSQLLocalDB;Database=SkillBridgeDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True
```

Khi app khởi động, EF Core sẽ:

- Kết nối SQL Server bằng `DefaultConnection`
- Tự tạo database/tables nếu chưa có
- Seed 5 users demo và 4 teacher profiles nếu database đang trống

Kiểm tra database bằng `sqlcmd`:

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -d SkillBridgeDb -Q "SELECT COUNT(*) FROM dbo.Users; SELECT COUNT(*) FROM dbo.TeacherProfiles;"
```

Backend API docs:

```text
http://localhost:5123/api/docs
```

Health check:

```text
http://localhost:5123/api/health
```

If port `5123` is already in use:

```powershell
netstat -ano | Select-String ':5123'
Stop-Process -Id <PID>
dotnet run --project backend\SkillBridge.Api.csproj --urls http://localhost:5123
```

## Demo Accounts

```text
student@skillbridge.dev / password
minh@skillbridge.dev / password
an@skillbridge.dev / password
quyen@skillbridge.dev / password
duy@skillbridge.dev / password
```

## Implemented MVP

- ASP.NET Core API
- SignalR hub for chat/call events
- In-memory users, teacher profiles, bookings, messages, and transactions
- Booking lifecycle: pending, confirmed, paid, released/cancelled
- Escrow simulation with hold and release transactions
- Static responsive frontend served from `wwwroot`

## Notes

This MVP avoids external NuGet or npm dependencies so it can run in the current environment, where `node/npm` are not installed. The frontend loads the SignalR browser client from CDN when internet access is available and falls back to REST message sending if the client script is unavailable.
