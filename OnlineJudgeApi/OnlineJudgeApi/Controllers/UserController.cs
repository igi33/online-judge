using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using System.IdentityModel.Tokens.Jwt;
using OnlineJudgeApi.Helpers;
using Microsoft.Extensions.Options;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using OnlineJudgeApi.Services;
using OnlineJudgeApi.Dtos;
using OnlineJudgeApi.Entities;

namespace OnlineJudgeApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private IUserService userService;
        private IMapper mapper;
        private readonly AppSettings appSettings;

        public UserController(
            IUserService userService,
            IMapper mapper,
            IOptions<AppSettings> appSettings)
        {
            this.userService = userService;
            this.mapper = mapper;
            this.appSettings = appSettings.Value;
        }

        [AllowAnonymous]
        [HttpPost("authenticate")]
        public IActionResult Authenticate([FromBody]UserDto userDto)
        {
            var user = userService.Authenticate(userDto.Username, userDto.Password);

            if (user == null)
            {
                return BadRequest(new { message = "Username or password is incorrect" });
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(appSettings.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                    new Claim("username", user.Username),
                    new Claim("email", user.Email),
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            // return basic user info (without password) and token to store client side
            return Ok(new
            {
                user.Id,
                user.Username,
                Token = tokenString
            });
        }

        [AllowAnonymous]
        [HttpPost]
        public IActionResult PostUser([FromBody]UserDto userDto)
        {
            // map dto to entity
            var user = mapper.Map<User>(userDto);

            try
            {
                // save 
                User userEntity = userService.Create(user, userDto.Password);
                var dto = mapper.Map<UserDto>(userEntity);
                return CreatedAtAction("GetUser", new { id = userEntity.Id }, dto);
            }
            catch (AppException ex)
            {
                // return error message if there was an exception
                return BadRequest(new { message = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult GetUsers()
        {
            var users = userService.GetAll();
            var userDtos = mapper.Map<IList<UserDto>>(users);
            return Ok(userDtos);
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        public IActionResult GetUser(int id)
        {
            var user = userService.GetById(id);
            var userDto = mapper.Map<UserDto>(user);
            return Ok(userDto);
        }

        [HttpPut("{id}")]
        public IActionResult PutUser(int id, [FromBody]UserDto userDto)
        {
            if (id != userDto.Id)
            {
                return BadRequest();
            }

            // Map DTO to entity
            User user = mapper.Map<User>(userDto);

            // Fetch current user id
            int userId = int.Parse((User.Identity as ClaimsIdentity).FindFirst(ClaimTypes.Name).Value);

            // Return Unauthorized if current user doesn't match model user
            if (id != userId)
            {
                return Unauthorized();
            }

            try
            {
                // Save 
                userService.Update(user, userDto.Password);
                return NoContent();
            }
            catch (AppException ex)
            {
                // Return error message if there was an exception
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteUser(int id)
        {
            int userId = int.Parse((User.Identity as ClaimsIdentity).FindFirst(ClaimTypes.Name).Value);

            if (userId != id)
            {
                return Unauthorized();
            }
            userService.Delete(id);
            return NoContent();
        }
    }
}
