
#region Using Directives

using Ninject;
using PdfSharp.Pdf;
using PdfSharp.Xps;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Xps;
using System.Windows.Xps.Packaging;

#endregion

namespace System.Windows.Documents.Reporting
{
    /// <summary>
    /// Represents a service which can be used to generate documents and tables. The service supports printing and export to formats like XAML, XPS, PDF, CSV or XLS(X).
    /// </summary>
    public class ReportingService
    {
        #region Constructors

        /// <summary>
        /// Initializes a new <see cref="ReportingService"/> instance.
        /// </summary>
        /// <param name="kernel">The Ninject kernel, which is used to instantiate the documents and their corresponding view models.</param>
        public ReportingService(IKernel kernel)
        {
            // Validates the arguments
            if (kernel == null)
                throw new ArgumentNullException("kernel");

            // Stores the Ninject kernel for later use
            this.kernel = kernel;
        }

        #endregion

        #region Private Fields

        /// <summary>
        /// Contains the Ninjet kernel, which is used to instantiate views and view models.
        /// </summary>
        private IKernel kernel;

        /// <summary>
        /// Contains all cached types of the assembly of a view that has been created.
        /// </summary>
        private Type[] assemblyTypes = null;

        #endregion

        #region Private Methods

        /// <summary>
        /// Exports a document to XPS.
        /// </summary>
        /// <typeparam name="T">The type of document that is to be rendered and exported.</typeparam>
        /// <param name="outputStream">The stream to which the file is written.</param>
        /// <param name="parameters">The parameters that are to be injected into the view model of the document.</param>
        private async Task ExportToXpsAsync<T>(Stream outputStream, object parameters = null) where T : Document
        {
            // Creates a new task completion source, because the XPS document must always be rendered in an STA thread (which is not possible to do, when only calling Task.Run, therefore a new thread is created by hand)
            TaskCompletionSource<bool> exportTaskCompletionSource = new TaskCompletionSource<bool>();

            // The stream comes from a different thread, therefore it must be made thread safe
            Stream threadSafeStream = Stream.Synchronized(outputStream);

            // Creates a new thread in which the XPS document is rendered
            Thread thread = new Thread(new ThreadStart(async () =>
            {
                // Renders the document
                FixedDocument fixedDocument = await this.RenderAsync<T>(parameters);

                // Opens a new package and a document
                using (Package package = Package.Open(threadSafeStream, FileMode.Create))
                {
                    using (XpsDocument xpsDocument = new XpsDocument(package, CompressionOption.Normal))
                    {
                        // Creates a new writer for the XPS stream
                        XpsDocumentWriter xpsWriter = XpsDocument.CreateXpsDocumentWriter(xpsDocument);

                        // Writes out the fixed document to XPS
                        xpsWriter.Write(fixedDocument.DocumentPaginator);
                    }
                }

                // Since the rendering of the XPS document has finished, the task completion source is resolved
                exportTaskCompletionSource.TrySetResult(true);
            }));

            // Makes the thread a background thread (otherwise it would not be cancelled, when the application is quit)
            thread.IsBackground = true;

            // Makes the thread an STA thread, which is needed for rendering the XPS document (especially when using the library in a console application)
            thread.SetApartmentState(ApartmentState.STA);

            // Starts the thread and awaits the rendering of the XPS document
            thread.Start();
            await exportTaskCompletionSource.Task;
        }

