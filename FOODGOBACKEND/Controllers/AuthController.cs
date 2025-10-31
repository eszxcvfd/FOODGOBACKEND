using FOODGOBACKEND.Dtos.Auth;
using FOODGOBACKEND.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Org.BouncyCastle.Crypto.Generators;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt;

namespace FOODGOBACKEND.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly FoodGoContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(FoodGoContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserDto registerUserDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check if phone number already exists
            if (await _context.Users.AnyAsync(u => u.PhoneNumber == registerUserDto.PhoneNumber))
            {
                return BadRequest("Phone number is already registered.");
            }

            // Hash the password
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(registerUserDto.Password);

            var user = new User
            {
                PhoneNumber = registerUserDto.PhoneNumber,
                PasswordHash = passwordHash,
                UserType = registerUserDto.UserType,
                IsActive = true, // Default to active
                CreatedAt = DateTime.UtcNow
            };

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync(); // Save to get the UserId

                // Create role-specific entity
                switch (user.UserType.ToLower())
                {
                    case "customer":
                        var customer = new Customer
                        {
                            CustomerId = user.UserId,
                            FullName = registerUserDto.FullName,
                            Email = registerUserDto.Email
                        };
                        await _context.Customers.AddAsync(customer);
                        break;
                    case "shipper":
                        var shipper = new Shipper
                        {
                            ShipperId = user.UserId,
                            FullName = registerUserDto.FullName,
                            IsAvailable = true // Default to available
                        };
                        await _context.Shippers.AddAsync(shipper);
                        break;
                    case "restaurant":
                        var restaurant = new Restaurant
                        {
                            OwnerId = user.UserId,
                            RestaurantName = registerUserDto.FullName, // Assuming FullName is used as RestaurantName for registration
                            PhoneNumber = user.PhoneNumber,
                            IsActive = true // Default to active
                        };
                        await _context.Restaurants.AddAsync(restaurant);
                        break;
                    default:
                        return BadRequest("Invalid user type specified.");
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { Message = "User registered successfully." });
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                // Log the exception
                return StatusCode(500, "An error occurred during registration.");
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == loginDto.PhoneNumber);

            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            {
                return Unauthorized("Invalid phone number or password.");
            }

            if (!user.IsActive)
            {
                return Unauthorized("This account has been deactivated.");
            }

            var token = GenerateJwtToken(user);

            return Ok(new { Token = token });
        }

        private string GenerateJwtToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Role, user.UserType),
                new Claim("phoneNumber", user.PhoneNumber)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddDays(7); // Token expires in 7 days

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}