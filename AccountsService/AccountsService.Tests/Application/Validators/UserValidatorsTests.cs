using AccountsService.Application.Options;
using AccountsService.Application.Services.DTOs;
using AccountsService.Application.Validators;
using FluentValidation.TestHelper;
using Xunit;

namespace AccountsService.Tests.Application.Validators
{
    public class UserValidatorsTests
    {
        private static readonly Microsoft.Extensions.Options.IOptions<UserValidationSettings> DefaultSettings =
            Microsoft.Extensions.Options.Options.Create(new UserValidationSettings());

        private readonly ChangePasswordRequestDTOValidator _changePasswordValidator = new(DefaultSettings);
        private readonly ChangeEmailRequestDTOValidator _changeEmailValidator = new();
        private readonly ChangePhoneNumberRequestDTOValidator _changePhoneNumberValidator = new(DefaultSettings);

        [Fact]
        public void ChangePassword_EmptyCurrentPassword_HasValidationError()
        {
            var result = _changePasswordValidator.TestValidate(
                new UserServiceDTOs.ChangePasswordRequestDTO("", "NewPassword123!"));

            result.ShouldHaveValidationErrorFor(x => x.CurrentPassword);
        }

        [Fact]
        public void ChangePassword_NewPasswordTooShort_HasValidationError()
        {
            var result = _changePasswordValidator.TestValidate(
                new UserServiceDTOs.ChangePasswordRequestDTO("CurrentPassword123!", "short"));

            result.ShouldHaveValidationErrorFor(x => x.NewPassword);
        }

        [Fact]
        public void ChangePassword_NewPasswordSameAsCurrent_HasValidationError()
        {
            var result = _changePasswordValidator.TestValidate(
                new UserServiceDTOs.ChangePasswordRequestDTO("SamePassword123!", "SamePassword123!"));

            result.ShouldHaveValidationErrorFor(x => x.NewPassword);
        }

        [Fact]
        public void ChangePassword_Valid_HasNoValidationErrors()
        {
            var result = _changePasswordValidator.TestValidate(
                new UserServiceDTOs.ChangePasswordRequestDTO("CurrentPassword123!", "NewPassword456!"));

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData("")]
        [InlineData("not-an-email")]
        public void ChangeEmail_InvalidEmail_HasValidationError(string email)
        {
            var result = _changeEmailValidator.TestValidate(new UserServiceDTOs.ChangeEmailRequestDTO(email));

            result.ShouldHaveValidationErrorFor(x => x.NewEmail);
        }

        [Fact]
        public void ChangeEmail_Valid_HasNoValidationErrors()
        {
            var result = _changeEmailValidator.TestValidate(new UserServiceDTOs.ChangeEmailRequestDTO("new@ims.local"));

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData("")]
        [InlineData("not-a-phone-number")]
        [InlineData("0123456789")]
        public void ChangePhoneNumber_InvalidPhoneNumber_HasValidationError(string phoneNumber)
        {
            var result = _changePhoneNumberValidator.TestValidate(new UserServiceDTOs.ChangePhoneNumberRequestDTO(phoneNumber));

            result.ShouldHaveValidationErrorFor(x => x.NewPhoneNumber);
        }

        [Fact]
        public void ChangePhoneNumber_Valid_HasNoValidationErrors()
        {
            var result = _changePhoneNumberValidator.TestValidate(new UserServiceDTOs.ChangePhoneNumberRequestDTO("+380501234567"));

            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}