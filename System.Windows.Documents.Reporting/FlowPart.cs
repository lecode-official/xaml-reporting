using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Markup;

namespace System.Windows.Documents.Reporting
{
    [ContentProperty(nameof(Content))]
    public class FlowPart : DocumentPart
    {
        #region Public Properties

        public DataTemplate PageTemplate { get; set; }

        public FlowDocument Content { get; set; }

        #endregion
        
        #region DocumentPart Implementation

        public override async Task<IEnumerable<FixedPage>> RenderAsync(object dataContext)
        {
            if (this.PageTemplate == null || this.Content == null)
                return new List<FixedPage>();

            IDocumentPaginatorSource paginatorSource = this.Content as IDocumentPaginatorSource;
            if (paginatorSource == null)
                return new List<FixedPage>();

            this.Content.DataContext = dataContext;

            int currentPage = 0;
            List<FixedPage> renderedFixedPages = new List<FixedPage>();
            do
            {
                // Gets the first instance of the page template, so the size of the fixed page can be measured
                FixedPage fixedPage = this.PageTemplate.LoadContent() as FixedPage;
                if (fixedPage == null)
                    return new List<FixedPage>();

                fixedPage.DataContext = dataContext;

                fixedPage.Measure(new Size(fixedPage.Width, fixedPage.Height));
                fixedPage.Arrange(new Rect(0, 0, fixedPage.Width, fixedPage.Height));
                fixedPage.UpdateLayout();

                ContentPresenter contentPresenter = DocumentPart.FindVisualChild<ContentPresenter>(fixedPage);
                if (contentPresenter == null)
                    return new List<FixedPage>();

                if (!renderedFixedPages.Any())
                {
                    paginatorSource.DocumentPaginator.PageSize = new Size(contentPresenter.ActualWidth, contentPresenter.ActualHeight);

                    TaskCompletionSource<bool> pageCountCompletionSource = new TaskCompletionSource<bool>();
                    paginatorSource.DocumentPaginator.ComputePageCountCompleted += (sender, e) => pageCountCompletionSource.TrySetResult(true);
                    paginatorSource.DocumentPaginator.ComputePageCountAsync();
                    await pageCountCompletionSource.Task;

                    if (paginatorSource.DocumentPaginator.PageCount == 0)
                        return new List<FixedPage>();
                }

                TaskCompletionSource<DocumentPage> documentPageCompletionSource = new TaskCompletionSource<DocumentPage>();
                paginatorSource.DocumentPaginator.GetPageCompleted += (sender, e) => documentPageCompletionSource.TrySetResult(e.DocumentPage);
                paginatorSource.DocumentPaginator.GetPageAsync(currentPage);
                DocumentPage documentPage = await documentPageCompletionSource.Task;

                contentPresenter.Content = new VisualHost { Visual = documentPage.Visual };
                contentPresenter.UpdateLayout();
                renderedFixedPages.Add(fixedPage);
            } while (++currentPage < paginatorSource.DocumentPaginator.PageCount);

            return renderedFixedPages;
        }

        #endregion
    }
}