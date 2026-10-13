using Application.DTOs;
using System.Threading.Tasks;

namespace Application.Interfaces.Observers
{
    public interface ISubjectObserver
    {
        void AddObserver(IAnalysisObserver observer);
        Task AddChange(AnalysisDto dto);
    }
}