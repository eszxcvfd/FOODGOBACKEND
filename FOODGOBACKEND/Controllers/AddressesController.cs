using FOODGOBACKEND.Dtos.Address;
using FOODGOBACKEND.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FOODGOBACKEND.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "CUSTOMER")]
    public class AddressesController : ControllerBase
    {
        private readonly FoodGoContext _context;

        public AddressesController(FoodGoContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets all addresses for the authenticated customer.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AddressDto>>> GetMyAddresses()
        {
            // Get customer ID from JWT token
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var customerId))
            {
                return Unauthorized("Invalid user token.");
            }

            var addresses = await _context.Addresses
                .Where(a => a.CustomerId == customerId)
                .OrderByDescending(a => a.IsDefault)
                .ThenByDescending(a => a.AddressId)
                .Select(a => new AddressDto
                {
                    AddressId = a.AddressId,
                    CustomerId = a.CustomerId,
                    Street = a.Street,
                    Ward = a.Ward,
                    District = a.District,
                    City = a.City,
                    FullAddress = a.FullAddress,
                    IsDefault = a.IsDefault
                })
                .ToListAsync();

            return Ok(addresses);
        }

        /// <summary>
        /// Creates a new address for the authenticated customer.
        /// </summary>
        /// <param name="dto">Address creation data</param>
        [HttpPost]
        public async Task<ActionResult<AddressDto>> CreateAddress([FromBody] CreateAddressDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Get customer ID from JWT token
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var customerId))
            {
                return Unauthorized("Invalid user token.");
            }

            // Verify customer exists
            var customerExists = await _context.Customers.AnyAsync(c => c.CustomerId == customerId);
            if (!customerExists)
            {
                return NotFound("Customer not found.");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // If this is being set as default, unset other default addresses
                if (dto.IsDefault)
                {
                    var existingDefaultAddresses = await _context.Addresses
                        .Where(a => a.CustomerId == customerId && a.IsDefault)
                        .ToListAsync();

                    foreach (var addr in existingDefaultAddresses)
                    {
                        addr.IsDefault = false;
                    }
                }

                // Create new address
                var address = new Address
                {
                    CustomerId = customerId,
                    Street = dto.Street,
                    Ward = dto.Ward,
                    District = dto.District,
                    City = dto.City,
                    FullAddress = dto.FullAddress,
                    IsDefault = dto.IsDefault
                };

                _context.Addresses.Add(address);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var addressDto = new AddressDto
                {
                    AddressId = address.AddressId,
                    CustomerId = address.CustomerId,
                    Street = address.Street,
                    Ward = address.Ward,
                    District = address.District,
                    City = address.City,
                    FullAddress = address.FullAddress,
                    IsDefault = address.IsDefault
                };

                return CreatedAtAction(nameof(GetMyAddresses), new { id = address.AddressId }, addressDto);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Address creation error: {ex.Message}");
                return StatusCode(500, $"An error occurred while creating the address: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates an existing address for the authenticated customer.
        /// </summary>
        /// <param name="id">Address ID</param>
        /// <param name="dto">Address update data</param>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAddress(int id, [FromBody] UpdateAddressDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Get customer ID from JWT token
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var customerId))
            {
                return Unauthorized("Invalid user token.");
            }

            // Find address and verify ownership
            var address = await _context.Addresses
                .FirstOrDefaultAsync(a => a.AddressId == id && a.CustomerId == customerId);

            if (address == null)
            {
                return NotFound("Address not found or you do not have permission to update it.");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Update only provided fields (partial update)
                if (dto.Street != null)
                {
                    address.Street = dto.Street;
                }

                if (dto.Ward != null)
                {
                    address.Ward = dto.Ward;
                }

                if (dto.District != null)
                {
                    address.District = dto.District;
                }

                if (dto.City != null)
                {
                    address.City = dto.City;
                }

                if (dto.FullAddress != null)
                {
                    address.FullAddress = dto.FullAddress;
                }

                // Handle IsDefault flag
                if (dto.IsDefault.HasValue)
                {
                    if (dto.IsDefault.Value && !address.IsDefault)
                    {
                        // Unset other default addresses
                        var existingDefaultAddresses = await _context.Addresses
                            .Where(a => a.CustomerId == customerId && a.IsDefault && a.AddressId != id)
                            .ToListAsync();

                        foreach (var addr in existingDefaultAddresses)
                        {
                            addr.IsDefault = false;
                        }
                    }

                    address.IsDefault = dto.IsDefault.Value;
                }

                _context.Addresses.Update(address);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Address update error: {ex.Message}");
                return StatusCode(500, $"An error occurred while updating the address: {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes an address for the authenticated customer.
        /// </summary>
        /// <param name="id">Address ID</param>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAddress(int id)
        {
            // Get customer ID from JWT token
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var customerId))
            {
                return Unauthorized("Invalid user token.");
            }

            // Find address and verify ownership
            var address = await _context.Addresses
                .FirstOrDefaultAsync(a => a.AddressId == id && a.CustomerId == customerId);

            if (address == null)
            {
                return NotFound("Address not found or you do not have permission to delete it.");
            }

            try
            {
                _context.Addresses.Remove(address);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Address deletion error: {ex.Message}");
                return StatusCode(500, $"An error occurred while deleting the address: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets a specific address as the default for the authenticated customer.
        /// </summary>
        /// <param name="id">Address ID</param>
        [HttpPatch("{id}/set-default")]
        public async Task<IActionResult> SetDefaultAddress(int id)
        {
            // Get customer ID from JWT token
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var customerId))
            {
                return Unauthorized("Invalid user token.");
            }

            // Find address and verify ownership
            var address = await _context.Addresses
                .FirstOrDefaultAsync(a => a.AddressId == id && a.CustomerId == customerId);

            if (address == null)
            {
                return NotFound("Address not found or you do not have permission to modify it.");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Unset all other default addresses for this customer
                var existingDefaultAddresses = await _context.Addresses
                    .Where(a => a.CustomerId == customerId && a.IsDefault)
                    .ToListAsync();

                foreach (var addr in existingDefaultAddresses)
                {
                    addr.IsDefault = false;
                }

                // Set this address as default
                address.IsDefault = true;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Set default address error: {ex.Message}");
                return StatusCode(500, $"An error occurred while setting the default address: {ex.Message}");
            }
        }
    }
}