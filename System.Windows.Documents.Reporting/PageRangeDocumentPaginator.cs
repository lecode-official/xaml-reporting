
#region Using Directives

using System.Reflection;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

#endregion

namespace MineralManagement.Clients.Desktop.Fixes
{
    /// <summary>
    /// Represents a paginator which only returns a specific range of pages.
    /// </summary>
    public class PageRangeDocumentPaginator : DocumentPaginator
    {
        #region Constructors

        /// <summary>
        /// Initializes a new <see cref="PageRangeDocumentPaginator"/>
        /// </summary>
        /// <param name="paginator">The original paginator.</param>
        /// <param name="startPageNumber">The starting page number.</param>
        /// <param name="endPageNumber">The ending page number.</param>
        public PageRangeDocumentPaginator(DocumentPaginator paginator, int startPageNumber, int endPageNumber)
        {
            this.startPageNumber = startPageNumber;
            this.endPageNumber = endPageNumber;
            this.paginator = paginator;
        }

        #endregion

        #region Private Fields

        /// <summary>
        /// Contains the starting page number.
        /// </summary>
        private int startPageNumber;

        /// <summary>
        /// Contains the ending page number.
        /// </summary>
        private int endPageNumber;

        /// <summary>
        /// Contains the original paginator.
        /// </summary>
        private DocumentPaginator paginator;

        #endregion

        #region Overridden Properties

        /// <summary>
        /// Gets a value that determines whether the page count is valid.
        /// </summary>
        public override bool IsPageCountValid
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets the page count.
        /// </summary>
        public override int PageCount
        {
            get
            {
                return this.endPageNumber - this.startPageNumber + 1;
            }
        }

        /// <summary>
        /// Gets or sets the page size.
        /// </summary>
        public override Size PageSize
        {
            get
            {
                return this.paginator.PageSize;
            }

            set
            {
                this.paginator.PageSize = value;
            }
        }

        /// <summary>
        /// Gets the source of the paginator.
        /// </summary>
        public override IDocumentPaginatorSource Source
        {
            get
            {
                return this.paginator.Source;
            }
        }

        #endregion

        #region Overridden Methods

        /// <summary>
        /// Gets the page with the specified page number.
        /// </summary>
        /// <param name="pageNumber">The page number of the page to be returned.</param>
        /// <returns>Returns the page.</returns>
        public override DocumentPage GetPage(int pageNumber)
        {
            DocumentPage page = this.paginator.GetPage(pageNumber + this.startPageNumber);

            // Creates a new container visual as a new parent for page children
            ContainerVisual containerVisual = new ContainerVisual();
            if (page.Visual is FixedPage)
            {
                foreach (UIElement child in ((FixedPage)page.Visual).Children)
                {
                    // Makes a shallow clone of the child using reflection
                    UIElement childClone = (UIElement)child.GetType().GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(child, null);

                    // Sets the parent of the cloned child to the created container visual by using reflection
                    FieldInfo parentField = childClone.GetType().GetField("_parent", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (parentField != null)
                    {
                        parentField.SetValue(childClone, null);
                        containerVisual.Children.Add(childClone);
                    }
                }

                // Returns the page
                return new DocumentPage(containerVisual, page.Size, page.BleedBox, page.ContentBox);
            }

            // Returns the page
            return page;
        }

        #endregion
    }
}