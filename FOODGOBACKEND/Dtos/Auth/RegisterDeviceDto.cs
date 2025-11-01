using System.ComponentModel.DataAnnotations;

namespace FOODGOBACKEND.Dtos.Auth
{
    public class RegisterDeviceDto
    {
        [Required(ErrorMessage = "Device token is required.")]
        [StringLength(500, ErrorMessage = "Device token exceeds maximum length.")]
        public string DeviceToken { get; set; } = null!;

        [Required(ErrorMessage = "Device type is required.")]
        [StringLength(20)]
        [RegularExpression("^(Android|iOS|Web)$", ErrorMessage = "Device type must be Android, iOS, or Web.")]
        public string DeviceType { get; set; } = null!;

        [StringLength(100)]
        public string? DeviceModel { get; set; }

        [StringLength(50)]
        public string? AppVersion { get; set; }
    }
}