using System.Security.Claims;

namespace SmartParking.Api.Auth;

public static class AppClaims
{
    public const string Id = "id";
    public const string Role = "role";
    public const string Name = "name";
    public const string Email = "email";

    // Role names (match the ENUM values in the users table)
    public const string User = "user";
    public const string Owner = "parking_owner";
    public const string Admin = "admin";

    public static int GetUserId(this ClaimsPrincipal principal) =>
        int.Parse(principal.FindFirst(Id)?.Value ?? throw new InvalidOperationException("Missing id claim."));

    public static string GetRole(this ClaimsPrincipal principal) => principal.FindFirst(Role)?.Value ?? "";

    public static bool IsAdmin(this ClaimsPrincipal principal) => principal.GetRole() == Admin;
}
