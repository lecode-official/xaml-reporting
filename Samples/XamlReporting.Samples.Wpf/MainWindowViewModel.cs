
#region Using Directives

using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Documents.Reporting;
using System.Windows.Mvvm.Reactive;
using System.Windows.Mvvm.Services.Navigation;

#endregion

namespace XamlReporting.Samples.Wpf
{
    /// <summary>
    /// Represents the view model for the main window.
    /// </summary>
    public class MainWindowViewModel : ViewModel
    {
        #region Constructors

        /// <summary>
        /// Initializes a new <see cref="MainWindowViewModel"/> instance.
        /// </summary>
        /// <param name="reportingService">The reporting service, which is used to render reports using XAML.</param>
        public MainWindowViewModel(ReportingService reportingService)
        {
            this.reportingService = reportingService;
        }

        #endregion

        #region Private Fields

        /// <summary>
        /// Contains the reporting service, which is used to render reports using XAML.
        /// </summary>
        private readonly ReportingService reportingService;

        #endregion

        #region Public Properties
        
        /// <summary>
        /// Gets the fixed document, which represents the rendered report.
        /// </summary>
        public ReactiveProperty<FixedDocument> FixedDocument { get; } = new ReactiveProperty<FixedDocument>();

        /// <summary>
        /// Gets or sets the name of the file into which the report is being exported.
        /// </summary>
        public ReactiveProperty<string> ReportFileName { get; } = new ReactiveProperty<string>();

        /// <summary>
        /// Gets or sets the name of the file into which the table is being exported.
        /// </summary>
        public ReactiveProperty<string> TableFileName { get; } = new ReactiveProperty<string>();

        /// <summary>
        /// Gets a command, which exports the report.
        /// </summary>
        public ReactiveCommand ExportReportCommand { get; private set; }

        /// <summary>
        /// Gets a command, which export a table.
        /// </summary>
        public ReactiveCommand ExportTableCommand { get; private set; }

        #endregion

        #region ReactiveViewModel Implementation

        /// <summary>
        /// Is called when the view model is created. Initializes the properties and the commands of the view model.
        /// </summary>
        public override Task OnActivateAsync()
        {
            // Initializes the file names of the exports
            this.ReportFileName.Value = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Report.pdf");
            this.TableFileName.Value = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Table.xlsx");

            // Initializes the commmand, that renders and exports the reports
            this.ExportReportCommand = new ReactiveCommand(async () =>
            {
                DocumentFormat documentFormat = Path.GetExtension(this.ReportFileName.Value).ToUpperInvariant() == ".XPS" ? DocumentFormat.Xps : DocumentFormat.Pdf;
                await this.reportingService.ExportAsync<Document>(documentFormat, this.ReportFileName.Value);
            });

            // Initializes the command, which exports a table
            this.ExportTableCommand = new ReactiveCommand(async () =>
            {
                // Creates an overview table of the directories in the directory to which the table is to be exported
                DirectoryInfo directoryInfo = new DirectoryInfo(Path.GetDirectoryName(this.TableFileName.Value));
                Table<DirectoryInfo> table = new Table<DirectoryInfo>("Directories Overview");
                foreach(DirectoryInfo subDirectoryInfo in directoryInfo.GetDirectories())
                    table.Rows.Add(subDirectoryInfo);
                table.Columns.Add(new Column<DirectoryInfo>("Name", y => y.Name));
                table.Columns.Add(new Column<DirectoryInfo>("Full name", y => y.FullName));
                table.Columns.Add(new Column<DirectoryInfo>("Last access time", y => y.LastAccessTime.ToString()));

                // Determines the table format and export the table
                TableFormat tableFormat = Path.GetExtension(this.TableFileName.Value).ToUpperInvariant() == ".CSV" ? TableFormat.Csv : (Path.GetExtension(this.TableFileName.Value).ToUpperInvariant() == ".XLS" ? TableFormat.Xls : TableFormat.Xlsx);
                await this.reportingService.ExportAsync(table, tableFormat, this.TableFileName.Value);
            });

            // Since no asynchronous operation is performed, an empty task is returned
            return Task.FromResult(0);
        }

        /// <summary>
        /// Is called when the user is navigated to the view corresponding to this view model. Renders the report for display.
        /// </summary>
        /// <param name="e">The event arguments, that contain more information about the navigation.</param>
        public override async Task OnNavigateToAsync(NavigationEventArgs e) => this.FixedDocument.Value = await this.reportingService.RenderAsync<Document>();

        #endregion
    }
}