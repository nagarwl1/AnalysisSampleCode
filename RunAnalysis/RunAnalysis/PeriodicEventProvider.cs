using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OSIsoft.AF.Time;

namespace RunAnalysis
{

    public class PeriodicEventProvider : IEventProvider
    {
        private AFTimeRule _timeRule;

        public PeriodicEventProvider(AFTimeRule timeRule)
        {
            if (timeRule == null || timeRule.PlugIn == null || timeRule.PlugIn.Name != "Periodic")
                throw new ArgumentException("Invalid time rule.");

            _timeRule = timeRule;
        }

        public IEnumerable<AFTime> GetEvents(AFTimeRange timeRange)
        {
            AFTime lastTime = timeRange.StartTime - TimeSpan.FromTicks(1);
            while (lastTime < timeRange.EndTime)
            {
                lastTime = _timeRule.GetNextEvent(lastTime).EndTime;
                yield return lastTime;
            }
        }
    }
}
