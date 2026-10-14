using Application.Interfaces.Observers;
using Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Observers
{
    public class SubjectObserver : ISubjectObserver
    {
        private List<IAnalysisObserver> _observers = new List<IAnalysisObserver>();
        public void AddObserver(IAnalysisObserver observer)
        {
            _observers.Add(observer);
        }

        public async Task AddChange(AnalysisDto analysis)
        {
            await NotifyObservers(analysis);
        }

        public async Task NotifyObservers(AnalysisDto analysis)
        {
            foreach (var observer in _observers)
            {
                await observer.Analyze(analysis);
            }
        }
    }
}
