using Application.DTOs;
using Domain.Entities;
using System.Threading.Tasks;

namespace Application.Interfaces.Services
{
    public interface IOrganizationService
    {
        Task<OrganizationEntity> CreateOrganization(OrganizationDetailsDto org);
    }
}
