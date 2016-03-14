
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
        /// Gets an HTML string, which is displayed in the document.
        /// </summary>
        public string Html { get; } = @"<!DOCTYPE html><html><head></head><body>Hello, World!<p>This is a<br/><br/><em><strong>test</strong></em></p>Check out <a href='https://www.google.de'><em>Google</em></a></body></html>";

        #endregion
    }
}