        /// <summary>
        /// Exports a document to PDF.
        /// </summary>
        /// <typeparam name="T">The type of document that is to be rendered and exported.</typeparam>
        /// <param name="outputStream">The stream to which the file is written.</param>
        /// <param name="parameters">The parameters that are to be injected into the view model of the document.</param>
        private async Task ExportToPdfAsync<T>(Stream outputStream, object parameters = null) where T : Document
        {
            // Since there is no direct way to render a fixed document, the document is first rendered to an XPS document in a memory stream, which is later converted to a PDF document
            using (MemoryStream memoryStream = new MemoryStream())
            {
                // Renders the fixed document
                await this.ExportToXpsAsync<T>(memoryStream, parameters);

                // Creates a new PDF document and an loads the XPS document, which was just rendered, so it can be converted to the PDF document
                PdfDocument pdfDocument = new PdfDocument();
                PdfSharp.Xps.XpsModel.XpsDocument xpsDocument = PdfSharp.Xps.XpsModel.XpsDocument.Open(memoryStream);

                // Convers the XPS document to a PDF document, page by page
                XpsConverter xpsConverter = new XpsConverter(pdfDocument, xpsDocument);
                for (int currentPageIndex = 0; currentPageIndex < xpsDocument.GetDocument().PageCount; currentPageIndex++)
                {
                    PdfPage pdfPage = xpsConverter.CreatePage(currentPageIndex);
                    xpsConverter.RenderPage(pdfPage, currentPageIndex);
                }

                // Saves the rendered PDF document to the actual output stream
                pdfDocument.Save(outputStream, false);
            }
        }

