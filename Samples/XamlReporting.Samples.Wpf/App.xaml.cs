
#region Using Directives

using System.InversionOfControl.Abstractions;
using System.InversionOfControl.Abstractions.SimpleIoc;
using System.Threading.Tasks;
using System.Windows.Documents.Reporting;
using System.Windows.Mvvm.Application;
using System.Windows.Mvvm.Services.Navigation;

#endregion

namespace XamlReporting.Samples.Wpf
{
    /// <summary>
    /// Represents the MVVM application and serves as an entry-point to the application.
    /// </summary>
    public partial class App : MvvmApplication
    {
        #region Private Fields

        /// <summary>
        /// Contains the IOC container which is used by the navigation service to activate the views and view models.
        /// </summary>
        private IIocContainer iocContainer;

        #endregion

        #region MvvmApplication Implementation

        /// <summary>
        /// This is the entry-piont to the application, which gets called as soon as the application has finished starting up.
        /// </summary>
        /// <param name="eventArguments">The event arguments, that contains all necessary information about the application startup like the command line arguments.</param>
        protected override async Task OnStartedAsync(ApplicationStartedEventArgs eventArguments)
        {
            // Initializes the IOC container; in this sample the Simple IOC is used
            this.iocContainer = new SimpleIocContainer();

            // Binds the todo list item repository and some services to the IOC container, so that it can be automatically injected into view models
            this.iocContainer.RegisterType<IReadOnlyIocContainer>(() => this.iocContainer);
            this.iocContainer.RegisterType<WindowNavigationService>(Scope.Singleton);
            this.iocContainer.RegisterType<ReportingService>(Scope.Singleton);

            // Navigates the user to the main view
            WindowNavigationService windowNavigationService = this.iocContainer.GetInstance<WindowNavigationService>();
            await windowNavigationService.NavigateAsync<MainWindow>(true);
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes of all managed and unmanaged resources that have been allocated.
        /// </summary>
        /// <param name="disposing">
        /// Determines whether only unmanaged, or managed and unmanaged resources should be disposed of. This is needed when the method is called from the destructor, because when the destructor is called all managed resources have already been disposed of.
        /// </param>
        protected override void Dispose(bool disposing)
        {
            // Calls the base implementation
            base.Dispose(disposing);

            // Checks if managed resources should be disposed of
            if (disposing)
            {
                // Disposes of the IOC container
                if (this.iocContainer != null)
                    this.iocContainer.Dispose();
                this.iocContainer = null;
            }
        }

        #endregion
    }
}