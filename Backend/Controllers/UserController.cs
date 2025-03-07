using Backend.Exceptions;
using Backend.Models;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // Базовый маршрут для контроллера: /api/user
    public class UserController : ControllerBase
    {
        private readonly IUserManager _userManager;

        public UserController(IUserManager userManager)
        {
            _userManager = userManager;
        }

        // POST: api/user/add
        [HttpPost("add")]
        public async Task<IActionResult> AddUserAsync([FromBody] long userId)
        {
            try
            {
                var user = new User(userId);
                var addedUser = await _userManager.AddUserAsync(user);
                return Ok(addedUser);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // GET: api/user/get?userId=123
        [HttpGet("get")]
        public async Task<IActionResult> GetUserAsync([FromQuery] long userId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(userId);
                return Ok(user);
            }
            catch (UserNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // GET: api/user/exists?userId=123
        [HttpGet("exists")]
        public async Task<IActionResult> UserExistsAsync([FromQuery] long userId)
        {
            try
            {
                var exists = await _userManager.IsUserExistsAsync(userId);
                return Ok(exists);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}