        /// <summary>
        /// Exports a list of items with the specified columns to a file.
        /// </summary>
        /// <param name="table">The table which is to be exported.</param>
        /// <param name="outputStream">The stream to which the XPS file is written.</param>
        private async Task ExportToCsvAsync<T>(Table<T> table, Stream outputStream) where T : class
        {
            // Initializes the rows
            List<string> rows = new List<string>();

            // Adds the header row if requested
            if (table.IncludeHeader)
            {
                rows.Add(string.Join(";", table.Columns
                    .Select(column => column.Header ?? string.Empty)
                    .Select(column => column.Replace("\"", "\\\""))
                    .Select(column => string.Concat("\"", column, "\""))
                    .ToList()));
            }

            // Adds the rows
            foreach(T row in table.Rows)
            {
                rows.Add(string.Join(";", table.Columns
                    .Select(column => column.Formatter(row))
                    .Select(column => column.Replace("\"", "\\\""))
                    .Select(column => string.Concat("\"", column, "\""))
                    .ToList()));
            }

            // Saves the output in a stream
            byte[] bytes = Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, rows));
            await outputStream.WriteAsync(bytes, 0, bytes.Count());
        }
        
        #endregion

        #region Public Methods

        /// <summary>
        /// Exports a list of items with the specified columns to a file.
        /// </summary>
        /// <param name="table">The table which is to be exported.</param>
        /// <param name="format">The output format.</param>
        /// <param name="outputStream">The stream to which the XPS file is written.</param>
        public Task ExportAsync<T>(Table<T> table, TableFormat format, Stream outputStream) where T : class
        {
            switch (format)
            {
                default:
                    return this.ExportToCsvAsync(table, outputStream);
            }
        }

        /// <summary>
        /// Exports a list of items with the specified columns to a file.
        /// </summary>
        /// <param name="table">The table which is to be exported.</param>
        /// <param name="format">The output format.</param>
        /// <param name="fileName">The name of the output file.</param>
        public async Task ExportAsync<T>(Table<T> table, TableFormat format, string fileName) where T : class
        {
            using (FileStream fileStream = new FileStream(fileName, FileMode.Create))
                await this.ExportAsync(table, format, fileStream);
        }

        /// <summary>
        /// Renders the specified document.
        /// </summary>
        /// <typeparam name="T">The type of document that is to be rendered.</typeparam>
        /// <param name="parameters">The parameters that are to be injected into the view model of the document.</param>
        /// <returns>Returns the rendered fixed document.</returns>
        public async Task<FixedDocument> RenderAsync<T>(object parameters = null) where T : Document
        {
            // Determines the type of the view model, which can be done via attribute or convention
            Type viewModelType = null;
            object viewModel = null;
            ViewModelAttribute viewModelAttribute = typeof(T).GetCustomAttributes<ViewModelAttribute>().FirstOrDefault();
            if (viewModelAttribute != null)
                viewModelType = viewModelAttribute.ViewModelType;
            else if (this.assemblyTypes == null)
                this.assemblyTypes = typeof(T).Assembly.GetTypes();
            viewModelType = this.assemblyTypes.FirstOrDefault(type => type.Name == string.Concat(typeof(T).Name, "ViewModel"));

            // Checks if the document has a view model attribute, if so then the type specified in the attribute is used to instantiate a new view model for the document
            if (viewModelType != null)
            {
                // Safely instantiates the corresponding view model for the document
                object temporaryViewModel = null;
                try
                {
                    try
                    {
                        // Creates the view model via dependency injection
                        temporaryViewModel = this.kernel.Get(viewModelType);
                    }
                    catch (ActivationException e)
                    {
                        throw new InvalidOperationException(Resources.Localization.ReportingService.ViewModelCouldNotBeInstantiatedExceptionMessage, e);
                    }

                    // Checks if the user provided any custom parameters
                    if (parameters != null)
                    {
                        // Cycles through all properties of the parameters
                        foreach (PropertyDescriptor parameter in TypeDescriptor.GetProperties(parameters))
                        {
                            // Gets the information about the parameter in the view model
                            PropertyInfo parameterPropertyInfo = temporaryViewModel.GetType().GetProperty(parameter.Name);

                            // Checks if the property was found, the types match and if the setter is implemented, if not then the value cannot be assigned and we turn to the next parameter
                            if (parameterPropertyInfo == null || !parameterPropertyInfo.CanWrite || parameter.GetValue(parameters) == null)
                                continue;

                            // Sets the value of the parameter property in the view model to the value provided in the parameters
                            parameterPropertyInfo.SetValue(temporaryViewModel, parameter.GetValue(parameters));
                        }
                    }

                    // Converts the view model to the right base class and swaps the temporary view model with the final one
                    viewModel = temporaryViewModel;
                    temporaryViewModel = null;
                }
                finally
                {
                    // Checks if the temporary view model is not null, this only happens if an error occurred, therefore the view model is safely disposed of
                    if (temporaryViewModel != null && temporaryViewModel is IDisposable)
                        (temporaryViewModel as IDisposable).Dispose();
                }
            }

            // Instantiates the new document
            T document = null;
            try
            {
                // Lets the kernel instantiate the document, so that all dependencies can be injected
                document = this.kernel.Get<T>();

                // Since document is a framework element it must be properly initialized
                if (!document.IsInitialized)
                {
                    MethodInfo initializeComponentMethod = document.GetType().GetMethod("InitializeComponent", BindingFlags.Public | BindingFlags.Instance);
                    if (initializeComponentMethod != null)
                        initializeComponentMethod.Invoke(document, new object[0]);
                }
            }
            catch (ActivationException e)
            {
                throw new InvalidOperationException(Resources.Localization.ReportingService.DocumentCouldNotBeInstantiatedExceptionMessage, e);
            }
            
            // Renders the document and returns it
            return await document.RenderAsync(viewModel);
        }

        /// <summary>
        /// Renders and exports the specified document to the specified document format.
        /// </summary>
        /// <typeparam name="T">The type of document that is to be rendered and exported.</typeparam>
        /// <param name="documentFormat">The file format of the document to which it is to be exported.</param>
        /// <param name="outputStream">The output stream to which the rendered document file is written.</param>
        /// <param name="parameters">The parameters that are to be injected into the view model of the document.</param>
        public async Task ExportAsync<T>(DocumentFormat documentFormat, Stream outputStream, object parameters = null) where T : Document
        {
            switch (documentFormat)
            {
                case DocumentFormat.Xps:
                    await this.ExportToXpsAsync<T>(outputStream, parameters);
                    break;
                case DocumentFormat.Pdf:
                    await this.ExportToPdfAsync<T>(outputStream, parameters);
                    break;
            }
        }

        /// <summary>
        /// Renders and exports the specified document to the specified document format.
        /// </summary>
        /// <typeparam name="T">The type of document that is to be rendered and exported.</typeparam>
        /// <param name="documentFormat">The file format of the document to which it is to be exported.</param>
        /// <param name="fileName">The name of the file to which the rendered document file is written.</param>
        /// <param name="parameters">The parameters that are to be injected into the view model of the document.</param>
        public async Task ExportAsync<T>(DocumentFormat documentFormat, string fileName, object parameters = null) where T : Document
        {
            using (FileStream fileStream = new FileStream(fileName, FileMode.Create))
                await this.ExportAsync<T>(documentFormat, fileStream, parameters);
        }

        #endregion
    }
}