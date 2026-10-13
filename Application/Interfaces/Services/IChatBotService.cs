using Application.DTOs;
using Domain.Entities;
using System.Threading.Tasks;

namespace Application.Interfaces.Services
{
    public interface IChatBotService
    {
        Task<ChatBotEntity> CreateChatBot(ChatBotDto chatBotDto);
    }
}