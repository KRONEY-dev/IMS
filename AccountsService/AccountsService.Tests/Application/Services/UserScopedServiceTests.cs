using AccountsService.Application.Repositories.Interfaces;
using AccountsService.Application.Services;
using AccountsService.Application.Services.DTOs;
using AccountsService.Application.Services.Interfaces;
using AccountsService.Domain.Entities;
using Moq;
using Shared.Kernel.Database;
using Shared.Kernel.Exceptions;
using Shared.Kernel.Mapping;
using Shared.Kernel.Requests;
using Xunit;
using static AccountsService.Domain.Exceptions.GeneralExceptions;

namespace AccountsService.Tests.Application.Services
{
    public class UserScopedServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly Mock<IRequestContext> _requestContextMock = new();
        private readonly Mock<IMapperWrapper> _mapperMock = new();
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<IPasswordHasherService> _passwordHasherServiceMock = new();
        private readonly Mock<IUserAccessChangeNotifier> _userAccessChangeNotifierMock = new();

        private UserScopedService CreateSut()
        {
            return new UserScopedService(
                _unitOfWorkMock.Object, _requestContextMock.Object, _mapperMock.Object,
                _userRepositoryMock.Object, _passwordHasherServiceMock.Object,
                _userAccessChangeNotifierMock.Object);
        }

        private void SetActorRole(UserRole role, Guid? actorUserId = null, IReadOnlyList<Guid>? warehouseIds = null)
        {
            _requestContextMock.SetupGet(context => context.Role).Returns(role.ToString());
            _requestContextMock.SetupGet(context => context.UserId).Returns(actorUserId ?? Guid.NewGuid());
            _requestContextMock.SetupGet(context => context.WarehouseIds).Returns(warehouseIds ?? []);
        }

