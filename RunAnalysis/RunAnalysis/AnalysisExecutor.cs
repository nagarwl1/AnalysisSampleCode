using System;
using System.Collections.Generic;
using System.Linq;
using OSIsoft.AF.Analysis;
using OSIsoft.AF.Asset;
using OSIsoft.AF.Time;

namespace RunAnalysis
{
    public interface IExecuteAnalysis
    {
        void Run(AFTimeRange timeRange);
    }

    public interface IEventProvider
    {
        IEnumerable<AFTime> GetEvents(AFTimeRange timeRange);
    }

    public class AnalysisExecutor : IExecuteAnalysis
    {
        private AFAnalysis _analysis;
        private AFAnalysisRuleConfiguration _configuration;

        public AnalysisExecutor(AFAnalysis analysis)
        {
            _analysis = analysis;
        }

        public void Run(AFTimeRange timeRange)
        {
            if (!TryValidate())
            {
                Console.WriteLine("Failed to evaluate '{0}' due to configuration errors.", _analysis.GetPath());
                return;
            }

            var eventProvider = GetEventProvider();
            var state = new AFAnalysisRuleState(_configuration);
            var outputHandler = new OutputHandler(_configuration);

            foreach (var time in eventProvider.GetEvents(timeRange))
            {
                Console.WriteLine("Evaluating for {0}", time);
                state.Reset();
                state.SetExecutionTimeAndPopulateInputs(time);
                _analysis.AnalysisRule.Run(state);

                Exception evaluationError = state.EvaluationError;
                if (!ReferenceEquals(evaluationError, null))
                {
                    AFAnalysisException analysisException = evaluationError as AFAnalysisException;
                    if (ReferenceEquals(analysisException, null) || analysisException.Severity == AFAnalysisErrorSeverity.Error)
                    {
                        // This is something for which Analysis service would stop running the analysis
                        Console.WriteLine("Fatal error: {0}", state.EvaluationError.Message);
                        break;
                    }
                    else
                    {
                        // Non-fatal error with severity < Error; analysis would continue to run
                        Console.WriteLine("Warning: {0}", state.EvaluationError.Message);
                    }
                }
                else
                {
                    outputHandler.ProcessOutputs(state);
                    foreach (var output in _configuration.ResolvedOutputs.Zip(state.Outputs, Tuple.Create))
                    {
                        Console.WriteLine("\t{0} = {1}", output.Item1.DisplayName, output.Item2);
                    }
                }
            }

            outputHandler.Flush();
        }


        private bool TryValidate()
        {
            if (_analysis.Status != AFStatus.Enabled)
            {
                Console.WriteLine("'{0}' is not enabled.", _analysis.GetPath());
                return false;
            }

            _configuration = _analysis.AnalysisRule.GetConfiguration();
            if (_configuration.HasExceptions)
            {
                var exceptionGroups = _configuration.ConfigurationExceptions.ToLookup(ex => ex.Severity);
                Console.WriteLine("Configuration warnings: {0}", exceptionGroups[AFAnalysisErrorSeverity.Warning].Count());
                foreach (var warning in exceptionGroups[AFAnalysisErrorSeverity.Warning])
                {
                    Console.WriteLine("\t{0}", warning.Message);
                }

                // warnings mean ok to run, but something may not work quite as expected.  
                int errorCount = exceptionGroups[AFAnalysisErrorSeverity.Error].Count();
                Console.WriteLine("Configuration errors: {0}", errorCount);
                foreach (var error in exceptionGroups[AFAnalysisErrorSeverity.Error])
                {
                    Console.WriteLine("\t{0}", error.Message);
                }

                // can't run if there are errors.  
                if (errorCount > 0)
                    return false;
            }

            var inputs = _configuration.GetInputs();
            var outputs = _configuration.GetOutputs();

            foreach (var output in outputs)
            {
                if (inputs.Any(ip => ip.ID == output.ID))
                {
                    Console.WriteLine("This approach for evaluating analyses does not support using same attribute(s) as input as well output. ('{0}')", output.Name);
                    return false;
                }
            }

            return true;
        }

        private IEventProvider GetEventProvider()
        {
            if (_analysis.TimeRulePlugIn.Name == "Periodic")
                return new PeriodicEventProvider(_analysis.TimeRule);
            else if (_analysis.TimeRulePlugIn.Name == "Natural")
                return new NatualEventProvider(_configuration.GetInputs().OfType<AFAttribute>());
            else
                throw new InvalidOperationException("Invalid time rule...");
        }
    }
}
