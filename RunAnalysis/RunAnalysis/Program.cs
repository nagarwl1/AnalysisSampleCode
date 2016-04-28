using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OSIsoft.AF;
using OSIsoft.AF.Asset;
using OSIsoft.AF.Time;

namespace RunAnalysis
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 4)
            {
                Console.WriteLine("Usage: RunAnalysis.exe <elementPath> <analysisName> <startTime> <endTime>");
                Console.WriteLine("(e.g. RunAnalysis.exe" + @"\\afservername\dbname\elementName analysisName 'y' 't')");
                return;
            }

            var elementPath = args[0];
            var analysisName = args[1];
            var st = args[2];
            var et = args[3];

            var element = AFObject.FindObject(elementPath) as AFElement;
            if (element == null)
            {
                Console.WriteLine("Failed to find element '{0}'", elementPath);
                return;
            }

            var analysis = element.Analyses[analysisName];
            if (analysis == null)
            {
                Console.WriteLine("Failed to find analysis '{0}|{1}'", elementPath, analysisName);
                return;
            }

            AFTime startTime;
            if (!AFTime.TryParse(st, out startTime))
            {
                Console.WriteLine("Invalid start time '{0}';", st);
                return;
            }

            AFTime endTime;
            if (!AFTime.TryParse(et, out endTime))
            {
                Console.WriteLine("Invalid end time '{0}';", et);
                return;
            }

            Console.WriteLine("Evaluating {0}|{1} from {2} to {3}...", elementPath, analysisName, startTime, endTime);
            var executor = new AnalysisExecutor(analysis);
            executor.Run(new AFTimeRange(startTime, endTime));

            if (Debugger.IsAttached)
                Console.Read();
        }
    }
}
