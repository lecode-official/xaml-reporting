
#region Using Directives

using System.Collections;
using System.IO;
using System.Windows.Markup;

#endregion

namespace System.Windows.Documents.Reporting
{
    /// <summary>
    /// Represents a paragraph that created blocks based on the elements in the items source.
    /// </summary>
    public class ItemsSection : Section
    {
        #region Public Properties

        /// <summary>
        /// Contains the dependency property for the items source of the <see cref="ItemsSection"/>.
        /// </summary>
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register("ItemsSource", typeof(IEnumerable), typeof(ItemsSection), new PropertyMetadata(null, (sender, e) => (sender as ItemsSection)?.UpdateContent()));

        /// <summary>
        /// Gets or sets the items source of the <see cref="ItemsSection"/>.
        /// </summary>
        public IEnumerable ItemsSource
        {
            get
            {
                return this.GetValue(ItemsSection.ItemsSourceProperty) as IEnumerable;
            }

            set
            {
                this.SetValue(ItemsSection.ItemsSourceProperty, value);
            }
        }

        /// <summary>
        /// Contains the dependency property for the template of the blocks of the <see cref="ItemsSection"/>.
        /// </summary>
        public static readonly DependencyProperty ItemTemplateProperty = DependencyProperty.Register("ItemTemplate", typeof(DataTemplate), typeof(ItemsSection), new PropertyMetadata(null, (sender, e) => (sender as ItemsSection)?.UpdateContent()));

        /// <summary>
        /// Gets or sets the template for the blocks of the <see cref="ItemsSection"/>.
        /// </summary>
        public DataTemplate ItemTemplate
        {
            get
            {
                return this.GetValue(ItemsSection.ItemTemplateProperty) as DataTemplate;
            }

            set
            {
                this.SetValue(ItemsSection.ItemTemplateProperty, value);
            }
        }

        /// <summary>
        /// Contains the dependency property for the alternation count of <see cref="ItemsSection"/>.
        /// </summary>
        public static readonly DependencyProperty AlternationCountProperty = DependencyProperty.Register("AlternationCount", typeof(int?), typeof(ItemsSection), new PropertyMetadata(null, (sender, e) => (sender as ItemsSection)?.UpdateContent()));

        /// <summary>
        /// Gets or sets the alternation count of the <see cref="ItemsSection"/>.
        /// </summary>
        public int? AlternationCount
        {
            get
            {
                return (int?)this.GetValue(ItemsSection.AlternationCountProperty);
            }

            set
            {
                this.SetValue(ItemsSection.AlternationCountProperty, value);
            }
        }

        #endregion

        #region Attached Properties

        /// <summary>
        /// Contains a read-only dependency property, which always contains the current alternation index.
        /// </summary>
        private static DependencyPropertyKey alternationIndexPropertyKey = DependencyProperty.RegisterAttachedReadOnly("AlternationIndex", typeof(int), typeof(Block), new PropertyMetadata(0));

        /// <summary>
        /// Contains the actual alternation index dependency property, which is available in XAML.
        /// </summary>
        public static DependencyProperty AlternationIndexProperty = ItemsSection.alternationIndexPropertyKey.DependencyProperty;

        #endregion

        #region Private Methods

        /// <summary>
        /// Updates the content of the section.
        /// </summary>
        private void UpdateContent()
        {
            // Clears all blocks
            this.Blocks.Clear();

            // Checks whether the items source and the template are provided
            if (this.ItemsSource == null || this.ItemTemplate == null)
                return;

            // Adds the blocks to the collection of blocks
            int i = 0;
            foreach (object item in this.ItemsSource)
            {
                // Adds the block
                Block block = this.ItemTemplate.LoadContent() as Block;
                block.SetValue(ItemsSection.alternationIndexPropertyKey, i);
                block.DataContext = item;
                this.Blocks.Add(block);

                // Increases the alternation counter
                i++;
                if (this.AlternationCount.HasValue)
                    i = i % this.AlternationCount.Value;
            }
        }

        #endregion
    }
}