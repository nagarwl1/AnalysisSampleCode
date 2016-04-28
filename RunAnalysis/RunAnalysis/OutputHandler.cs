using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OSIsoft.AF;
using OSIsoft.AF.Analysis;
using OSIsoft.AF.Asset;
using OSIsoft.AF.Data;
using OSIsoft.AF.EventFrame;

namespace RunAnalysis
{
    public class OutputHandler
    {
        private AFAnalysisRuleConfiguration _configuration;
        private List<AFValue> _values;
        private List<AFEventFrame> _eventFrames;

        public OutputHandler(AFAnalysisRuleConfiguration configuration)
        {
            _configuration = configuration;
            _values = new List<AFValue>();
            _eventFrames = new List<AFEventFrame>();
        }

        public void ProcessOutputs(IAFAnalysisRuleState state)
        {
            for (int i = 0; i < _configuration.ResolvedOutputs.Count; ++i)
            {
                var resolvedOutput = _configuration.ResolvedOutputs[i];
                var value = state.Outputs[i];
                if (value is AFValue)
                {
                    var afValue = (AFValue)value;
                    if (resolvedOutput.Attribute as AFAttribute != null)
                    {
                        // If this is not null, that means output is mapped. 
                        afValue.Attribute = resolvedOutput.Attribute as AFAttribute;
                        _values.Add(afValue);
                    }
                }
                else if (value is AFEventFrame)
                {
                    // TODO:
                }
            }
        }

        public void Flush()
        {
            var sw = Stopwatch.StartNew();

            int numValuesWritten = 0;
            AFErrors<AFValue> writeErrors = null;
            try
            {
                // Write results with "Replace" mode. Ideally you would first want to delete existing data and then publish new results, in order to make sure we don't end up with mixed old & new events.
                writeErrors = AFListData.UpdateValues(_values, AFUpdateOption.Replace);
                numValuesWritten += _values.Count;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to publish outputs using bulk call. {0}", ex.Message);
                return;
            }
            finally
            {
                _values.Clear();
            }

            if (!ReferenceEquals(writeErrors, null) && writeErrors.HasErrors)
            {
                foreach (var afError in writeErrors.PISystemErrors)
                {
                    Console.WriteLine("Error while writing outputs to AF Server {0}. {1}", afError.Key, afError.Value.Message);
                }

                foreach (var piError in writeErrors.PIServerErrors)
                {
                    Console.WriteLine("Error while writing outputs to Data archive {0}. {1}", piError.Key, piError.Value.Message);
                }

                if (writeErrors.Errors.Count > 0)
                {
                    numValuesWritten -= writeErrors.Errors.Count;
                    Console.WriteLine("{0} Errors when publishing output results.", writeErrors.Errors.Count);
                    foreach (var error in writeErrors.Errors)
                    {
                        Console.WriteLine("/t{0}: {1}", error.Key.Attribute.Name, error.Value.Message);
                    }
                }
            }

            Console.WriteLine("Published {0} values. ({1} ms)", numValuesWritten, sw.ElapsedMilliseconds);
        }
    }
}
