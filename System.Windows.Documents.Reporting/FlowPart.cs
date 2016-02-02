
#region Using Directives

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Markup;

#endregion

namespace System.Windows.Documents.Reporting
{
    /// <summary>
    /// Represents a document part, which has flowing content. The flowing content is then paginated by using a fixed page.
    /// </summary>
    [ContentProperty(nameof(Content))]
    public class FlowPart : DocumentPart
    {
        #region Public Properties

        /// <summary>
        /// Gets or sets the page template, which is used during the rendering process to paginate the flow document. The page template can be used to render content that should be visible on all pages (e.g. headers, footers, page numbers, etc.).
        /// </summary>
        public DataTemplate PageTemplate { get; set; }

        /// <summary>
        /// Gets or sets the flowing content of the document. This content can be designed regardless of the document size. The content is automatically paginated during the rendering process, and wrapped in a page using the page template.
        /// </summary>
        public FlowDocument Content { get; set; }

        #endregion

        #region DocumentPart Implementation

        /// <summary>
        /// Paginates the flowing content, wraps the pages in the page defined by the page template, and renders the pages.
        /// </summary>
        /// <param name="dataContext">The data context, that is to be used during the rendering. The document part can bind to the content of this data context.</param>
        /// <returns>Returns the renderd pages.</returns>
        public override async Task<IEnumerable<FixedPage>> RenderAsync(object dataContext)
        {
            // If either the page template or the content is null, then nothing can be rendered, therefore an empty list of fixed pages is returned
            if (this.PageTemplate == null || this.Content == null)
                return new List<FixedPage>();

            // Gets the paginator source for the content, if it could not be retrieved, then nothing can be rendered, therefore an empty list of fixed pages is returned
            IDocumentPaginatorSource paginatorSource = this.Content as IDocumentPaginatorSource;
            if (paginatorSource == null)
                return new List<FixedPage>();

            // Sets the data contet of the content, so that it is able to bind against its contents
            this.Content.DataContext = dataContext;

            // Renders pages from the flow document content, till no more pages are available
            int currentPage = 0;
            List<FixedPage> renderedFixedPages = new List<FixedPage>();
            do
            {
                // Creates a new fixed page from the page template, for each page that is rendered from the flow content, a new page has to be created, because a fixed page must only be added once to the fixed document, if the template could not be instantiated, then nothing
                // can be rendered, therefore an empty list of fixed pages is returned
                FixedPage fixedPage = this.PageTemplate.LoadContent() as FixedPage;
                if (fixedPage == null)
                    return new List<FixedPage>();

                // Sets the data context for the page, so that the page is also able to bind against its contents
                fixedPage.DataContext = dataContext;

                // Initially fixed page has an actual width and height of 0, this makes it impossible for its contents to stretch the whole page, without having to size them absolutely, therefore the layout of the fixed page is updated, so that its actual width and height are correct
                fixedPage.Measure(new Size(fixedPage.Width, fixedPage.Height));
                fixedPage.Arrange(new Rect(0, 0, fixedPage.Width, fixedPage.Height));
                fixedPage.UpdateLayout();

                // Searches for the content presenter within the fixed page, the page template must have a content presenter, which is the control, that displays the paginated version of the flowing content, if no content presenter could be found, then nothing can be rendered,
                // therefore an empty list of fixed pages is returned
                ContentPresenter contentPresenter = DocumentPart.FindVisualChild<ContentPresenter>(fixedPage);
                if (contentPresenter == null)
                    return new List<FixedPage>();

                // If this is the first page, that is being rendered, then the flowing content has to be paginated first, to fit into the fixed pages, in order to do so, the number of pages needed must be computed first
                if (!renderedFixedPages.Any())
                {
                    // Sets the page size of the document paginator, so that it knows how to paginate the flowing content, the size is set to the size of the content presenter, which will later contain the paginated content
                    paginatorSource.DocumentPaginator.PageSize = new Size(contentPresenter.ActualWidth, contentPresenter.ActualHeight);

                    // Computes the amount of pages that can be generated from the flowing content
                    TaskCompletionSource<bool> pageCountCompletionSource = new TaskCompletionSource<bool>();
                    paginatorSource.DocumentPaginator.ComputePageCountCompleted += (sender, e) => pageCountCompletionSource.TrySetResult(true);
                    paginatorSource.DocumentPaginator.ComputePageCountAsync();
                    await pageCountCompletionSource.Task;

                    // If the calculated number of pages is 0, i.e. there is no content, then nothing can be rendered, therefore an empty list of fixed pages is returned
                    if (paginatorSource.DocumentPaginator.PageCount == 0)
                        return new List<FixedPage>();
                }

                // Paginates the current page
                TaskCompletionSource<DocumentPage> documentPageCompletionSource = new TaskCompletionSource<DocumentPage>();
                paginatorSource.DocumentPaginator.GetPageCompleted += (sender, e) => documentPageCompletionSource.TrySetResult(e.DocumentPage);
                paginatorSource.DocumentPaginator.GetPageAsync(currentPage);
                DocumentPage documentPage = await documentPageCompletionSource.Task;

                // Renders the paginated content in the content presenter (the layout needs to be updated, because otherwise the last page will appear empty when exporting the document to a file)
                contentPresenter.Content = new VisualHost { Visual = documentPage.Visual };
                contentPresenter.UpdateLayout();
                renderedFixedPages.Add(fixedPage);
            } while (++currentPage < paginatorSource.DocumentPaginator.PageCount);

            // Returns the rendered pages
            return renderedFixedPages;
        }

        #endregion
    }
}