        [Fact]
        public async Task AssignWarehouseAsync_AdminAssignsWarehouse_NotifiesWarehouseChangedWithAdded()
        {
            SetActorRole(UserRole.Admin);

            var warehouseId = Guid.NewGuid();
            var user = new User("Test", "Worker", "worker@ims.local", null, UserRole.Worker, "password-hash");

            _userRepositoryMock
                .Setup(repo => repo.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var sut = CreateSut();

            await sut.AssignWarehouseAsync(new UserServiceDTOs.AssignWarehouseRequestDTO(user.Id, warehouseId), CancellationToken.None);

            Assert.Contains(warehouseId, user.WarehouseIds);
            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _userAccessChangeNotifierMock.Verify(
                notifier => notifier.NotifyWarehouseChangedAsync(user.Id, warehouseId, true, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task RemoveWarehouseAsync_AdminRemovesWarehouse_NotifiesWarehouseChangedWithoutAdded()
        {
            SetActorRole(UserRole.Admin);

            var warehouseId = Guid.NewGuid();
            var user = new User("Test", "Worker", "worker@ims.local", null, UserRole.Worker, "password-hash");
            user.AssignWarehouse(warehouseId);

            _userRepositoryMock
                .Setup(repo => repo.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var sut = CreateSut();

            await sut.RemoveWarehouseAsync(new UserServiceDTOs.RemoveWarehouseRequestDTO(user.Id, warehouseId), CancellationToken.None);

            Assert.DoesNotContain(warehouseId, user.WarehouseIds);
            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _userAccessChangeNotifierMock.Verify(
                notifier => notifier.NotifyWarehouseChangedAsync(user.Id, warehouseId, false, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task DeleteAccountAsync_SelfDelete_NotifiesAccessRevoked()
        {
            var selfUserId = Guid.NewGuid();
            SetActorRole(UserRole.Worker, actorUserId: selfUserId);

            _userRepositoryMock
                .Setup(repo => repo.RemoveByIdAsync(selfUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var sut = CreateSut();

            await sut.DeleteAccountAsync(new UserServiceDTOs.DeleteAccountRequestDTO(selfUserId), CancellationToken.None);

            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _userAccessChangeNotifierMock.Verify(
                notifier => notifier.NotifyAccessRevokedAsync(selfUserId, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_CallerBelowManager_ThrowsInsufficientPermissionsException()
        {
            SetActorRole(UserRole.Worker);

            var sut = CreateSut();

            await Assert.ThrowsAsync<InsufficientPermissionsException>(() => sut.RegisterAsync(
                new UserServiceDTOs.RegisterRequestDTO("New", "Worker", "new@ims.local", null, "Password123!", UserRole.Worker),
                CancellationToken.None));

            _userRepositoryMock.Verify(repo => repo.Add(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_ManagerRegisteringNonWorkerRole_ThrowsInsufficientPermissionsException()
        {
            SetActorRole(UserRole.Manager);

            var sut = CreateSut();

            await Assert.ThrowsAsync<InsufficientPermissionsException>(() => sut.RegisterAsync(
                new UserServiceDTOs.RegisterRequestDTO("New", "Manager", "new@ims.local", null, "Password123!", UserRole.Manager),
                CancellationToken.None));
        }

        [Fact]
        public async Task RegisterAsync_ManagerAssigningWarehouseWithoutAccess_ThrowsInsufficientPermissionsException()
        {
            var warehouseId = Guid.NewGuid();
            SetActorRole(UserRole.Manager, warehouseIds: [Guid.NewGuid()]);

            var sut = CreateSut();

            await Assert.ThrowsAsync<InsufficientPermissionsException>(() => sut.RegisterAsync(
                new UserServiceDTOs.RegisterRequestDTO("New", "Worker", "new@ims.local", null, "Password123!",
                    UserRole.Worker, [warehouseId]),
                CancellationToken.None));
        }

        [Fact]
        public async Task RegisterAsync_EmailAlreadyTaken_ThrowsEmailAlreadyTakenException()
        {
            SetActorRole(UserRole.Admin);

            _userRepositoryMock
                .Setup(repo => repo.ExistsByEmailAsync("taken@ims.local", It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var sut = CreateSut();

            await Assert.ThrowsAsync<EmailAlreadyTakenException>(() => sut.RegisterAsync(
                new UserServiceDTOs.RegisterRequestDTO("New", "Worker", "taken@ims.local", null, "Password123!", UserRole.Worker),
                CancellationToken.None));

            _userRepositoryMock.Verify(repo => repo.Add(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_PhoneNumberAlreadyTaken_ThrowsPhoneNumberAlreadyTakenException()
        {
            SetActorRole(UserRole.Admin);

            _userRepositoryMock
                .Setup(repo => repo.ExistsByPhoneNumberAsync("+10000000000", It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var sut = CreateSut();

            await Assert.ThrowsAsync<PhoneNumberAlreadyTakenException>(() => sut.RegisterAsync(
                new UserServiceDTOs.RegisterRequestDTO("New", "Worker", null, "+10000000000", "Password123!", UserRole.Worker),
                CancellationToken.None));
        }

        [Fact]
        public async Task RegisterAsync_Valid_AddsUserWithHashedPasswordAndAssignedWarehouses()
        {
            var warehouseId = Guid.NewGuid();
            SetActorRole(UserRole.Admin);

            _passwordHasherServiceMock
                .Setup(hasher => hasher.Hash("Password123!"))
                .Returns("hashed-password");

            var sut = CreateSut();

            var response = await sut.RegisterAsync(
                new UserServiceDTOs.RegisterRequestDTO("New", "Worker", "new@ims.local", null, "Password123!",
                    UserRole.Worker, [warehouseId]),
                CancellationToken.None);

            Assert.NotEqual(Guid.Empty, response.UserId);

            _userRepositoryMock.Verify(repo => repo.Add(It.Is<User>(user =>
                user.Email == "new@ims.local" && user.PasswordHash == "hashed-password"
                    && user.Role == UserRole.Worker && user.WarehouseIds.Contains(warehouseId))),
                Times.Once);

            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_AdminRegisteringManagerRole_Succeeds()
        {
            SetActorRole(UserRole.Admin);

            var sut = CreateSut();

            var response = await sut.RegisterAsync(
                new UserServiceDTOs.RegisterRequestDTO("New", "Manager", "manager@ims.local", null, "Password123!", UserRole.Manager),
                CancellationToken.None);

            Assert.NotEqual(Guid.Empty, response.UserId);

            _userRepositoryMock.Verify(repo => repo.Add(It.Is<User>(user => user.Role == UserRole.Manager)), Times.Once);
        }

        [Fact]
        public async Task ChangeRoleAsync_CallerBelowAdmin_ThrowsInsufficientPermissionsException()
        {
            SetActorRole(UserRole.Manager);

            var sut = CreateSut();

            await Assert.ThrowsAsync<InsufficientPermissionsException>(() => sut.ChangeRoleAsync(
                new UserServiceDTOs.ChangeRoleRequestDTO(Guid.NewGuid(), UserRole.Manager), CancellationToken.None));
        }

        [Fact]
        public async Task ChangeRoleAsync_UserNotFound_ThrowsNotFoundException()
        {
            SetActorRole(UserRole.Admin);

            var targetUserId = Guid.NewGuid();

            _userRepositoryMock
                .Setup(repo => repo.GetByIdAsync(targetUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            var sut = CreateSut();

            await Assert.ThrowsAsync<NotFoundException>(() => sut.ChangeRoleAsync(
                new UserServiceDTOs.ChangeRoleRequestDTO(targetUserId, UserRole.Manager), CancellationToken.None));
        }

        [Fact]
        public async Task ChangeRoleAsync_Valid_ChangesRoleAndSaves()
        {
            SetActorRole(UserRole.Admin);

            var user = new User("Test", "Worker", "worker@ims.local", null, UserRole.Worker, "password-hash");

            _userRepositoryMock
                .Setup(repo => repo.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var sut = CreateSut();

            await sut.ChangeRoleAsync(new UserServiceDTOs.ChangeRoleRequestDTO(user.Id, UserRole.Manager), CancellationToken.None);

            Assert.Equal(UserRole.Manager, user.Role);
            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
