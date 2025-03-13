using Backend.Exceptions;
using Backend.Models;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // Базовый маршрут для контроллера: /api/user
    public class UserController : ControllerBase
    {
        private readonly IUserManager _userManager;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserManager userManager, ILogger<UserController> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Проверяет существование пользователя
        /// </summary>
        /// <param name="userId">ID пользователя</param>
        /// <returns>true если пользователь существует, иначе false</returns>
        [HttpGet("exists")]
        public async Task<ActionResult<bool>> UserExists([FromQuery] long userId)
        {
            try
            {
                var exists = await _userManager.UserExistsAsync(userId);
                return Ok(exists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking user existence for userId: {UserId}", userId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Добавляет нового пользователя
        /// </summary>
        /// <param name="userId">ID пользователя</param>
        /// <returns>Созданный пользователь</returns>
        [HttpPost("add")]
        public async Task<ActionResult<User>> AddUser([FromBody] long userId)
        {
            try
            {
                var exists = await _userManager.UserExistsAsync(userId);
                if (exists)
                {
                    return Conflict("User already exists");
                }

                var user = await _userManager.CreateUserAsync(userId);
                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user for userId: {UserId}", userId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Получает информацию о пользователе
        /// </summary>
        /// <param name="userId">ID пользователя</param>
        /// <returns>Информация о пользователе</returns>
        [HttpGet("{userId}")]
        public async Task<ActionResult<User>> GetUser(long userId)
        {
            try
            {
                var user = await _userManager.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return NotFound($"User with ID {userId} not found");
                }
                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user for userId: {UserId}", userId);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}