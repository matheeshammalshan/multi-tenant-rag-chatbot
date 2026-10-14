using Application.DTOs.Accounts;
using Application.Exceptions;
using Application.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController, Route("Users"), Authorize(Policy = "PlatformOwner")]
    public class UsersController : ControllerBase
    {
        private readonly IConsoleRepository console;
        private readonly IAuthRepository auth;
        public UsersController(IConsoleRepository console, IAuthRepository auth)
        {
            this.console = console;
            this.auth = auth;
        }

        [HttpGet]
        public Task<List<UserDto>> List([FromQuery] int skip = 0, [FromQuery] int take = 100)
        {
            return console.UsersAsync(Math.Max(0, skip), Math.Clamp(take, 1, 200));
        }

        [HttpPut("{id:guid}/organization")]
        public async Task<IActionResult> AssignOrganization(Guid id, [FromBody] int organizationId)
        {
            var user = await auth.FindUserAsync(id) ?? throw ApiException.NotFound("User");
            if (user.Role == Domain.Constants.Roles.Owner)
            {
                throw new ApiException(400, "The platform owner has global access.");
            }

            if (!await console.OrganizationExistsAsync(organizationId))
            {
                throw ApiException.NotFound("Organization");
            }

            if (user.OrganizationId != organizationId && (await console.ChatbotsAsync(null, id, 0, 1)).Count > 0)
            {
                throw new ApiException(409, "Reassign this user's chatbots before changing organization membership.");
            }

            user.OrganizationId = organizationId;
            user.SecurityStamp = Guid.NewGuid();
            await auth.RevokeSessionsAsync(id);
            await auth.SaveAsync();
            return NoContent();
        }

        [HttpPut("{id:guid}/status")]
        public async Task<IActionResult> Status(Guid id, [FromBody] bool active)
        {
            var user = await auth.FindUserAsync(id) ?? throw ApiException.NotFound("User");
            if (user.Role == Domain.Constants.Roles.Owner)
            {
                throw new ApiException(400, "The platform owner cannot be deactivated here.");
            }

            user.IsActive = active;
            user.SecurityStamp = Guid.NewGuid();
            await auth.RevokeSessionsAsync(user.Id);
            await auth.SaveAsync();
            return NoContent();
        }
    }
}
