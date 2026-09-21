using AccountsService.Application.Services.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace AccountsService.Infrastructure.Services
{
    public class PasswordHasherSingletonService : IPasswordHasherService
    {
        private readonly PasswordHasher<object> _hasher;

        public PasswordHasherSingletonService()
        {
            _hasher = new();
        }

        public string Hash(string password)
        {
            return _hasher.HashPassword(null!, password);
        }

        public bool Verify(string hashedPassword, string providedPassword)
        {
            var verificationResult = _hasher.VerifyHashedPassword(null!, hashedPassword, providedPassword);

            return verificationResult != PasswordVerificationResult.Failed;
        }
    }
}