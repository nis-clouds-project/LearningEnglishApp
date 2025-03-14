using Backend.Models;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserManager _userManager;

        public UserController(IUserManager userManager)
        {
            _userManager = userManager;
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
                return StatusCode(500, "Internal server error");
            }
        }
    }
}