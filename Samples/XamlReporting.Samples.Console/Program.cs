
using Ninject;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Documents.Reporting;

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
            IKernel kernel = new StandardKernel();

            ReportingService reportingService = new ReportingService(kernel);

            await reportingService.ExportAsync<Document>(DocumentFormat.Xps, fileName);
        }
    }
}