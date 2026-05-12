using InteriorAI.Models.DTOs;
using InteriorAI.Services;
using Microsoft.AspNetCore.Mvc;

namespace InteriorAI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthManager _authManager;

        // Bắt buộc phải có Constructor này để nhận AuthManager
        public AuthController(AuthManager authManager)
        {
            _authManager = authManager;
        }

        // POST /api/auth/register-device
        [HttpPost("register-device")]
        public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceRequest request)
        {
            if (string.IsNullOrEmpty(request.UserId))
                return BadRequest("Thiếu UserId");

            var user = await _authManager.RegisterOrGetUserAsync(request.UserId, request.Name);

            return Ok(new UserProfileResponse
            {
                UserId = user.Id,
                Name = user.Name,
                AvatarUrl = user.AvatarUrl,
                CreatedAt = user.CreatedAt
            });
        }

        // GET /api/auth/profile/{userId}
        [HttpGet("profile/{userId}")]
        public async Task<IActionResult> GetProfile(string userId)
        {
            var user = await _authManager.GetUserAsync(userId);

            // Check nếu không tìm thấy user trong Database
            if (user == null)
                return NotFound(new { message = "Không tìm thấy người dùng" });

            return Ok(new UserProfileResponse
            {
                UserId = user.Id,
                Name = user.Name,
                AvatarUrl = user.AvatarUrl,
                CreatedAt = user.CreatedAt
            });
        }

        // PUT /api/auth/profile/{userId}
        [HttpPut("profile/{userId}")]
        public async Task<IActionResult> UpdateProfile(string userId, [FromBody] UpdateProfileRequest request)
        {
            var user = await _authManager.UpdateUserProfileAsync(userId, request.Name, request.AvatarUrl);

            // Check nếu không tìm thấy user để cập nhật
            if (user == null)
                return NotFound(new { message = "Không tìm thấy người dùng để cập nhật" });

            return Ok(new UserProfileResponse
            {
                UserId = user.Id,
                Name = user.Name,
                AvatarUrl = user.AvatarUrl,
                CreatedAt = user.CreatedAt
            });
        }
    }
}