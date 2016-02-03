using ReactiveUI;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Mvvm.Reactive;
using System.Windows.Mvvm.Services.Navigation;

namespace System.Windows.Documents.Reporting.Sample
{
    public class MainWindowViewModel : ReactiveViewModel
    {
        public MainWindowViewModel(ReportingService reportingService)
        {
            this.reportingService = reportingService;
        }

        private readonly ReportingService reportingService;

        private FixedDocument fixedDocument;
        public FixedDocument FixedDocument
        {
            get
            {
                return this.fixedDocument;
            }

            set
            {
                this.RaiseAndSetIfChanged(ref this.fixedDocument, value);
            }
        }

        private string fileName;
        public string FileName
        {
            get
            {
                return this.fileName;
            }

            set
            {
                this.RaiseAndSetIfChanged(ref this.fileName, value);
            }
        }

        public ReactiveCommand<Unit> ExportToXpsCommand { get; private set; }
        public ReactiveCommand<Unit> ExportTableCommand { get; private set; }

        public override Task OnActivateAsync()
        {
            this.FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Export.pdf");

            this.ExportToXpsCommand = ReactiveCommand.CreateAsyncTask(async x =>
            {
                await this.reportingService.ExportAsync<Document>(DocumentFormat.Pdf, this.FileName);
            });

            this.ExportTableCommand = ReactiveCommand.CreateAsyncTask(async x =>
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(@"C:\");

                Table<DirectoryInfo> table = new Table<DirectoryInfo>("Directories in C");
                foreach(DirectoryInfo subDirectoryInfo in directoryInfo.GetDirectories())
                    table.Rows.Add(subDirectoryInfo);

                table.Columns.Add(new Column<DirectoryInfo>("Name", y => y.Name));
                table.Columns.Add(new Column<DirectoryInfo>("Full name", y => y.FullName));
                table.Columns.Add(new Column<DirectoryInfo>("Last access time", y => y.LastAccessTime.ToString()));

                await this.reportingService.ExportAsync(table, TableFormat.Csv, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Export.csv"));
                await this.reportingService.ExportAsync(table, TableFormat.Xls, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Export.xls"));
                await this.reportingService.ExportAsync(table, TableFormat.Xlsx, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Export.xlsx"));
            });

            return Task.FromResult(0);
        }

        public override async Task OnNavigateToAsync(NavigationEventArgs e)
        {
            this.FixedDocument = await this.reportingService.RenderAsync<Document>();
        }
    }
}