
#region Using Directives

using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using PdfSharp.Pdf;
using PdfSharp.Xps;
using System.Collections.Generic;
using System.InversionOfControl.Abstractions;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Markup;
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
        /// <param name="iocContainer">The IOC container which is used to instantiate the documents and their corresponding view models.</param>
        public ReportingService(IReadOnlyIocContainer iocContainer)
        {
            // Validates the arguments
            if (iocContainer == null)
                throw new ArgumentNullException(nameof(iocContainer));

            // Stores the IOC container for later use
            this.iocContainer = iocContainer;
        }

        #endregion

        #region Private Fields

        /// <summary>
        /// Contains the IOC container which is used to instantiate the documents and their corresponding view models.
        /// </summary>
        private IReadOnlyIocContainer iocContainer;

        /// <summary>
        /// Contains all cached types of the assembly of a view that has been created.
        /// </summary>
        private Type[] assemblyTypes = null;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets the naming convention for view models. The function gets the name of the document type and returns the name of the corresponding view model. This function is used for convention-based view model activation. The default implementation adds "ViewModel" to the name of the document.
        /// </summary>
        public Func<string, string> ViewModelNamingConvention { get; set; } = documentName => string.Concat(documentName, "ViewModel");

        #endregion

        #region Private Methods

        /// <summary>
        /// Executes the specified method in an STA thread.
        /// </summary>
        /// <typeparam name="T">The type of the result that is being returned.</typeparam>
        /// <param name="method">The method that is to be executed in an STA thread.</param>
        /// <returns>Returns the result of the method execution.</returns>
        private async Task<TDocument> ExecuteInStaThreadAsync<TDocument>(Func<Task<TDocument>> method)
        {
            // Checks if the execution is already taking place on a STA thread, if so then the method is executed and nothing is done
            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
                return await method();

            // Creates a new STA thread in which the method is executed
            TaskCompletionSource<TDocument> taskCompletionSource = new TaskCompletionSource<TDocument>();
            Thread thread = new Thread(new ThreadStart(async () =>
            {
                try
                {
                    taskCompletionSource.TrySetResult(await method());
                }
                catch (Exception e)
                {
                    taskCompletionSource.TrySetException(e);
                }
            }));
            
            // Makes the thread a background thread (otherwise it would not be cancelled, when the application is quit)
            thread.IsBackground = true;

            // Makes the thread an STA thread
            thread.SetApartmentState(ApartmentState.STA);

            // Starts the thread and returns the result
            thread.Start();
            return await taskCompletionSource.Task;
        }

        /// <summary>
        /// Renders the specified document.
        /// </summary>
        /// <param name="document">The document that is to be rendered.</param>
        /// <param name="viewModelType">The type of the view model that is to be instantiated and used for the rendering.</param>
        /// <param name="parameters">The parameters that are to be injected into the view model of the document.</param>
        /// <returns>Returns the rendered document.</returns>
        private async Task<FixedDocument> RenderAsync(Document document, Type viewModelType, object parameters)
        {
            // Instantiates the new view model
            object viewModel = null;
            if (viewModelType != null)
            {
                try
                {
                    // Creates the view model via dependency injection
                    viewModel = this.iocContainer.GetInstance(viewModelType).Inject(parameters);
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException(Resources.Localization.ReportingService.ViewModelCouldNotBeInstantiatedExceptionMessage, e);
                }
            }
            
            // Since document is a framework element it must be properly initialized
            if (!document.IsInitialized)
            {
                MethodInfo initializeComponentMethod = document.GetType().GetMethod("InitializeComponent", BindingFlags.Public | BindingFlags.Instance);
                if (initializeComponentMethod != null)
                    initializeComponentMethod.Invoke(document, new object[0]);
            }

            // Renders the document and returns it
            return await document.RenderAsync(viewModel);
        }

        /// <summary>
        /// Exports a document to XPS.
        /// </summary>
        /// <typeparam name="T">The type of document that is to be rendered and exported.</typeparam>
        /// <param name="outputStream">The stream to which the file is written.</param>
        /// <param name="parameters">The parameters that are to be injected into the view model of the document.</param>
        private async Task ExportToXpsAsync<TDocument>(Stream outputStream, object parameters) where TDocument : Document => this.ExportToXps(await this.RenderAsync<TDocument>(parameters), outputStream);

        /// <summary>
        /// Exports a document to XPS.
        /// </summary>
        /// <typeparam name="TViewModel">The type of the view model with which the document is to be rendered.</typeparam>
        /// <param name="fileName">The name of the XAML file from which the document, that is to be exported to XPS, should be loaded.</param>
        /// <param name="outputStream">The stream to which the file is written.</param>
        /// <param name="parameters">The parameters that are to be injected into the view model of the document.</param>
        private async Task ExportToXpsAsync<TViewModel>(string fileName, Stream outputStream, object parameters) => this.ExportToXps(await this.RenderAsync<TViewModel>(fileName, parameters), outputStream);

        /// <summary>
        /// Exports the specified rendered fixed document to XPS.
        /// </summary>
        /// <param name="fixedDocument">The fixed document that is to be exported to XPS.</param>
        /// <param name="outputStream">The stream to which the file is written.</param>
        private void ExportToXps(FixedDocument fixedDocument, Stream outputStream)
        {
            // Tries to render the document and package it in an XPS document
            try
            {
                // Opens a new package and a document
                using (Package package = Package.Open(outputStream, FileMode.Create))
                {
                    using (XpsDocument xpsDocument = new XpsDocument(package, CompressionOption.Normal))
                    {
                        // Creates a new writer for the XPS stream
                        XpsDocumentWriter xpsWriter = XpsDocument.CreateXpsDocumentWriter(xpsDocument);

                        // Writes out the fixed document to XPS
                        xpsWriter.Write(fixedDocument.DocumentPaginator);
                    }
                }
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(Resources.Localization.ReportingService.DocumentCouldNotBeExportedToXpsExceptionMessage, e);
            }
        }

        /// <summary>
        /// Exports a document to PDF.
        /// </summary>
        /// <typeparam name="T">The type of document that is to be rendered and exported.</typeparam>
        /// <param name="outputStream">The stream to which the file is written.</param>
        /// <param name="parameters">The parameters that are to be injected into the view model of the document.</param>
        private async Task ExportToPdfAsync<TDocument>(Stream outputStream, object parameters) where TDocument : Document
        {
            // Since there is no direct way to render a fixed document, the document is first rendered to an XPS document in a memory stream, which is later converted to a PDF document
            using (MemoryStream memoryStream = new MemoryStream())
            {
                await this.ExportToXpsAsync<TDocument>(memoryStream, parameters);
                this.ExportToPdf(memoryStream, outputStream);
            }
        }

        /// <summary>
        /// Exports a document to PDF.
        /// </summary>
        /// <typeparam name="TViewModel">The type of the view model with which the document is to be rendered.</typeparam>
        /// <param name="fileName">The name of the XAML file from which the document, that is to be exported to PDF, should be loaded.</param>
        /// <param name="outputStream">The stream to which the file is written.</param>
        /// <param name="parameters">The parameters that are to be injected into the view model of the document.</param>
        private async Task ExportToPdfAsync<TViewModel>(string fileName, Stream outputStream, object parameters)
        {
            // Since there is no direct way to render a fixed document, the document is first rendered to an XPS document in a memory stream, which is later converted to a PDF document
            using (MemoryStream memoryStream = new MemoryStream())
            {
                await this.ExportToXpsAsync<TViewModel>(fileName, memoryStream, parameters);
                this.ExportToPdf(memoryStream, outputStream);
            }
        }

        /// <summary>
        /// Exports the specified rendered fixed document to PDF.
        /// </summary>
        /// <param name="inputStream">The input stream that contains the XPS document (there is no direct way to export a fixed document to PDF, so an XPS document is created first and then converted to PDF).</param>
        /// <param name="outputStream">The stream to which the file is written.</param>
        private void ExportToPdf(Stream inputStream, Stream outputStream)
        {
            // Tries to export the XPS document stored in the input stream to PDF
            try
            {
                // Creates a new PDF document and an loads the XPS document, which was just rendered, so it can be converted to the PDF document
                PdfDocument pdfDocument = new PdfDocument();
                PdfSharp.Xps.XpsModel.XpsDocument xpsDocument = PdfSharp.Xps.XpsModel.XpsDocument.Open(inputStream);

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
            catch (Exception e)
            {
                throw new InvalidOperationException(Resources.Localization.ReportingService.DocumentCouldNotBeExportedToPdfExceptionMessage, e);
            }
        }

        /// <summary>
        /// Exports a list of items with the specified columns to a file.
        /// </summary>
        /// <param name="tables">The tables which are to be exported.</param>
        /// <param name="outputStream">The stream to which the CSV file is written.</param>
        private async Task ExportToCsvAsync<TDocument>(IEnumerable<Table<TDocument>> tables, Stream outputStream) where TDocument : class
        {
            // Initializes the tables
            List<string> tableList = new List<string>();

            // Adds all tables as new paragraphs
            foreach (Table<TDocument> table in tables)
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
                foreach (TDocument row in table.Rows)
                {
                    rows.Add(string.Join(";", table.Columns
                        .Select(column => column.Formatter(row))
                        .Select(column => column.Replace("\"", "\\\""))
                        .Select(column => string.Concat("\"", column, "\""))
                        .ToList()));
                }

                // Adds the name of the table if more than one table is to be exported
                if (tables.Count() > 1)
                    tableList.Add(string.Concat(string.IsNullOrWhiteSpace(table.Name) ? string.Empty : table.Name, Environment.NewLine, string.Join(Environment.NewLine, rows)));
                else
                    tableList.Add(string.Join(Environment.NewLine, rows));
            }

            // Saves the output in a stream
            byte[] bytes = Encoding.UTF8.GetBytes(string.Join(string.Concat(Environment.NewLine, Environment.NewLine), tableList));
            await outputStream.WriteAsync(bytes, 0, bytes.Count());
        }

        /// <summary>
        /// Exports a list of items with the specified columns to a file.
        /// </summary>
        /// <param name="tables">The tables which are to be exported.</param>
        /// <param name="outputStream">The stream to which the XLS(X) file is written.</param>
        /// <param name="useOpenXmlFormat">A value that determines whether the OpenXML format should be generated.</param>
        private async Task ExportToXlsAsync<TDocument>(IEnumerable<Table<TDocument>> tables, Stream outputStream, bool useOpenXmlFormat) where TDocument : class
        {
            // Creates a new workbook instance based on the requested XLS format
            IWorkbook workbook = useOpenXmlFormat ? new XSSFWorkbook() as IWorkbook : new HSSFWorkbook() as IWorkbook;

            // Adds all tables as new sheets
            foreach(Table<TDocument> table in tables)
            {
                // Creates a new sheet
                ISheet sheet = workbook.CreateSheet(string.IsNullOrWhiteSpace(table.Name) ? null : table.Name);

                // Adds the header row if requested
                if (table.IncludeHeader)
                {
                    // Creates a new header row
                    IRow row = sheet.CreateRow(0);

                    // Cycles over all columns of the table and adds the corresponding header cells
                    for (int i = 0; i < table.Columns.Count; i++)
                    {
                        // Creates a new cell
                        ICell cell = row.CreateCell(i);

                        // Sets the value of the call by using the header of the column
                        if (!string.IsNullOrWhiteSpace(table.Columns.ElementAt(i).Header))
                            cell.SetCellValue(table.Columns.ElementAt(i).Header);
                    }
                }

                // Adds the rows
                for (int i = 0; i < table.Rows.Count; i++)
                {
                    // Creates a new row
                    IRow row = sheet.CreateRow(i + (table.IncludeHeader ? 1 : 0));

                    // Cycles over all columns of the table and adds the corresponding cells
                    for (int j = 0; j < table.Columns.Count; j++)
                    {
                        // Creates a new cell
                        ICell cell = row.CreateCell(j);

                        // Sets the value of the cell by using the formatter defined in the column
                        if (!string.IsNullOrWhiteSpace(table.Columns.ElementAt(j).Formatter(table.Rows.ElementAt(i))))
                            cell.SetCellValue(table.Columns.ElementAt(j).Formatter(table.Rows.ElementAt(i)));
                    }
                }
                
                // Auto sizes all columns of the sheet
                for (int i = 0; i < table.Columns.Count; i++)
                    sheet.AutoSizeColumn(i);
            }
            
            // Sets the first table as selected sheet
            if (tables.Any())
                workbook.SetActiveSheet(0);

            // Saves the output in a stream
            await Task.Run(() => workbook.Write(outputStream));
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Exports a list of items with the specified columns to a file.
        /// </summary>
        /// <param name="table">The table which is to be exported.</param>
        /// <param name="format">The output format.</param>
        /// <param name="outputStream">The stream to which the output file is written.</param>
        public Task ExportAsync<TDocument>(Table<TDocument> table, TableFormat format, Stream outputStream) where TDocument : class => this.ExportAsync(new List<Table<TDocument>> { table }, format, outputStream);

        /// <summary>
        /// Exports a list of items with the specified columns to a file.
        /// </summary>
        /// <param name="table">The table which is to be exported.</param>
        /// <param name="format">The output format.</param>
        /// <param name="fileName">The name of the output file.</param>
        public Task ExportAsync<TDocument>(Table<TDocument> table, TableFormat format, string fileName) where TDocument : class => this.ExportAsync(new List<Table<TDocument>> { table }, format, fileName);

        /// <summary>
        /// Exports a list of items with the specified columns to a file.
        /// </summary>
        /// <param name="tables">The tables which is to be exported.</param>
        /// <param name="format">The output format.</param>
        /// <param name="outputStream">The stream to which the output file is written.</param>
        public async Task ExportAsync<TDocument>(IEnumerable<Table<TDocument>> tables, TableFormat format, Stream outputStream) where TDocument : class
        {
            switch (format)
            {
                case TableFormat.Csv:
                    await this.ExportToCsvAsync(tables, outputStream);
                    break;
                case TableFormat.Xls:
                case TableFormat.Xlsx:
                    await this.ExportToXlsAsync(tables, outputStream, format == TableFormat.Xlsx);
                    break;
            }
        }

        /// <summary>
        /// Exports a list of items with the specified columns to a file.
        /// </summary>
        /// <param name="tables">The table which is to be exported.</param>
        /// <param name="format">The output format.</param>
        /// <param name="fileName">The name of the output file.</param>
        public async Task ExportAsync<TDocument>(IEnumerable<Table<TDocument>> tables, TableFormat format, string fileName) where TDocument : class
        {
            using (FileStream fileStream = new FileStream(fileName, FileMode.Create))
                await this.ExportAsync(tables, format, fileStream);
        }

        /// <summary>
        /// Renders the specified document.
        /// </summary>
        /// <typeparam name="T">The type of document that is to be rendered.</typeparam>
        /// <param name="parameters">The parameters that are to be injected into the view model of the document.</param>
        /// <returns>Returns the rendered fixed document.</returns>
        public async Task<FixedDocument> RenderAsync<TDocument>(object parameters = null) where TDocument : Document
        {
            // Determines the type of the view model, which can be done via attribute or convention
            Type viewModelType = null;
            ViewModelAttribute viewModelAttribute = typeof(TDocument).GetCustomAttributes<ViewModelAttribute>().FirstOrDefault();
            if (viewModelAttribute != null)
            {
                viewModelType = viewModelAttribute.ViewModelType;
            }
            else
            {
                this.assemblyTypes = this.assemblyTypes ?? typeof(TDocument).Assembly.GetTypes();
                string viewModelName = this.ViewModelNamingConvention(typeof(TDocument).Name);
                viewModelType = this.assemblyTypes.FirstOrDefault(type => type.Name == viewModelName);
            }
            
            // Instantiates the new document
            TDocument document;
            try
            {
                // Lets the kernel instantiate the document, so that all dependencies can be injected
                document = this.iocContainer.GetInstance<TDocument>();
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(Resources.Localization.ReportingService.DocumentCouldNotBeInstantiatedExceptionMessage, e);
            }

            // Renders the XAML document
            try
            {
                return await this.RenderAsync(document, viewModelType, parameters);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(Resources.Localization.ReportingService.DocumentCouldNotBeRenderedExceptionMessage, e);
            }
        }

        /// <summary>
        /// Loads the specified XAML file and renders it.
        /// </summary>
        /// <typeparam name="TViewModel">The type of the view model with which the document is to be rendered.</typeparam>
        /// <param name="fileName">The name of the XAML file that is to be loaded and rendered.</param>
        /// <param name="parameters">The parameters that are to be injected into the view model of the document.</param>
        /// <returns>Returns the rendered fixed document.</returns>
        public async Task<FixedDocument> RenderAsync<TViewModel>(string fileName, object parameters = null)
        {
            // Checks if the specified XAML file exists, if the file does not exist, then an exception is thrown
            if (!File.Exists(fileName))
                throw new FileNotFoundException(Resources.Localization.ReportingService.XamlFileCouldNotBeFoundExceptionMessage, fileName);
            
            // Tries to load the XAML file and instantiates a document for it
            Document document = null;
            try
            {
                // Tries to load the XAML file, if it could not be loaded, then an exception is thrown
                try
                {
                    using (StreamReader streamReader = new StreamReader(fileName))
                        document = XamlReader.Load(streamReader.BaseStream) as Document;
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException(Resources.Localization.ReportingService.XamlFileCouldNotBeLoadedExceptionMessage, e);
                }

                // Checks if the document could not be loaded, if so then an exception is thrown
                if (document == null)
                    throw new InvalidOperationException(Resources.Localization.ReportingService.DocumentCouldNotBeLoadedExceptionMessage);

                // Renders the document and returns it
                return await this.RenderAsync(document, typeof(TViewModel), parameters);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(Resources.Localization.ReportingService.DocumentCouldNotBeRenderedExceptionMessage, e);
            }
        }

        /// <summary>
        /// Renders and exports the specified document to the specified document format.
        /// </summary>
        /// <typeparam name="T">The type of document that is to be rendered and exported.</typeparam>
        /// <param name="documentFormat">The file format of the document to which it is to be exported.</param>
        /// <param name="outputStream">The output stream to which the rendered document file is written.</param>
        /// <param name="parameters">The parameters that are to be injected into the view model of the document.</param>
        public async Task ExportAsync<TDocument>(DocumentFormat documentFormat, Stream outputStream, object parameters = null) where TDocument : Document
        {
            // Tries to export the document, if it could not be exported, then an exception is thrown
            try
            {
                // Creates a new synchronized stream to make the stream thread-safe
                Stream threadSafeStream = Stream.Synchronized(new MemoryStream());

                // Executes the exporting on an STA thread
                await this.ExecuteInStaThreadAsync<bool>(async () =>
                {
                    // Checks which document type is to be exported and exports the document accordingly
                    switch (documentFormat)
                    {
                        case DocumentFormat.Xps:
                            await this.ExportToXpsAsync<TDocument>(threadSafeStream, parameters);
                            break;
                        case DocumentFormat.Pdf:
                            await this.ExportToPdfAsync<TDocument>(threadSafeStream, parameters);
                            break;
                    }

                    // Since everything went alright, true is returned
                    return true;
                });

                // Copies the contents of the synchronized stream into the output stream
                threadSafeStream.Seek(0, SeekOrigin.Begin);
                await threadSafeStream.CopyToAsync(outputStream);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(Resources.Localization.ReportingService.DocumentCouldNotBeExportedExceptionMessage, e);
            }
        }

        /// <summary>
        /// Renders and exports the specified document to the specified document format.
        /// </summary>
        /// <typeparam name="TViewModel">The type of the view model with which the document is to be rendered.</typeparam>
        /// <param name="fileName">The name of the XAML file, which contains the document that is to be exported.</param>
        /// <param name="documentFormat">The file format of the document to which it is to be exported.</param>
        /// <param name="outputStream">The output stream to which the rendered document file is written.</param>
        /// <param name="parameters">The parameters that are to be injected into the view model of the document.</param>
        public async Task ExportAsync<TViewModel>(string fileName, DocumentFormat documentFormat, Stream outputStream, object parameters = null)
        {
            // Tries to export the document, if it could not be exported, then an exception is thrown
            try
            {
                // Creates a new synchronized stream to make the stream thread-safe
                Stream threadSafeStream = Stream.Synchronized(new MemoryStream());

                // Executes the exporting on an STA thread
                await this.ExecuteInStaThreadAsync<bool>(async () =>
                {
                    // Checks which document type is to be exported and exports the document accordingly
                    switch (documentFormat)
                    {
                        case DocumentFormat.Xps:
                            await this.ExportToXpsAsync<TViewModel>(fileName, threadSafeStream, parameters);
                            break;
                        case DocumentFormat.Pdf:
                            await this.ExportToPdfAsync<TViewModel>(fileName, threadSafeStream, parameters);
                            break;
                    }

                    // Since everything went alright, true is returned
                    return true;
                });

                // Copies the contents of the synchronized stream into the output stream
                threadSafeStream.Seek(0, SeekOrigin.Begin);
                await threadSafeStream.CopyToAsync(outputStream);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(Resources.Localization.ReportingService.DocumentCouldNotBeExportedExceptionMessage, e);
            }
        }

        /// <summary>
        /// Renders and exports the specified document to the specified document format.
        /// </summary>
        /// <typeparam name="T">The type of document that is to be rendered and exported.</typeparam>
        /// <param name="documentFormat">The file format of the document to which it is to be exported.</param>
        /// <param name="fileName">The name of the file to which the rendered document file is written.</param>
        /// <param name="parameters">The parameters that are to be injected into the view model of the document.</param>
        public async Task ExportAsync<TDocument>(DocumentFormat documentFormat, string fileName, object parameters = null) where TDocument : Document
        {
            using (FileStream fileStream = new FileStream(fileName, FileMode.Create))
                await this.ExportAsync<TDocument>(documentFormat, fileStream, parameters);
        }

        /// <summary>
        /// Renders and exports the specified document to the specified document format.
        /// </summary>
        /// <typeparam name="TViewModel">The type of the view model with which the document is to be rendered.</typeparam>
        /// <param name="fileName">The name of the XAML file, which contains the document that is to be exported.</param>
        /// <param name="documentFormat">The file format of the document to which it is to be exported.</param>
        /// <param name="outputFileName">The name of the file to which the rendered document file is written.</param>
        /// <param name="parameters">The parameters that are to be injected into the view model of the document.</param>
        public async Task ExportAsync<TViewModel>(string fileName, DocumentFormat documentFormat, string outputFileName, object parameters = null)
        {
            using (FileStream fileStream = new FileStream(outputFileName, FileMode.Create))
                await this.ExportAsync<TViewModel>(fileName, documentFormat, fileStream, parameters);
        }

        #endregion
    }
}