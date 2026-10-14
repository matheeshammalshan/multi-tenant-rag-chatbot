using Application.DTOs;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController, Route("Organization"), Route("Organizations")]
    public class OrganizationController : ControllerBase
    {
        private readonly IOrganizationService service;
        private readonly IConsoleRepository console;
        private readonly ICurrentUser user;
        private readonly IResourceAccessService access;
        public OrganizationController(IOrganizationService service, IConsoleRepository console, ICurrentUser user, IResourceAccessService access)
        {
            this.service = service;
            this.console = console;
            this.user = user;
            this.access = access;
        }

        [HttpGet]
        public Task<List<OrganizationSummaryDto>> List([FromQuery] int skip = 0, [FromQuery] int take = 100)
        {
            return console.OrganizationsAsync(user.IsOwner ? null : user.OrganizationId ?? -1, Math.Max(0, skip), Math.Clamp(take, 1, 200));
        }

        [HttpGet("{id:int}")]
        public async Task<OrganizationSummaryDto> Get(int id)
        {
            await access.RequireOrganizationAsync(id);
            return (await console.OrganizationsAsync(id, 0, 1)).Single();
        }

        [HttpPost("create"), Authorize(Policy = "PlatformOwner")]
        public async Task<IActionResult> Create(OrganizationDetailsDto request)
        {
            var result = await service.CreateOrganization(request);
            return Created($"/Organizations/{result.Id}", await Get(result.Id));
        }
    }
}
