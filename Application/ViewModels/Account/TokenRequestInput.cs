using Application.Common.Validation;
using System.ComponentModel.DataAnnotations;

namespace Application.ViewModels.Account
{
    public class TokenRequestInput : Input<TokenRequestInput>
    {
        [Required]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }
    }
}
