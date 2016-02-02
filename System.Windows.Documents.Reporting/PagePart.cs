
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace System.Windows.Documents.Reporting
{
    [ContentProperty(nameof(Page))]
    public class PagePart : DocumentPart
    {
        public FixedPage Page { get; set; }

        public override Task<IEnumerable<FixedPage>> RenderAsync(object dataContext)
        {
            if (this.Page == null)
                return Task.FromResult(new List<FixedPage>() as IEnumerable<FixedPage>);

            this.Page.DataContext = dataContext;
            
            this.Page.Measure(new Size(this.Page.Width, this.Page.Height));
            this.Page.Arrange(new Rect(0, 0, this.Page.Width, this.Page.Height));
            this.Page.UpdateLayout();

            return Task.FromResult(new List<FixedPage> { this.Page } as IEnumerable<FixedPage>);
        }
    }
}