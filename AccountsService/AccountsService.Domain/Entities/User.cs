using AccountsService.Domain.Entities.Base;
using System.Diagnostics.CodeAnalysis;

namespace AccountsService.Domain.Entities
{
    public enum UserRole
    {
        Admin = 0,
        Manager = 10,
        Worker = 20
    }

    public class User : BaseEntity
    {
        public string FirstName { get; private set; }
        public string LastName { get; private set; }

        public string Email { get; private set; }
        public string PhoneNumber { get; private set; }

        public UserRole Role { get; private set; }

        public IReadOnlyList<Guid> WarehouseIds { get; private set; }

        public string PasswordHash { get; private set; }

        private User() { }

        [SetsRequiredMembers]
        public User(string firstName, string lastName, string email, string phoneNumber, UserRole role, string passwordHash)
        {
            Id = Guid.NewGuid();

            FirstName = firstName;
            LastName = lastName;

            Email = email;
            PhoneNumber = phoneNumber;

            Role = role;

            WarehouseIds = [];

            PasswordHash = passwordHash;
        }

        public void SetRole(UserRole newRole)
        {
            Role = newRole;
        }

        public void AssignWarehouse(Guid warehouseId)
        {
            if (!WarehouseIds.Contains(warehouseId))
            {
                WarehouseIds = [.. WarehouseIds, warehouseId];
            }
        }

        public void RemoveWarehouse(Guid warehouseId)
        {
            WarehouseIds = [.. WarehouseIds.Where(id => id != warehouseId)];
        }

        public void ChangePassword(string newPasswordHash)
        {
            PasswordHash = newPasswordHash;
        }

        public void ChangeEmail(string newEmail)
        {
            Email = newEmail;
        }

        public void ChangePhoneNumber(string newPhoneNumber)
        {
            PhoneNumber = newPhoneNumber;
        }
    }
}