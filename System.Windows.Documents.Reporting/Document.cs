using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace System.Windows.Documents.Reporting
{
    [ContentProperty(nameof(Parts))]
    public class Document : FrameworkElement
    {
        #region Public Properties

        public Collection<DocumentPart> Parts { get; private set; } = new Collection<DocumentPart>();

        #endregion

        #region Attached Properties

        private static DependencyPropertyKey pageNumberPropertyKey = DependencyProperty.RegisterAttachedReadOnly("PageNumber", typeof(int), typeof(FixedPage), new PropertyMetadata(1));

        public static DependencyProperty PageNumberProperty = Document.pageNumberPropertyKey.DependencyProperty;

        #endregion

        #region Public Methods

        public async Task<FixedDocument> RenderAsync(object dataContext)
        {
            FixedDocument fixedDocument = new FixedDocument();

            int currentPageNumber = 1;
            foreach (DocumentPart documentPart in this.Parts)
            {
                foreach (FixedPage fixedPage in await documentPart.RenderAsync(dataContext))
                {
                    fixedPage.SetValue(Document.pageNumberPropertyKey, currentPageNumber++);
                    fixedPage.UpdateLayout();
                    fixedDocument.Pages.Add(new PageContent { Child = fixedPage });
                }
            }

            return fixedDocument;
        }

        #endregion
    }
}