using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;
using System.Windows.Media;

namespace System.Windows.Documents.Reporting
{
    [ContentProperty(nameof(Visual))]
    public class VisualHost : FrameworkElement
    {
        public static DependencyProperty VisualProperty = DependencyProperty.Register(nameof(Visual), typeof(Visual), typeof(VisualHost), new PropertyMetadata(null, (sender, e) =>
        {
            VisualHost visualHost = sender as VisualHost;

            if (e.OldValue != null)
                visualHost.RemoveVisualChild(e.OldValue as Visual);
            if (e.NewValue != null)
                visualHost.AddVisualChild(e.NewValue as Visual);
        }));

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
        
        protected override int VisualChildrenCount
        {
            get
            {
                return this.Visual == null ? 0 : 1;
            }
        }

        protected override Visual GetVisualChild(int index)
        {
            if (this.Visual == null)
                return base.GetVisualChild(index);
            return this.Visual;
        }
    }
}