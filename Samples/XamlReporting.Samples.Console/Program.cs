
#region Using Directives

using System;
using System.Dynamic;
using System.InversionOfControl.Abstractions;
using System.InversionOfControl.Abstractions.SimpleIoc;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Documents.Reporting;

#endregion

namespace XamlReporting.Samples.Console
{
    /// <summary>
    /// Represents the console sample application, which showcases how to use the XAML Reporting engine.
    /// </summary>
    public static class Program
    {
        #region Public Static Methods

        /// <summary>
        /// Represents the entry point to the application.
        /// </summary>
        /// <param name="args">The command line parameters, which should always be empty, because they are not used.</param>
        public static void Main(string[] args) => Program.MainAsync().Wait();

        /// <summary>
        /// Represents the asynchronous entry point to the application, which allows us to use asynchorous methods.
        /// </summary>
        public static async Task MainAsync()
        {
            // Asks the user to specify a file name
            System.Console.Write("Specify file name (default is My Documents): ");
            string fileName = System.Console.ReadLine();
            if (string.IsNullOrWhiteSpace(fileName))
                fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Export.pdf");

            // Asks the user for his name
            System.Console.Write("What is your name: ");
            string documentAuthor = System.Console.ReadLine();

            // Creates the report, renders, and exports it
            System.Console.WriteLine("Generating document...");
            IIocContainer iocContainer = new SimpleIocContainer();
            ReportingService reportingService = new ReportingService(iocContainer);
            DocumentFormat documentFormat = Path.GetExtension(fileName).ToUpperInvariant() == ".XPS" ? DocumentFormat.Xps : DocumentFormat.Pdf;
            await reportingService.ExportAsync<Document>(documentFormat, fileName, string.IsNullOrWhiteSpace(documentAuthor) ? null : new { Author = documentAuthor });
            System.Console.WriteLine("Finished generating document");

            // Waits for a keystroke before the application is quit
            System.Console.WriteLine("Press any key to exit...");
            System.Console.ReadLine();
        }

        #endregion
    }
}