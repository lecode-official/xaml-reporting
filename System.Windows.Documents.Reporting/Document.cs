
#region Using Directives

using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Markup;

#endregion

namespace System.Windows.Documents.Reporting
{
    /// <summary>
    /// Represents the core component of the reporting engine, which can be used in XAML to create documents. Documents consist of a list of parts, which in turn contain the actual content.
    /// </summary>
    [ContentProperty(nameof(Parts))]
    public class Document : FrameworkElement
    {
        #region Public Properties

        /// <summary>
        /// Gets a list of the parts of the document. The parts are the actual content of the document.
        /// </summary>
        public Collection<DocumentPart> Parts { get; private set; } = new Collection<DocumentPart>();

        #endregion

        #region Attached Properties

        /// <summary>
        /// Contains a read-only dependency property, which always contains the current page number. This can be used to render page numbers within the XAML document, by binding to it.
        /// </summary>
        private static DependencyPropertyKey pageNumberPropertyKey = DependencyProperty.RegisterAttachedReadOnly("PageNumber", typeof(int), typeof(FixedPage), new PropertyMetadata(1));

        /// <summary>
        /// Contains the actual page number dependency property, which is available in XAML.
        /// </summary>
        public static DependencyProperty PageNumberProperty = Document.pageNumberPropertyKey.DependencyProperty;

        #endregion

        #region Public Methods

        /// <summary>
        /// Renders the document, by rendering each part of the document and concatenating them.
        /// </summary>
        /// <param name="dataContext">The data context, which is to be used during rendering. The document and its parts can bind to the contents of the data context.</param>
        /// <returns></returns>
        public async Task<FixedDocument> RenderAsync(object dataContext)
        {
            // Creates a new fixed document, which will contain the visual content of the parts of the document
            FixedDocument fixedDocument = new FixedDocument();

            // Cycles over all of the parts of the document, renders them, and adds the visuals to the fixed document
            int currentPageNumber = 1;
            foreach (DocumentPart documentPart in this.Parts)
            {
                // Renders the part of the document, the result of the rendering is a list of fixed pages (they are fixed in size), for each page the current page number is set, so that they are able to properly bind to the page number
                foreach (FixedPage fixedPage in await documentPart.RenderAsync(dataContext))
                {
                    // Sets the current page number, so that the fixed page is able to bind against it, the layout of the fixed page must be updated afterwards, because otherwise the bindings would not be updated during the exporting process
                    fixedPage.SetValue(Document.pageNumberPropertyKey, currentPageNumber++);
                    fixedPage.UpdateLayout();

                    // Adds the newly rendered fixed page to the fixed document
                    fixedDocument.Pages.Add(new PageContent { Child = fixedPage });
                }
            }

            // Returns the rendered fixed document
            return fixedDocument;
        }

        #endregion
    }
}