using Ninject;
using System.Threading.Tasks;
using System.Windows.Mvvm.Application;
using System.Windows.Mvvm.Services.Navigation;

namespace System.Windows.Documents.Reporting.Sample
{
    /// <summary>
    /// Interaktionslogik für "App.xaml"
    /// </summary>
    public partial class App : MvvmApplication
    {
        protected override async Task OnStartedAsync(ApplicationStartedEventArgs eventArguments)
        {
            this.Kernel.Bind<WindowNavigationService>().ToSelf().InSingletonScope();
            this.Kernel.Bind<ReportingService>().ToSelf().InSingletonScope();
            WindowNavigationService windowNavigationService = this.Kernel.Get<WindowNavigationService>();
            await windowNavigationService.NavigateAsync<MainWindow>(true);
        }
    }
}