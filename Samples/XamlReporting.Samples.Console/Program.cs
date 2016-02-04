
#region Using Directives

using System;
using System.InversionOfControl.Abstractions;
using System.InversionOfControl.Abstractions.SimpleIoc;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Documents.Reporting;

#endregion

namespace XamlReporting.Samples.Console
{
    public static class Program
    {
        static void Main(string[] args)
        {
            string fileName;
            if (args.Length == 0)
                fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Export.xps");
            else
                fileName = args[0];

            Program.MainAsync(fileName).Wait();
        }

        static async Task MainAsync(string fileName)
        {
            IIocContainer iocContainer = new SimpleIocContainer();

            ReportingService reportingService = new ReportingService(iocContainer);

            await reportingService.ExportAsync<Document>(DocumentFormat.Xps, fileName);
        }
    }
}