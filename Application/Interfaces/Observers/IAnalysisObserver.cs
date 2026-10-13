using Application.DTOs;
using System.Threading.Tasks;

namespace Application.Interfaces.Observers
{
    public interface IAnalysisObserver
    {
        Task Analyze(AnalysisDto dto);
    }
}