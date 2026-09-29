namespace AccountsService.Application.Options
{
    public class UserValidationSettings
    {
        public int MinPasswordLength { get; init; } = 8;
        public string PhoneNumberPattern { get; init; } = @"^\+?[1-9]\d{7,14}$";
    }
}