
namespace XamlReporting.Samples.AspDotNet
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
        public string Title { get; set; } = "Hello World from the Web";

        /// <summary>
        /// Gets or sets the abstract of the document.
        /// </summary>
        public string Abstract { get; set; } = "This is a simple test of the XAML Reporting engine in a ASP.NET 6 application.";

        /// <summary>
        /// Gets or sets the name of the author.
        /// </summary>
        public string Author { get; set; } = "John Doe";

        #endregion
    }
}