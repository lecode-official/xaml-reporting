
#region Using Directives

using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media;

#endregion

namespace System.Windows.Documents.Reporting
{
    /// <summary>
    /// Represents the abstract base class for all kinds of document parts. Document parts make up the content of a document.
    /// </summary>
    public abstract class DocumentPart : FrameworkElement
    {
        #region Public Abstract Methods

        /// <summary>
        /// Renders the document part.
        /// </summary>
        /// <param name="dataContext">The data context, that is to be used during the rendering. The document part can bind to the content of this data context.</param>
        /// <returns>Returns a list of rendered fixed pages, which can then be added to the document.</returns>
        public abstract Task<IEnumerable<FixedPage>> RenderAsync(object dataContext);

        #endregion

        #region Protected Static Methods

        /// <summary>
        /// A helper method, which makes it possible to find a child dependency object within the specified dependency object.
        /// </summary>
        /// <typeparam name="T">The type of dependency object that is to be searched for.</typeparam>
        /// <param name="dependencyObject">The dependency object within is to be searched.</param>
        /// <returns>Returns the first child of the specified type that was found. If not child of the specified type was found, then <c>null</c> is returned.</returns>
        protected static T FindVisualChild<T>(DependencyObject dependencyObject) where T : DependencyObject
        {
            // Checks if a dependency object was specified, only if one was specified, the search has to be performed
            if (dependencyObject != null)
            {
                // Cycles over all direct children of the dependency object
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(dependencyObject); i++)
                {
                    // Gets the child at the current index
                    DependencyObject child = VisualTreeHelper.GetChild(dependencyObject, i);

                    // Checks if the child is of the specified type, if so then the child was found and is therefore returned
                    if (child != null && child is T)
                        return (T)child;

                    // This is a recursive depth-first search, so the children of the current child are searched recursively, if a child of the specified type could be found, then it is returned
                    child = DocumentPart.FindVisualChild<T>(child);
                    if (child != null && child is T)
                        return (T)child;
                }
            }

            // Since no dependency object was specified or no child of the specified type was found, null is returned
            return null;
        }

        #endregion
    }
}