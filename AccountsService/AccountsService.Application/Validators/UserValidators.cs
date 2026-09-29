using AccountsService.Application.Options;
using AccountsService.Application.Services.DTOs;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace AccountsService.Application.Validators
{
    public class RegisterRequestDTOValidator : AbstractValidator<UserServiceDTOs.RegisterRequestDTO>
    {
        public RegisterRequestDTOValidator(IOptions<UserValidationSettings> settings)
        {
            var settingsValue = settings.Value;

            RuleFor(x => x.FirstName).NotEmpty();
            RuleFor(x => x.LastName).NotEmpty();
            RuleFor(x => x.PhoneNumber).NotEmpty().When(x => string.IsNullOrEmpty(x.Email));
            RuleFor(x => x.PhoneNumber).Matches(settingsValue.PhoneNumberPattern).When(x => !string.IsNullOrEmpty(x.PhoneNumber));
            RuleFor(x => x.Email).NotEmpty().EmailAddress().When(x => string.IsNullOrEmpty(x.PhoneNumber));
            RuleFor(x => x.Password).NotEmpty().MinimumLength(settingsValue.MinPasswordLength);
            RuleFor(x => x.Role).IsInEnum();
        }
    }

    public class DeleteAccountRequestDTOValidator : AbstractValidator<UserServiceDTOs.DeleteAccountRequestDTO>
    {
        public DeleteAccountRequestDTOValidator()
        {
            RuleFor(x => x.TargetUserId).NotEmpty();
        }
    }

    public class ChangeRoleRequestDTOValidator : AbstractValidator<UserServiceDTOs.ChangeRoleRequestDTO>
    {
        public ChangeRoleRequestDTOValidator()
        {
            RuleFor(x => x.TargetUserId).NotEmpty();
            RuleFor(x => x.NewRole).IsInEnum();
        }
    }

    public class AssignWarehouseRequestDTOValidator : AbstractValidator<UserServiceDTOs.AssignWarehouseRequestDTO>
    {
        public AssignWarehouseRequestDTOValidator()
        {
            RuleFor(x => x.TargetUserId).NotEmpty();
            RuleFor(x => x.WarehouseId).NotEmpty();
        }
    }

    public class RemoveWarehouseRequestDTOValidator : AbstractValidator<UserServiceDTOs.RemoveWarehouseRequestDTO>
    {
        public RemoveWarehouseRequestDTOValidator()
        {
            RuleFor(x => x.TargetUserId).NotEmpty();
            RuleFor(x => x.WarehouseId).NotEmpty();
        }
    }

    public class ChangePasswordRequestDTOValidator : AbstractValidator<UserServiceDTOs.ChangePasswordRequestDTO>
    {
        public ChangePasswordRequestDTOValidator(IOptions<UserValidationSettings> settings)
        {
            RuleFor(x => x.CurrentPassword).NotEmpty();
            RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(settings.Value.MinPasswordLength);
            RuleFor(x => x.NewPassword).NotEqual(x => x.CurrentPassword)
                .WithMessage("New password must be different from the current password.");
        }
    }

    public class ChangeEmailRequestDTOValidator : AbstractValidator<UserServiceDTOs.ChangeEmailRequestDTO>
    {
        public ChangeEmailRequestDTOValidator()
        {
            RuleFor(x => x.NewEmail).NotEmpty().EmailAddress();
        }
    }

    public class ChangePhoneNumberRequestDTOValidator : AbstractValidator<UserServiceDTOs.ChangePhoneNumberRequestDTO>
    {
        public ChangePhoneNumberRequestDTOValidator(IOptions<UserValidationSettings> settings)
        {
            RuleFor(x => x.NewPhoneNumber).NotEmpty().Matches(settings.Value.PhoneNumberPattern);
        }
    }
}