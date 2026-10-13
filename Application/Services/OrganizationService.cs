using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.DTOs;
using Domain.Entities;
using System.Threading.Tasks;
using AutoMapper;

namespace Application.Services
{
    public class OrganizationService : IOrganizationService
    {
        private readonly IApplicationRepository _appDb;
        private readonly IMapper _mapper;

        public OrganizationService(IApplicationRepository appDb, IMapper mapper)
        {
            _appDb = appDb;
            _mapper = mapper;
        }

        public async Task<OrganizationEntity> CreateOrganization(OrganizationDetailsDto org)
        {
            var organizationEntity = _mapper.Map<OrganizationDetailsDto, OrganizationEntity>(org);

            await _appDb.AddAsync(organizationEntity);
            await _appDb.SaveChangesAsync();
            return organizationEntity;
        }

    }   
}