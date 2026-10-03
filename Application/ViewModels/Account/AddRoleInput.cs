using Application.Common.Validation;
using System.ComponentModel.DataAnnotations;

namespace Application.ViewModels.Account
{
    public class RoleInput: Input<RoleInput>
    {
        [Required]
        public string Email { get; set; }
        
        [Required]
        public string Role { get; set; }
    }
}
