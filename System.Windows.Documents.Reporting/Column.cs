#region Using Directives


#endregion

namespace System.Windows.Documents.Reporting
{
    /// <summary>
    /// Represents the column of table which can be exported.
    /// </summary>
    /// <typeparam name="T">The data type which represents a table row.</typeparam>
    public class Column<T> where T : class
    {
        #region Constructors

        /// <summary>
        /// Initializes a new <see cref="Column{T}"/> instance.
        /// </summary>
        public Column() { }

        /// <summary>
        /// Initializes a new <see cref="Column{T}"/> instance.
        /// </summary>
        /// <param name="formatter">The formatter which formats the data.</param>
        public Column(Func<T, string> formatter)
        {
            this.Formatter = formatter;
        }

        /// <summary>
        /// Initializes a new <see cref="Column{T}"/> instance.
        /// </summary>
        /// <param name="header">The header of the column.</param>
        /// <param name="formatter">The formatter which formats the data.</param>
        public Column(string header, Func<T, string> formatter)
        {
            this.Header = header;
            this.Formatter = formatter;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets the header of the column.
        /// </summary>
        public string Header { get; set; }

        /// <summary>
        /// Gets or sets the formatter for the column.
        /// </summary>
        public Func<T, string> Formatter { get; set; }

        #endregion
    }
}