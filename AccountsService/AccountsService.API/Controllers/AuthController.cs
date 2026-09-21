using AccountsService.Application.Services.DTOs;
using AccountsService.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Kernel.AspNetCore;

namespace AccountsService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class AuthController(IAuthService mainService) : BaseController<IAuthService>(mainService)
    {
        [HttpPost]
        public async Task<ActionResult<AuthServiceDTOs.LoginResponseDTO>> Login(
            AuthServiceDTOs.LoginRequestDTO request, CancellationToken cancellationToken)
        {
            return Ok(await MainService.LoginAsync(request, cancellationToken));
        }

        [HttpPost]
        public async Task<ActionResult<AuthServiceDTOs.RefreshAccessTokenResponseDTO>> Refresh(
            AuthServiceDTOs.RefreshAccessTokenRequestDTO request, CancellationToken cancellationToken)
        {
            return Ok(await MainService.RefreshAccessTokenAsync(request, cancellationToken));
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<AuthServiceDTOs.LogoutResponseDTO>> Logout(
            AuthServiceDTOs.LogoutRequestDTO request, CancellationToken cancellationToken)
        {
            return Ok(await MainService.LogoutAsync(request, cancellationToken));
        }
    }
}