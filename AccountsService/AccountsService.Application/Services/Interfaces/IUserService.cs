using AccountsService.Application.Services.DTOs;

namespace AccountsService.Application.Services.Interfaces
{
    public interface IUserService
    {
        Task<UserServiceDTOs.RegisterResponseDTO> RegisterAsync(UserServiceDTOs.RegisterRequestDTO request, CancellationToken cancellationToken);
        Task<UserServiceDTOs.DeleteAccountResponseDTO> DeleteAccountAsync(UserServiceDTOs.DeleteAccountRequestDTO request, CancellationToken cancellationToken);
        Task<UserServiceDTOs.ChangeRoleResponseDTO> ChangeRoleAsync(UserServiceDTOs.ChangeRoleRequestDTO request, CancellationToken cancellationToken);
        Task<UserServiceDTOs.AssignWarehouseResponseDTO> AssignWarehouseAsync(UserServiceDTOs.AssignWarehouseRequestDTO request, CancellationToken cancellationToken);
        Task<UserServiceDTOs.RemoveWarehouseResponseDTO> RemoveWarehouseAsync(UserServiceDTOs.RemoveWarehouseRequestDTO request, CancellationToken cancellationToken);
        Task<UserServiceDTOs.ChangePasswordResponseDTO> ChangePasswordAsync(UserServiceDTOs.ChangePasswordRequestDTO request, CancellationToken cancellationToken);
        Task<UserServiceDTOs.ChangeEmailResponseDTO> ChangeEmailAsync(UserServiceDTOs.ChangeEmailRequestDTO request, CancellationToken cancellationToken);
        Task<UserServiceDTOs.ChangePhoneNumberResponseDTO> ChangePhoneNumberAsync(UserServiceDTOs.ChangePhoneNumberRequestDTO request, CancellationToken cancellationToken);
    }
}