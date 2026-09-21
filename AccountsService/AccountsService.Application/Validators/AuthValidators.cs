using AccountsService.Application.Services.DTOs;
using FluentValidation;

namespace AccountsService.Application.Validators
{
    public static class AuthValidators
    {
        public class LoginRequestDTOValidator : AbstractValidator<AuthServiceDTOs.LoginRequestDTO>
        {
            public LoginRequestDTOValidator()
            {
                RuleFor(x => x.PhoneNumber).NotEmpty().When(x => string.IsNullOrEmpty(x.Email));
                RuleFor(x => x.Email).NotEmpty().EmailAddress().When(x => string.IsNullOrEmpty(x.PhoneNumber));
                RuleFor(x => x.Password).NotEmpty();
            }
        }

        public class RefreshAccessTokenRequestDTOValidator : AbstractValidator<AuthServiceDTOs.RefreshAccessTokenRequestDTO>
        {
            public RefreshAccessTokenRequestDTOValidator()
            {
                RuleFor(x => x.RefreshToken).NotNull().SetValidator(new RefreshTokenDTOValidator());
            }
        }

        public class LogoutRequestDTOValidator : AbstractValidator<AuthServiceDTOs.LogoutRequestDTO>
        {
            public LogoutRequestDTOValidator()
            {
                RuleFor(x => x.RefreshTokenId).NotEmpty();
            }
        }

        public class RefreshTokenDTOValidator : AbstractValidator<AuthServiceDTOs.RefreshTokenDTO>
        {
            public RefreshTokenDTOValidator()
            {
                RuleFor(x => x.Id).NotEmpty();
                RuleFor(x => x.Token).NotEmpty();
            }
        }
    }
}