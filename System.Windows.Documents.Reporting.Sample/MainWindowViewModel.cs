using ReactiveUI;
using System.IO;
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

        public override Task OnActivateAsync()
        {
            this.FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Export.pdf");

            this.ExportToXpsCommand = ReactiveCommand.CreateAsyncTask(async x =>
            {
                await this.reportingService.ExportAsync<Document>(DocumentFormat.Pdf, this.FileName);
            });

            return Task.FromResult(0);
        }

        public override async Task OnNavigateToAsync(NavigationEventArgs e)
        {
            this.FixedDocument = await this.reportingService.RenderAsync<Document>();
        }
    }
}