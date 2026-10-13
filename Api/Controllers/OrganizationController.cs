using Application.Interfaces.Services;
using Application.DTOs;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OrganizationController : ControllerBase
    {
        private readonly IOrganizationService _organizationService;
    
        public OrganizationController(IOrganizationService organization){
             _organizationService=organization;
        }
    
        [HttpPost("create")]
        public async Task<IActionResult> CreateOrganization([FromBody] OrganizationDetailsDto org)
        {
            var result=await _organizationService.CreateOrganization(org);
            return Ok();
        }
    
    



    }
}
