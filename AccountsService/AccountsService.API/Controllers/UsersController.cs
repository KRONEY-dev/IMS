using AccountsService.Application.Services.DTOs;
using AccountsService.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Kernel.AspNetCore;

namespace AccountsService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    [Authorize]
    public class UsersController(IUserService userService) : BaseController<IUserService>(userService)
    {
        [HttpPost]
        public async Task<ActionResult<UserServiceDTOs.RegisterResponseDTO>> Register(
            UserServiceDTOs.RegisterRequestDTO request, CancellationToken cancellationToken)
        {
            return Ok(await MainService.RegisterAsync(request, cancellationToken));
        }

        [HttpPost]
        public async Task<ActionResult<UserServiceDTOs.DeleteAccountResponseDTO>> DeleteAccount(
            UserServiceDTOs.DeleteAccountRequestDTO request, CancellationToken cancellationToken)
        {
            return Ok(await MainService.DeleteAccountAsync(request, cancellationToken));
        }

        [HttpPost]
        public async Task<ActionResult<UserServiceDTOs.ChangeRoleResponseDTO>> ChangeRole(
            UserServiceDTOs.ChangeRoleRequestDTO request, CancellationToken cancellationToken)
        {
            return Ok(await MainService.ChangeRoleAsync(request, cancellationToken));
        }

        [HttpPost]
        public async Task<ActionResult<UserServiceDTOs.AssignWarehouseResponseDTO>> AssignWarehouse(
            UserServiceDTOs.AssignWarehouseRequestDTO request, CancellationToken cancellationToken)
        {
            return Ok(await MainService.AssignWarehouseAsync(request, cancellationToken));
        }

        [HttpPost]
        public async Task<ActionResult<UserServiceDTOs.RemoveWarehouseResponseDTO>> RemoveWarehouse(
            UserServiceDTOs.RemoveWarehouseRequestDTO request, CancellationToken cancellationToken)
        {
            return Ok(await MainService.RemoveWarehouseAsync(request, cancellationToken));
        }
    }
}