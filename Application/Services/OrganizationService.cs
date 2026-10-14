using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.DTOs;
using Domain.Entities;
using System.Threading.Tasks;

namespace Application.Services
{
    public class OrganizationService : IOrganizationService
    {
        private readonly IApplicationRepository _appDb;
        public OrganizationService(IApplicationRepository appDb)
        {
            _appDb = appDb;
        }

        public async Task<OrganizationEntity> CreateOrganization(OrganizationDetailsDto org)
        {
            var organizationEntity = new OrganizationEntity
            {
                Name = org.Name,
                Email = org.Email,
                PhoneNumber = org.PhoneNumber
            };
            await _appDb.AddAsync(organizationEntity);
            await _appDb.SaveChangesAsync();
            return organizationEntity;
        }
    }
}
