using AccountsService.Application.Common;
using AccountsService.Application.Repositories.Interfaces;
using AccountsService.Application.Services.DTOs;
using AccountsService.Application.Services.Interfaces;
using AccountsService.Domain.Entities;
using Shared.Kernel.Database;
using Shared.Kernel.Exceptions;
using Shared.Kernel.Mapping;
using Shared.Kernel.Requests;
using Shared.Kernel.Services;
using static AccountsService.Domain.Exceptions.GeneralExceptions;

namespace AccountsService.Application.Services
{
    public class UserScopedService : BaseService, IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasherService _passwordHasherService;
        private readonly IUserAccessChangeNotifier _userAccessChangeNotifier;

        public UserScopedService(IUnitOfWork unitOfWork, IRequestContext requestContext, IMapperWrapper mapper,
            IUserRepository userRepository, IPasswordHasherService passwordHasherService,
            IUserAccessChangeNotifier userAccessChangeNotifier)
            : base(unitOfWork, requestContext, mapper)
        {
            _userRepository = userRepository;
            _passwordHasherService = passwordHasherService;
            _userAccessChangeNotifier = userAccessChangeNotifier;
        }

        public async Task<UserServiceDTOs.RegisterResponseDTO> RegisterAsync(
            UserServiceDTOs.RegisterRequestDTO request, CancellationToken cancellationToken)
        {
            RequestContext.EnsureMinimumRole(UserRole.Manager);

            if (RequestContext.GetUserRole() == UserRole.Manager)
            {
                if (request.Role != UserRole.Worker)
                {
                    throw new InsufficientPermissionsException();
                }

                foreach (var warehouseId in request.WarehouseIds ?? [])
                {
                    RequestContext.EnsureWarehouseAccess(warehouseId, UserRole.Admin);
                }
            }

            if (!string.IsNullOrEmpty(request.Email) && await _userRepository.ExistsByEmailAsync(request.Email, cancellationToken))
            {
                throw new EmailAlreadyTakenException(request.Email);
            }

            if (!string.IsNullOrEmpty(request.PhoneNumber) && await _userRepository.ExistsByPhoneNumberAsync(request.PhoneNumber, cancellationToken))
            {
                throw new PhoneNumberAlreadyTakenException(request.PhoneNumber);
            }

            var passwordHash = _passwordHasherService.Hash(request.Password);
            var user = new User(request.FirstName, request.LastName, request.Email, request.PhoneNumber, request.Role, passwordHash);

            foreach (var warehouseId in request.WarehouseIds ?? [])
            {
                user.AssignWarehouse(warehouseId);
            }

            _userRepository.Add(user);
            await UnitOfWork.SaveChangesAsync(cancellationToken);

            return new UserServiceDTOs.RegisterResponseDTO(user.Id);
        }

        public async Task<UserServiceDTOs.DeleteAccountResponseDTO> DeleteAccountAsync(
            UserServiceDTOs.DeleteAccountRequestDTO request, CancellationToken cancellationToken)
        {
            var isSelf = RequestContext.UserId == request.TargetUserId;

            if (!isSelf)
            {
                RequestContext.EnsureMinimumRole(UserRole.Manager);
            }

            if (!isSelf && RequestContext.GetUserRole() != UserRole.Admin)
            {
                var targetUser = await _userRepository.GetByIdAsync(request.TargetUserId, cancellationToken)
                    ?? throw new NotFoundException(nameof(User), request.TargetUserId);

                EnsureManagerCanManageWorker(targetUser);
            }

            if (!await _userRepository.RemoveByIdAsync(request.TargetUserId, cancellationToken))
            {
                throw new NotFoundException(nameof(User), request.TargetUserId);
            }

            await UnitOfWork.SaveChangesAsync(cancellationToken);

            await _userAccessChangeNotifier.NotifyAccessRevokedAsync(request.TargetUserId, cancellationToken);

            return new UserServiceDTOs.DeleteAccountResponseDTO();
        }

        public async Task<UserServiceDTOs.ChangeRoleResponseDTO> ChangeRoleAsync(
            UserServiceDTOs.ChangeRoleRequestDTO request, CancellationToken cancellationToken)
        {
            RequestContext.EnsureMinimumRole(UserRole.Admin);

            var user = await _userRepository.GetByIdAsync(request.TargetUserId, cancellationToken)
                ?? throw new NotFoundException(nameof(User), request.TargetUserId);

            user.SetRole(request.NewRole);
            await UnitOfWork.SaveChangesAsync(cancellationToken);

            return new UserServiceDTOs.ChangeRoleResponseDTO();
        }

        public async Task<UserServiceDTOs.AssignWarehouseResponseDTO> AssignWarehouseAsync(
            UserServiceDTOs.AssignWarehouseRequestDTO request, CancellationToken cancellationToken)
        {
            RequestContext.EnsureMinimumRole(UserRole.Manager);

            var user = await _userRepository.GetByIdAsync(request.TargetUserId, cancellationToken)
                ?? throw new NotFoundException(nameof(User), request.TargetUserId);

            if (RequestContext.GetUserRole() == UserRole.Manager)
            {
                if (user.Role != UserRole.Worker)
                {
                    throw new InsufficientPermissionsException();
                }

                RequestContext.EnsureWarehouseAccess(request.WarehouseId, UserRole.Admin);
            }

            user.AssignWarehouse(request.WarehouseId);
            await UnitOfWork.SaveChangesAsync(cancellationToken);

            await _userAccessChangeNotifier.NotifyWarehouseChangedAsync(user.Id, request.WarehouseId, added: true, cancellationToken);

            return new UserServiceDTOs.AssignWarehouseResponseDTO();
        }

        public async Task<UserServiceDTOs.RemoveWarehouseResponseDTO> RemoveWarehouseAsync(
            UserServiceDTOs.RemoveWarehouseRequestDTO request, CancellationToken cancellationToken)
        {
            RequestContext.EnsureMinimumRole(UserRole.Manager);

            var user = await _userRepository.GetByIdAsync(request.TargetUserId, cancellationToken)
                ?? throw new NotFoundException(nameof(User), request.TargetUserId);

            if (RequestContext.GetUserRole() == UserRole.Manager)
            {
                if (user.Role != UserRole.Worker)
                {
                    throw new InsufficientPermissionsException();
                }

                RequestContext.EnsureWarehouseAccess(request.WarehouseId, UserRole.Admin);
            }

            user.RemoveWarehouse(request.WarehouseId);
            await UnitOfWork.SaveChangesAsync(cancellationToken);

            await _userAccessChangeNotifier.NotifyWarehouseChangedAsync(user.Id, request.WarehouseId, added: false, cancellationToken);

            return new UserServiceDTOs.RemoveWarehouseResponseDTO();
        }

        // A Manager can only manage Workers, and only if they share at least one warehouse.
        private void EnsureManagerCanManageWorker(User targetUser)
        {
            if (RequestContext.GetUserRole() != UserRole.Manager)
            {
                throw new InsufficientPermissionsException();
            }

            var sharesWarehouse = targetUser.WarehouseIds.Intersect(RequestContext.WarehouseIds).Any();

            if (targetUser.Role != UserRole.Worker || !sharesWarehouse)
            {
                throw new InsufficientPermissionsException();
            }
        }
    }
}