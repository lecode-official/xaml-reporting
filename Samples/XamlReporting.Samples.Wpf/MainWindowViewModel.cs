
#region Using Directives

using ReactiveUI;
using System;
using System.IO;
using System.Reactive;
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
    public class MainWindowViewModel : ReactiveViewModel
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
        /// Contains the fixed document, which represents the rendered report.
        /// </summary>
        private FixedDocument fixedDocument;

        /// <summary>
        /// Gets the fixed document, which represents the rendered report.
        /// </summary>
        public FixedDocument FixedDocument
        {
            get
            {
                return this.fixedDocument;
            }

            private set
            {
                this.RaiseAndSetIfChanged(ref this.fixedDocument, value);
            }
        }

        /// <summary>
        /// Contains a flow document, which is bound to the report.
        /// </summary>
        private FlowDocument flowDocument;

        /// <summary>
        /// Gets a flow document, which is bound to the report.
        /// </summary>
        public FlowDocument FlowDocument
        {
            get
            {
                return this.flowDocument;
            }

            private set
            {
                this.RaiseAndSetIfChanged(ref this.flowDocument, value);
            }
        }

        /// <summary>
        /// Contains the name of the file into which the report is being exported.
        /// </summary>
        private string reportFileName;

        /// <summary>
        /// Gets or sets the name of the file into which the report is being exported.
        /// </summary>
        public string ReportFileName
        {
            get
            {
                return this.reportFileName;
            }

            set
            {
                this.RaiseAndSetIfChanged(ref this.reportFileName, value);
            }
        }

        /// <summary>
        /// Contains the name of the file into which the table is being exported.
        /// </summary>
        private string tableFileName;

        /// <summary>
        /// Gets or sets the name of the file into which the table is being exported.
        /// </summary>
        public string TableFileName
        {
            get
            {
                return this.tableFileName;
            }

            set
            {
                this.RaiseAndSetIfChanged(ref this.tableFileName, value);
            }
        }

        /// <summary>
        /// Gets a command, which exports the report.
        /// </summary>
        public ReactiveCommand<Unit> ExportReportCommand { get; private set; }

        /// <summary>
        /// Gets a command, which export a table.
        /// </summary>
        public ReactiveCommand<Unit> ExportTableCommand { get; private set; }

        #endregion

        #region ReactiveViewModel Implementation

        /// <summary>
        /// Is called when the view model is created. Initializes the properties and the commands of the view model.
        /// </summary>
        public override Task OnActivateAsync()
        {
            // Initializes the file names of the exports
            this.ReportFileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Report.pdf");
            this.TableFileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Table.xlsx");

            // Initializes the commmand, that renders and exports the reports
            this.ExportReportCommand = ReactiveCommand.CreateAsyncTask(async x =>
            {
                DocumentFormat documentFormat = Path.GetExtension(this.ReportFileName).ToUpperInvariant() == ".XPS" ? DocumentFormat.Xps : DocumentFormat.Pdf;
                await this.reportingService.ExportAsync<Document>(documentFormat, this.ReportFileName);
            });

            // Initializes the command, which exports a table
            this.ExportTableCommand = ReactiveCommand.CreateAsyncTask(async x =>
            {
                // Creates an overview table of the directories in the directory to which the table is to be exported
                DirectoryInfo directoryInfo = new DirectoryInfo(Path.GetDirectoryName(this.TableFileName));
                Table<DirectoryInfo> table = new Table<DirectoryInfo>("Directories Overview");
                foreach(DirectoryInfo subDirectoryInfo in directoryInfo.GetDirectories())
                    table.Rows.Add(subDirectoryInfo);
                table.Columns.Add(new Column<DirectoryInfo>("Name", y => y.Name));
                table.Columns.Add(new Column<DirectoryInfo>("Full name", y => y.FullName));
                table.Columns.Add(new Column<DirectoryInfo>("Last access time", y => y.LastAccessTime.ToString()));

                // Determines the table format and export the table
                TableFormat tableFormat = Path.GetExtension(this.TableFileName).ToUpperInvariant() == ".CSV" ? TableFormat.Csv : (Path.GetExtension(this.TableFileName).ToUpperInvariant() == ".XLS" ? TableFormat.Xls : TableFormat.Xlsx);
                await this.reportingService.ExportAsync(table, tableFormat, this.TableFileName);
            });

            // Since no asynchronous operation is performed, an empty task is returned
            return Task.FromResult(0);
        }

        /// <summary>
        /// Is called when the user is navigated to the view corresponding to this view model. Renders the report for display.
        /// </summary>
        /// <param name="e">The event arguments, that contain more information about the navigation.</param>
        public override async Task OnNavigateToAsync(NavigationEventArgs e)
        {
            this.FlowDocument = new FlowDocument(await HtmlConverter.ConvertFromStringAsync(@"<!DOCTYPE html><html><head></head><body>Hello, World!<p>This is a<br/><br/><em><strong>test</strong></em></p>Check out <a href='https://www.google.de'><em>Google</em></a></body></html>"));
            this.FixedDocument = await this.reportingService.RenderAsync<Document>();
        }

        #endregion
    }
}