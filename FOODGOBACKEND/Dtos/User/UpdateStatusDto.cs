using System.ComponentModel.DataAnnotations;

namespace FOODGOBACKEND.Dtos.User
{
    public class UpdateStatusDto
    {
        /// <summary>
        /// The new status for the user account. 
        /// Set to 'true' to activate/unlock or 'false' to deactivate/lock.
        /// </summary>
        [Required(ErrorMessage = "The active status is required.")]
        public bool IsActive { get; set; }
    }
}