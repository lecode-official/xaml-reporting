
#region Using Directives

using System.Collections.Generic;
using System.Collections.ObjectModel;

#endregion

namespace System.Windows.Documents.Reporting
{
    /// <summary>
    /// Represents a table which can be exported.
    /// </summary>
    /// <typeparam name="T">The data type which represents a table row.</typeparam>
    public class Table<T> where T : class
    {
        #region Constructors

        /// <summary>
        /// Initializes a new <see cref="Table{T}"/> instance.
        /// </summary>
        public Table() { }

        /// <summary>
        /// Initializes a new <see cref="Table{T}"/> instance.
        /// </summary>
        /// <param name="name">The name of the table.</param>
        public Table(string name)
        {
            this.Name = name;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets the name of the table.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a value that determines whether a header row should be included.
        /// </summary>
        public bool IncludeHeader { get; set; } = true;

        /// <summary>
        /// Gets the rows of the table.
        /// </summary>
        public ICollection<T> Rows { get; private set; } = new Collection<T>();

        /// <summary>
        /// Gets the columns of the table.
        /// </summary>
        public ICollection<Column<T>> Columns { get; private set; } = new Collection<Column<T>>();

        #endregion
    }
}