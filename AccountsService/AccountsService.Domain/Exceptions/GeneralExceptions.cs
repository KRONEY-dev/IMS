namespace AccountsService.Domain.Exceptions
{
    public static class GeneralExceptions
    {
        public abstract class GeneralException : Exception
        {
            protected GeneralException(string message) : base(message) { }
        }

        public class EmailAlreadyTakenException(string email)
            : GeneralException($"Email '{email}' is already taken.")
        {
            public string Email { get; } = email;
        }

        public class InvalidCredentialsException() : GeneralException("Invalid email or password.");

        public class RefreshTokenExpiredException(Guid tokenId)
            : GeneralException($"Refresh token '{tokenId}' has expired.")
        {
            public Guid TokenId { get; } = tokenId;
        }

        public class RefreshTokenAlreadyRevokedException(Guid tokenId)
            : GeneralException($"Refresh token '{tokenId}' has already expired.")
        {
            public Guid TokenId { get; } = tokenId;
        }

        public class InvalidRefreshTokenException()
            : GeneralException("Refresh token is invalid.");

        public class RefreshTokenReuseDetectedException(Guid sessionId)
            : GeneralException($"Refresh token reuse detected for session '{sessionId}'. Session revoked.")
        {
            public Guid SessionId { get; } = sessionId;
        }

        public class PhoneNumberAlreadyTakenException(string phoneNumber)
            : GeneralException($"Phone number '{phoneNumber}' is already taken.")
        {
            public string PhoneNumber { get; } = phoneNumber;
        }
    }
}