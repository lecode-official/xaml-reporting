
#region Using Directives

using System.Collections;
using System.IO;
using System.Windows.Markup;

#endregion

namespace System.Windows.Documents.Reporting
{
    /// <summary>
    /// Represents a paragraph that created inlines based on the elements in the items source.
    /// </summary>
    public class ItemsParagraph : Paragraph
    {
        #region Public Properties

        /// <summary>
        /// Contains the dependency property for the items source of the <see cref="ItemsParagraph"/>.
        /// </summary>
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register("ItemsSource", typeof(IEnumerable), typeof(ItemsParagraph), new PropertyMetadata(null, (sender, e) => (sender as ItemsParagraph)?.UpdateContent()));

        /// <summary>
        /// Gets or sets the items source of the <see cref="ItemsParagraph"/>.
        /// </summary>
        public IEnumerable ItemsSource
        {
            get
            {
                return this.GetValue(ItemsParagraph.ItemsSourceProperty) as IEnumerable;
            }

            set
            {
                this.SetValue(ItemsParagraph.ItemsSourceProperty, value);
            }
        }

        /// <summary>
        /// Contains the dependency property for the template of the inlines of the <see cref="ItemsParagraph"/>.
        /// </summary>
        public static readonly DependencyProperty ItemTemplateProperty = DependencyProperty.Register("ItemTemplate", typeof(DataTemplate), typeof(ItemsParagraph), new PropertyMetadata(null, (sender, e) => (sender as ItemsParagraph)?.UpdateContent()));

        /// <summary>
        /// Gets or sets the template for the inlines of the <see cref="ItemsParagraph"/>.
        /// </summary>
        public DataTemplate ItemTemplate
        {
            get
            {
                return this.GetValue(ItemsParagraph.ItemTemplateProperty) as DataTemplate;
            }

            set
            {
                this.SetValue(ItemsParagraph.ItemTemplateProperty, value);
            }
        }

        /// <summary>
        /// Contains the dependency property for the alternation count of <see cref="ItemsParagraph"/>.
        /// </summary>
        public static readonly DependencyProperty AlternationCountProperty = DependencyProperty.Register("AlternationCount", typeof(int?), typeof(ItemsParagraph), new PropertyMetadata(null, (sender, e) => (sender as ItemsParagraph)?.UpdateContent()));

        /// <summary>
        /// Gets or sets the alternation count of the <see cref="ItemsParagraph"/>.
        /// </summary>
        public int? AlternationCount
        {
            get
            {
                return (int?)this.GetValue(ItemsParagraph.AlternationCountProperty);
            }

            set
            {
                this.SetValue(ItemsParagraph.AlternationCountProperty, value);
            }
        }

        #endregion

        #region Attached Properties

        /// <summary>
        /// Contains a read-only dependency property, which always contains the current alternation index.
        /// </summary>
        private static DependencyPropertyKey alternationIndexPropertyKey = DependencyProperty.RegisterAttachedReadOnly("AlternationIndex", typeof(int), typeof(Inline), new PropertyMetadata(0));

        /// <summary>
        /// Contains the actual alternation index dependency property, which is available in XAML.
        /// </summary>
        public static DependencyProperty AlternationIndexProperty = ItemsParagraph.alternationIndexPropertyKey.DependencyProperty;

        #endregion

        #region Private Methods

        /// <summary>
        /// Updates the content of the paragraph.
        /// </summary>
        private void UpdateContent()
        {
            // Clears all inlines
            this.Inlines.Clear();

            // Checks whether the items source and the template are provided
            if (this.ItemsSource == null || this.ItemTemplate == null)
                return;

            // Adds the inlines to the collection of inlines
            int i = 0;
            foreach (object item in this.ItemsSource)
            {
                // Adds the inline
                Inline inline = this.ItemTemplate.LoadContent() as Inline;
                inline.SetValue(ItemsParagraph.alternationIndexPropertyKey, i);
                inline.DataContext = item;
                this.Inlines.Add(inline);

                // Increases the alternation counter
                i++;
                if (this.AlternationCount.HasValue)
                    i = i % this.AlternationCount.Value;
            }
        }

        #endregion
    }
}