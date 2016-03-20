
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
        public string Html { get; } = @"
            <!DOCTYPE html>
            <html>
                <head></head>
                <body>
                    <h1>Heading 1</h1>
                    Hello, World!
                    <h2>Heading 2</h2>
                    <p>
                        This is a
                        <br/><br/>
                        <q><em><strong>quote</strong></em></q>
                    </p>
                    <h3>Heading 3</h3>
                    Check out
                    <s><a href='https://www.yahoo.com'>Yahoo</a></s>
                    <a href='https://www.bingc'><em>Google</em></a>
                </body>
            </html>";

        #endregion
    }
}