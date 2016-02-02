
#region Using Directives

using System.Windows.Markup;
using System.Windows.Media;

#endregion

namespace System.Windows.Documents.Reporting
{
    /// <summary>
    /// Represents a framework element, which can be used to render a visual. This is needed, because the result of the pagination process of a flow document is a visual, which can not be directly rendered.
    /// </summary>
    [ContentProperty(nameof(Visual))]
    public class VisualHost : FrameworkElement
    {
        #region Dependency Properties

        /// <summary>
        /// Contains a dependency property for the visual content of the visual host.
        /// </summary>
        public static DependencyProperty VisualProperty = DependencyProperty.Register(nameof(Visual), typeof(Visual), typeof(VisualHost), new PropertyMetadata(null, (sender, e) =>
        {
            VisualHost visualHost = sender as VisualHost;

            if (e.OldValue != null)
                visualHost.RemoveVisualChild(e.OldValue as Visual);
            if (e.NewValue != null)
                visualHost.AddVisualChild(e.NewValue as Visual);
        }));

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets the visual content of the visual host.
        /// </summary>
        public Visual Visual
        {
            get
            {
                return this.GetValue(VisualHost.VisualProperty) as Visual;
            }

            set
            {
                this.SetValue(VisualHost.VisualProperty, value);
            }
        }

        #endregion

        #region FrameworkElement Implementation

        /// <summary>
        /// Gets the number of visual children of the visual host. If the visual host contains a visual, then 1 is returned, if not then 0 is returned.
        /// </summary>
        protected override int VisualChildrenCount
        {
            get
            {
                return this.Visual == null ? 0 : 1;
            }
        }

        /// <summary>
        /// Gets the visual child of the visual host at the specified index.
        /// </summary>
        /// <param name="index">The index of the visual child.</param>
        /// <returns>Returns the visual child at the specified index.</returns>
        protected override Visual GetVisualChild(int index)
        {
            if (this.Visual == null)
                return base.GetVisualChild(index);
            return this.Visual;
        }

        #endregion
    }
}