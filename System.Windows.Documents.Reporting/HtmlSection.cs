
#region Using Directives

using System.Windows.Markup;

#endregion

namespace System.Windows.Documents.Reporting
{
    /// <summary>
    /// Represents a section which converts HTML into a flow document content.
    /// </summary>
    [ContentProperty("Content")]
    public class HtmlSection : Section
    {
        #region Public Properties

        /// <summary>
        /// Contains the dependency property for the HTML content of the <see cref="HtmlSection"/>.
        /// </summary>
        public static DependencyProperty ContentProperty = DependencyProperty.Register("Content", typeof(string), typeof(HtmlSection), new PropertyMetadata(null, async (sender, e) =>
        {
            // Gets the HTML section, which triggered the value update
            HtmlSection htmlSection = sender as HtmlSection;
            if (htmlSection == null)
                return;

            // Converts the HTML code into flow document content and adds it to be block being displayed
            string newValue = e.NewValue as string;
            htmlSection.Blocks.Clear();
            if (!string.IsNullOrWhiteSpace(newValue))
                htmlSection.Blocks.Add(await HtmlConverter.ConvertFromStringAsync(e.NewValue.ToString()));
        }));

        /// <summary>
        /// Gets or sets the HTML content, which is to be displayed in the <see cref="HtmlSection"/>.
        /// </summary>
        public string Content
        {
            get
            {
                return this.GetValue(HtmlSection.ContentProperty).ToString();
            }

            set
            {
                this.SetValue(HtmlSection.ContentProperty, value);
            }
        }

        #endregion
    }
}