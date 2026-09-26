using Microsoft.AspNetCore.Mvc;
using SmartParking.Api.Auth;
using SmartParking.Api.Data;
using SmartParking.Api.Models;

namespace SmartParking.Api.Infrastructure;

/// <summary>Shared helpers for all controllers.</summary>
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected readonly Db Db;
    protected ApiControllerBase(Db db) => Db = db;

    /// <summary>Returns { "error": message } with the given status code, like the Node API.</summary>
    protected ObjectResult Error(int statusCode, string message) =>
        StatusCode(statusCode, new ErrorResponse(message));

    protected int CurrentUserId => User.GetUserId();
    protected string CurrentRole => User.GetRole();
    protected bool IsAdmin => User.IsAdmin();
}
