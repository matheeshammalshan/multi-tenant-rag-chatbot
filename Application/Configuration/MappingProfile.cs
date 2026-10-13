using Application.DTOs;
using Domain.Entities;
using AutoMapper;

namespace Application.Configuration
{
    public class MappingProfile: Profile
    {
        public MappingProfile()
        {
            CreateMap<OrganizationDetailsDto, OrganizationEntity>();
            CreateMap<ChatBotDto, ChatBotEntity>();
            CreateMap<LlmConfigurationsDto, LlmConfigurationEntity>();
            CreateMap<DocumentDto, DocumentEntity>();
            CreateMap<AnalysisDto, AnalysisEntity>();
            CreateMap<LlmConfigurationEntity, LlmConfigurationsDto>();
        }
    }
}