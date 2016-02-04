
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
                throw new ArgumentNullException("iocContainer");

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
        /// <param name="tables">The tables which are to be exported.</param>
        /// <param name="outputStream">The stream to which the CSV file is written.</param>
        private async Task ExportToCsvAsync<T>(IEnumerable<Table<T>> tables, Stream outputStream) where T : class
        {
            // Initializes the tables
            List<string> tableList = new List<string>();

            // Adds all tables as new paragraphs
            foreach (Table<T> table in tables)
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
                foreach (T row in table.Rows)
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
        private async Task ExportToXlsAsync<T>(IEnumerable<Table<T>> tables, Stream outputStream, bool useOpenXmlFormat) where T : class
        {
            // Creates a new workbook instance based on the requested XLS format
            IWorkbook workbook = useOpenXmlFormat ? new XSSFWorkbook() as IWorkbook : new HSSFWorkbook() as IWorkbook;

            // Adds all tables as new sheets
            foreach(Table<T> table in tables)
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
        public Task ExportAsync<T>(Table<T> table, TableFormat format, Stream outputStream) where T : class => this.ExportAsync(new List<Table<T>> { table }, format, outputStream);

        /// <summary>
        /// Exports a list of items with the specified columns to a file.
        /// </summary>
        /// <param name="table">The table which is to be exported.</param>
        /// <param name="format">The output format.</param>
        /// <param name="fileName">The name of the output file.</param>
        public Task ExportAsync<T>(Table<T> table, TableFormat format, string fileName) where T : class => this.ExportAsync(new List<Table<T>> { table }, format, fileName);

        /// <summary>
        /// Exports a list of items with the specified columns to a file.
        /// </summary>
        /// <param name="tables">The tables which is to be exported.</param>
        /// <param name="format">The output format.</param>
        /// <param name="outputStream">The stream to which the output file is written.</param>
        public async Task ExportAsync<T>(IEnumerable<Table<T>> tables, TableFormat format, Stream outputStream) where T : class
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
        public async Task ExportAsync<T>(IEnumerable<Table<T>> tables, TableFormat format, string fileName) where T : class
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
        public async Task<FixedDocument> RenderAsync<T>(object parameters = null) where T : Document
        {
            // Determines the type of the view model, which can be done via attribute or convention
            Type viewModelType = null;
            ViewModelAttribute viewModelAttribute = typeof(T).GetCustomAttributes<ViewModelAttribute>().FirstOrDefault();
            if (viewModelAttribute != null)
            {
                viewModelType = viewModelAttribute.ViewModelType;
            }
            else
            {
                this.assemblyTypes = this.assemblyTypes ?? typeof(T).Assembly.GetTypes();
                string viewModelName = this.ViewModelNamingConvention(typeof(T).Name);
                viewModelType = this.assemblyTypes.FirstOrDefault(type => type.Name == viewModelName);
            }
            
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

            // Instantiates the new document
            T document;
            try
            {
                // Lets the kernel instantiate the document, so that all dependencies can be injected
                document = this.iocContainer.GetInstance<T>();
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(Resources.Localization.ReportingService.DocumentCouldNotBeInstantiatedExceptionMessage, e);
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

        #region Public Properties

        /// <summary>
        /// Gets or sets the naming convention for view models. The function gets the name of the document type and returns the name of the corresponding view model. This function is used for convention-based view model activation. The default implementation adds "ViewModel" to the name of the document.
        /// </summary>
        public Func<string, string> ViewModelNamingConvention { get; set; } = documentName => string.Concat(documentName, "ViewModel");

        #endregion
    }
}