
using System.Windows.Documents;

namespace XamlReporting.Samples.Wpf
{
    /// <summary>
    /// Represents the view model for the document that is to be created.
    /// </summary>
    public class DocumentViewModel
    {
        #region Public Properties

        /// <summary>
        /// Gets or sets the title of the document.
        /// </summary>
        public string Title { get; set; } = "Hello World from WPF";

        /// <summary>
        /// Gets or sets the abstract of the document.
        /// </summary>
        public string Abstract { get; set; } = "This is a simple test of the XAML Reporting engine in a WPF application.";

        /// <summary>
        /// Gets or sets the name of the author.
        /// </summary>
        public string Author { get; set; } = "Jane Doe";

        /// <summary>
        /// Gets or sets a flow document, which is bound to the report.
        /// </summary>
        public FlowDocument FlowDocument { get; set; }

        #endregion
    }
}