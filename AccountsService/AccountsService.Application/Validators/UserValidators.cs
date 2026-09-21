using AccountsService.Application.Services.DTOs;
using FluentValidation;

namespace AccountsService.Application.Validators
{
    public class RegisterRequestDTOValidator : AbstractValidator<UserServiceDTOs.RegisterRequestDTO>
    {
        public RegisterRequestDTOValidator()
        {
            RuleFor(x => x.FirstName).NotEmpty();
            RuleFor(x => x.LastName).NotEmpty();
            RuleFor(x => x.PhoneNumber).NotEmpty().When(x => string.IsNullOrEmpty(x.Email));
            RuleFor(x => x.Email).NotEmpty().EmailAddress().When(x => string.IsNullOrEmpty(x.PhoneNumber));
            RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
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
}