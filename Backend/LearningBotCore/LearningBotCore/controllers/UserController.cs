using LearningBotCore.exceptions;
using LearningBotCore.model;
using LearningBotCore.service.interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LearningBotCore.controllers
{
    [ApiController]
    [Route("api/[controller]")] // Базовый маршрут для контроллера: /api/user
    public class UserController(IUserManager userManager) : ControllerBase
    {
        // POST: api/user/add
        [HttpPost("add")]
        public IActionResult AddUser([FromBody] User user)
        {
            try
            {
                userManager.AddUser(user);
                return Ok(); // Возвращаем 200 OK
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message); // Возвращаем 500
            }
        }

        // GET: api/user/get?userId=123
        [HttpGet("get")]
        public IActionResult GetUser([FromQuery] long userId)
        {
            try
            {
                var user = userManager.GetUser(userId);
                return Ok(user); // Возвращаем объект пользователя с кодом 200 OK
            }
            catch (UserNotFoundException ex)
            {
                return NotFound(ex.Message); // Возвращаем 404 Not Found с сообщением об ошибке
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message); // Возвращаем 500
            }
        }
    }
}