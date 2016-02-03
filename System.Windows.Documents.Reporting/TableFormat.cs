
namespace System.Windows.Documents.Reporting
{
    /// <summary>
    /// Represents a file format which can be used to export tables.
    /// </summary>
    public enum TableFormat
    {
        /// <summary>
        /// A comma separated list.
        /// </summary>
        Csv,

        /// <summary>
        /// The old Microsoft Excel workbook format.
        /// </summary>
        Xls,

        /// <summary>
        /// The Microsoft Excel OpenXML format.
        /// </summary>
        Xlsx
    }
}