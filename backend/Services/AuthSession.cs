using SkillBridge.Api.DTOs;
using SkillBridge.Api.Models;

namespace SkillBridge.Api.Services;

public static class AuthSession
{
    public static void SignIn(HttpResponse response, AuthResponse user)
    {
        var prefix = Prefix(user.Role);
        var otherPrefixes = Enum.GetValues<UserRole>()
            .Where(role => role != user.Role)
            .Select(Prefix)
            .ToList();
        response.Cookies.Append($"{prefix}_user_id", user.Id.ToString(), new CookieOptions { HttpOnly = true, SameSite = SameSiteMode.Lax });
        response.Cookies.Append($"{prefix}_user_name", user.Name, new CookieOptions { HttpOnly = false, SameSite = SameSiteMode.Lax });
        response.Cookies.Append($"{prefix}_user_role", user.Role.ToString(), new CookieOptions { HttpOnly = false, SameSite = SameSiteMode.Lax });

        foreach (var otherPrefix in otherPrefixes)
        {
            response.Cookies.Delete($"{otherPrefix}_user_id");
            response.Cookies.Delete($"{otherPrefix}_user_name");
            response.Cookies.Delete($"{otherPrefix}_user_role");
        }
        response.Cookies.Delete("skillbridge_user_id");
        response.Cookies.Delete("skillbridge_user_name");
        response.Cookies.Delete("skillbridge_user_role");
    }

    public static void SignOut(HttpResponse response, UserRole role)
    {
        var prefix = Prefix(role);
        response.Cookies.Delete($"{prefix}_user_id");
        response.Cookies.Delete($"{prefix}_user_name");
        response.Cookies.Delete($"{prefix}_user_role");
    }

    public static UserRole? RoleFor(HttpContext context)
    {
        if (Enum.TryParse<UserRole>(context.Request.Query["role"], out var queryRole))
        {
            return queryRole;
        }

        var path = context.Request.Path.Value ?? "";
        if (path.StartsWith("/TeacherDashboard", StringComparison.OrdinalIgnoreCase))
        {
            return UserRole.Teacher;
        }

        if (path.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase))
        {
            return UserRole.Admin;
        }

        if (path.StartsWith("/Dashboard", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/Bookings", StringComparison.OrdinalIgnoreCase))
        {
            return UserRole.Student;
        }

        if (path.StartsWith("/Teachers", StringComparison.OrdinalIgnoreCase))
        {
            if (context.Request.Cookies.ContainsKey($"{Prefix(UserRole.Teacher)}_user_id"))
            {
                return UserRole.Teacher;
            }

            return context.Request.Cookies.ContainsKey($"{Prefix(UserRole.Student)}_user_id") ? UserRole.Student : null;
        }

        if (context.Request.Cookies.ContainsKey($"{Prefix(UserRole.Student)}_user_id"))
        {
            return UserRole.Student;
        }

        if (context.Request.Cookies.ContainsKey($"{Prefix(UserRole.Teacher)}_user_id"))
        {
            return UserRole.Teacher;
        }

        return context.Request.Cookies.ContainsKey($"{Prefix(UserRole.Admin)}_user_id") ? UserRole.Admin : null;
    }

    public static Guid? UserId(HttpContext context, UserRole role) =>
        Guid.TryParse(context.Request.Cookies[$"{Prefix(role)}_user_id"], out var id) ? id : null;

    public static string? UserName(HttpContext context, UserRole role) =>
        context.Request.Cookies[$"{Prefix(role)}_user_name"];

    public static bool IsSignedIn(HttpContext context, UserRole role) =>
        context.Request.Cookies.ContainsKey($"{Prefix(role)}_user_id");

    public static string Prefix(UserRole role) =>
        role switch
        {
            UserRole.Teacher => "skillbridge_teacher",
            UserRole.Admin => "skillbridge_admin",
            _ => "skillbridge_student"
        };
}
