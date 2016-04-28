using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OSIsoft.AF.Asset;
using OSIsoft.AF.Time;

namespace RunAnalysis
{
    public class NatualEventProvider : IEventProvider
    {
        private List<AFAttribute> _inputs;

        public NatualEventProvider(IEnumerable<AFAttribute> inputAttributes)
        {
            _inputs = inputAttributes.ToList();
        }

        public IEnumerable<AFTime> GetEvents(AFTimeRange timeRange)
        {
            // Access values for each inputs and use their timestamps to determine the "trigger times"
            var events = new HashSet<AFTime>();
            foreach (var input in _inputs)
            {
                // Assumptions (need more logic to handle other cases)
                // 1. All inputs are used as trigger inputs for the analysis
                // 2. Each input supports RecordedValues
                // 3. OK to get values for the entire timerange 
                // 4. This is not optimal, as retrieved values are only being used for timestamps. For calculation, we are 
                // accessing the values again.
                var values = input.Data.RecordedValues(timeRange, OSIsoft.AF.Data.AFBoundaryType.Inside, null, null, false);
                foreach (var value in values)
                    events.Add(value.Timestamp);
            }

            var uniqueEvents = events.ToList();
            uniqueEvents.Sort();

            foreach (var ev in uniqueEvents)
            {
                if (ev >= timeRange.StartTime && ev <= timeRange.EndTime)
                    yield return ev;
            }
        }
    }
}
