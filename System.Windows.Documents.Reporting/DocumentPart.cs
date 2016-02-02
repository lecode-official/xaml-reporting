using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media;

namespace System.Windows.Documents.Reporting
{
    public abstract class DocumentPart : FrameworkElement
    {
        #region Public Abstract Methods

        public abstract Task<IEnumerable<FixedPage>> RenderAsync(object dataContext);

        #endregion

        #region Protected Static Methods

        protected static T FindVisualChild<T>(DependencyObject dependencyObject) where T : DependencyObject
        {
            if (dependencyObject != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(dependencyObject); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(dependencyObject, i);
                    if (child != null && child is T)
                    {
                        return (T)child;
                    }

                    child = FindVisualChild<T>(child);
                    if (child != null && child is T)
                    {
                        return (T)child;
                    }
                }
            }

            return null;
        }

        #endregion
    }
}