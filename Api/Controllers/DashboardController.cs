using Application.DTOs;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController, Route("Dashboard"), Route("Analysis")]
    public class DashboardController : ControllerBase
    {
        private readonly IConsoleRepository repository;
        private readonly ICurrentUser user;
        private readonly IResourceAccessService access;
        public DashboardController(IConsoleRepository repository, ICurrentUser user, IResourceAccessService access)
        {
            this.repository = repository;
            this.user = user;
            this.access = access;
        }

        [HttpGet]
        public async Task<DashboardDto> Get([FromQuery] int? organizationId)
        {
            if (organizationId is int id)
            {
                await access.RequireOrganizationAsync(id);
            }

            return await repository.DashboardAsync(user.IsOwner ? organizationId : user.OrganizationId ?? -1, user.IsOwner ? null : user.Id ?? Guid.Empty);
        }
    }
}
