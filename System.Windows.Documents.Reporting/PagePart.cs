
#region Using Directives

using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Markup;

#endregion

namespace System.Windows.Documents.Reporting
{
    /// <summary>
    /// Represents a document part, which has only one fixed page.
    /// </summary>
    [ContentProperty(nameof(Page))]
    public class PagePart : DocumentPart
    {
        #region Public Properties

        public FixedPage Page { get; set; }

        #endregion

        #region DocumentPart Implementation

        /// <summary>
        /// Renders the fixed page.
        /// </summary>
        /// <param name="dataContext">The data context, that is to be used during the rendering. The document part can bind to the content of this data context.</param>
        /// <returns>Returns a list, which only contains the single rendered fixed page.</returns>
        public override Task<IEnumerable<FixedPage>> RenderAsync(object dataContext)
        {
            // Checks if the fixed page exists, if not then nothing can be rendered, therefore an empty list of fixed pages is returned
            if (this.Page == null)
                return Task.FromResult(new List<FixedPage>() as IEnumerable<FixedPage>);

            // Sets the data context of the fixed page, so that it bind against its contents
            this.Page.DataContext = dataContext;

            // Initially fixed page has an actual width and height of 0, this makes it impossible for its contents to stretch the whole page, without having to size them absolutely, therefore the layout of the fixed page is updated, so that its actual width and height are correct
            this.Page.Measure(new Size(this.Page.Width, this.Page.Height));
            this.Page.Arrange(new Rect(0, 0, this.Page.Width, this.Page.Height));
            this.Page.UpdateLayout();

            // Returns a list, which only contains the one fixed page that was rendered
            return Task.FromResult(new List<FixedPage> { this.Page } as IEnumerable<FixedPage>);
        }

        #endregion
    }
}