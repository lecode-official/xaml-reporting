
#region Using Directives

using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.PlatformAbstractions;
using System;
using System.InversionOfControl.Abstractions;
using System.InversionOfControl.Abstractions.SimpleIoc;
using System.IO;
using System.Windows.Documents.Reporting;

#endregion

namespace XamlReporting.Samples.AspDotNet
{
    /// <summary>
    /// Represents the entry point to the web application.
    /// </summary>
    public class Startup
    {
        #region Constructors

        /// <summary>
        /// Initializes a new <see cref="Startup"/> instance.
        /// </summary>
        /// <param name="applicationEnvironment">The information about the application environment of the web application.</param>
        public Startup(IApplicationEnvironment applicationEnvironment)
        {
            this.applicationEnvironment = applicationEnvironment;
        }

        #endregion

        #region Private Fields

        /// <summary>
        /// Contains the information about the application environment of the web application.
        /// </summary>
        private readonly IApplicationEnvironment applicationEnvironment;

        #endregion

        #region Public Static Methods

        /// <summary>
        /// This is the entry-point to the application. It bootstraps and runs the web application.
        /// </summary>
        /// <param name="args">The command line arguments, which is are passed to the application. This array should always be empty, since the application does not use command line arguments.</param>
        public static void Main(string[] args) => WebApplication.Run<Startup>(args);

        #endregion

        #region Public Methods

        /// <summary>
        /// Defines and configures the service, that are used by the web application.
        /// </summary>
        /// <param name="serviceCollection">The services collection, which is used to manage the services used by the web application.</param>
        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            // Configures the reporting service
            serviceCollection.AddSingleton<IReadOnlyIocContainer>(services => new SimpleIocContainer());
            serviceCollection.AddSingleton<ReportingService>();
        }

        /// <summary>
        /// Configures the request pipeline that is used by the application. All requests coming from clients are processed by the request pipeline. In ASP.NET 5 you compose your request pipeline using middleware. ASP.NET 5 middleware perform asynchronous logic on an HTTP
        /// context and then optionally invoke the next middleware in the sequence or terminate the request directly. Each middleware performs a single task when processing incoming requests and producing an outgoing response.
        /// </summary>
        /// <param name="applicationBuilder">The application builder, which is used to configure the request pipeline (e.g. by adding middlewares to it).</param>
        public void Configure(IApplicationBuilder applicationBuilder)
        {
            // Adds middleware for interacting with the IIS HttpPlatformHandler reverse proxy module, this will handle forwarded Windows Authentication, request scheme, remote IPs, etc.
            applicationBuilder.UseIISPlatformHandler();
            
            // Maps the /xps path to the creation of the PDF document
            applicationBuilder.Map("/xps", builder =>
            {
                builder.Run(async (context) =>
                {
                    try
                    {
                        ReportingService reportingService = applicationBuilder.ApplicationServices.GetService<ReportingService>();
                        context.Response.StatusCode = 200;
                        context.Response.Headers.SetCommaSeparatedValues("Content-Type", "application/vnd.ms-xpsdocument");
                        context.Response.Headers.SetCommaSeparatedValues("Content-Disposition", "attachment; filename=Document.xps");
                        await reportingService.ExportAsync<DocumentViewModel>(Path.Combine(this.applicationEnvironment.ApplicationBasePath, "Document.xaml"), DocumentFormat.Xps, context.Response.Body);
                    }
                    catch (Exception e)
                    {
                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsync($"Something went wrong during the creation of the report: {e.Message}.");
                    }
                });
            });
            
            // Maps the /pdf path to the creation of the PDF document
            applicationBuilder.Map("/pdf", builder =>
            {
                builder.Run(async (context) =>
                {
                    try
                    {
                        ReportingService reportingService = applicationBuilder.ApplicationServices.GetService<ReportingService>();
                        context.Response.StatusCode = 200;
                        context.Response.Headers.SetCommaSeparatedValues("Content-Type", "application/pdf");
                        context.Response.Headers.SetCommaSeparatedValues("Content-Disposition", "attachment; filename=Document.pdf");
                        await reportingService.ExportAsync<DocumentViewModel>(Path.Combine(this.applicationEnvironment.ApplicationBasePath, "Document.xaml"), DocumentFormat.Pdf, context.Response.Body);
                    }
                    catch (Exception e)
                    {
                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsync($"Something went wrong during the creation of the report: {e.Message}.");
                    }
                });
            });

            // Prints out an informational message when the user navigates to the default route
            applicationBuilder.Run(async context => await context.Response.WriteAsync("Please navigate to '/xps' or '/pdf' to generate the XPS or PDF version of the report respectively."));
        }

        #endregion
    }
}