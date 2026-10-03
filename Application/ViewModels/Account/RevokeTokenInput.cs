
using Application.Common.Validation;

namespace Application.ViewModels.Account
{
    public class RevokeTokenInput : Input<RevokeTokenInput>
    {
        public string Token { get; set; }
    }
}
