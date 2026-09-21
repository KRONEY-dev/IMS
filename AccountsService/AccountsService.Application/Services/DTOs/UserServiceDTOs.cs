using AccountsService.Domain.Entities;

namespace AccountsService.Application.Services.DTOs
{
    public static class UserServiceDTOs
    {
        public record RegisterRequestDTO(
            string FirstName,
            string LastName,
            string? Email,
            string? PhoneNumber,
            string Password,
            UserRole Role,
            IReadOnlyList<Guid>? WarehouseIds = null);

        public record RegisterResponseDTO(Guid UserId);

        public record DeleteAccountRequestDTO(Guid TargetUserId);
        public record DeleteAccountResponseDTO;

        public record ChangeRoleRequestDTO(Guid TargetUserId, UserRole NewRole);
        public record ChangeRoleResponseDTO;

        public record AssignWarehouseRequestDTO(Guid TargetUserId, Guid WarehouseId);
        public record AssignWarehouseResponseDTO;

        public record RemoveWarehouseRequestDTO(Guid TargetUserId, Guid WarehouseId);
        public record RemoveWarehouseResponseDTO;
    }
}