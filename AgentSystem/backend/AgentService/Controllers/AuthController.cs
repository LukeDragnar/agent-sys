using AgentCore.Services;
using AgentInfrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgentService.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly IUserRepository _users;

    public AuthController(IAuthService auth, IUserRepository users)
    {
        _auth = auth;
        _users = users;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest rq)
    {
        var token = await _auth.Login(rq.Username, rq.Password);
        if (token is null) return Unauthorized(new { message = "Sai tên đăng nhập hoặc mật khẩu." });
        return Ok(new { token });
    }

    [HttpPost("setup")]
    [AllowAnonymous]
    public async Task<IActionResult> Setup([FromBody] SetupRequest rq,
        [FromServices] IConfiguration config)
    {
        var setupKey = config["SetupKey"];
        if (string.IsNullOrEmpty(setupKey) || rq.SetupKey != setupKey)
            return Forbid();

        var existing = await _users.GetByUsername(rq.Username);
        if (existing is not null)
            return Conflict(new { message = "User already exists." });

        var hash = AuthService.HashPassword(rq.Password);
        var user = await _users.Create(rq.Username, hash, "admin", 0);
        return Ok(new { user.Id, user.Username, user.Role });
    }
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class SetupRequest
{
    public string SetupKey { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
