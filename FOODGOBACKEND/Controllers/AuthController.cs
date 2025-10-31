using FOODGOBACKEND.Dtos.Auth;
using FOODGOBACKEND.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

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

        // CUSTOMER REGISTRATION
        [HttpPost("register/customer")]
        public async Task<IActionResult> RegisterCustomer([FromBody] RegisterCustomerDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (await _context.Users.AnyAsync(u => u.PhoneNumber == dto.PhoneNumber))
            {
                return BadRequest("Phone number is already registered.");
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            var user = new User
            {
                PhoneNumber = dto.PhoneNumber,
                PasswordHash = passwordHash,
                UserType = "CUSTOMER",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync();

                var customer = new Customer
                {
                    CustomerId = user.UserId,
                    FullName = dto.FullName,
                    Email = dto.Email
                };
                await _context.Customers.AddAsync(customer);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { Message = "Customer registered successfully." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // Log the exception for debugging
                Console.WriteLine($"Registration error: {ex.Message}");
                return StatusCode(500, $"An error occurred during registration: {ex.Message}");
            }
        }

        // SHIPPER REGISTRATION
        [HttpPost("register/shipper")]
        public async Task<IActionResult> RegisterShipper([FromBody] RegisterShipperDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (await _context.Users.AnyAsync(u => u.PhoneNumber == dto.PhoneNumber))
            {
                return BadRequest("Phone number is already registered.");
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            var user = new User
            {
                PhoneNumber = dto.PhoneNumber,
                PasswordHash = passwordHash,
                UserType = "SHIPPER",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync();

                var shipper = new Shipper
                {
                    ShipperId = user.UserId,
                    FullName = dto.FullName,
                    LicensePlate = dto.LicensePlate,
                    IsAvailable = true
                };
                await _context.Shippers.AddAsync(shipper);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { Message = "Shipper registered successfully." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // Log the exception for debugging
                Console.WriteLine($"Registration error: {ex.Message}");
                return StatusCode(500, $"An error occurred during registration: {ex.Message}");
            }
        }

        // RESTAURANT REGISTRATION
        [HttpPost("register/restaurant")]
        public async Task<IActionResult> RegisterRestaurant([FromBody] RegisterRestaurantDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (await _context.Users.AnyAsync(u => u.PhoneNumber == dto.PhoneNumber))
            {
                return BadRequest("Phone number is already registered.");
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            var user = new User
            {
                PhoneNumber = dto.PhoneNumber,
                PasswordHash = passwordHash,
                UserType = "RESTAURANT",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync();

                var restaurant = new Restaurant
                {
                    OwnerId = user.UserId,
                    RestaurantName = dto.RestaurantName,
                    PhoneNumber = dto.PhoneNumber,
                    Address = dto.Address,
                    IsActive = true
                };
                await _context.Restaurants.AddAsync(restaurant);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { Message = "Restaurant registered successfully." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // Log the exception for debugging
                Console.WriteLine($"Registration error: {ex.Message}");
                return StatusCode(500, $"An error occurred during registration: {ex.Message}");
            }
        }

        // COMMON LOGIN FOR ALL USER TYPES
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

            return Ok(new { Token = token, UserType = user.UserType });
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