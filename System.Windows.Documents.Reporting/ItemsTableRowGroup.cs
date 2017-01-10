
#region Using Directives

using System.Collections;
using System.IO;
using System.Windows.Markup;

#endregion

namespace System.Windows.Documents.Reporting
{
    /// <summary>
    /// Represents a table row group that created rows based on the elements in the items source.
    /// </summary>
    public class ItemsTableRowGroup : TableRowGroup
    {
        #region Public Properties

        /// <summary>
        /// Contains the dependency property for the items source of the <see cref="ItemsTableRowGroup"/>.
        /// </summary>
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register("ItemsSource", typeof(IEnumerable), typeof(ItemsTableRowGroup), new PropertyMetadata(null, (sender, e) => (sender as ItemsTableRowGroup)?.UpdateContent()));

        /// <summary>
        /// Gets or sets the items source of the <see cref="ItemsTableRowGroup"/>.
        /// </summary>
        public IEnumerable ItemsSource
        {
            get
            {
                return this.GetValue(ItemsTableRowGroup.ItemsSourceProperty) as IEnumerable;
            }

            set
            {
                this.SetValue(ItemsTableRowGroup.ItemsSourceProperty, value);
            }
        }

        /// <summary>
        /// Contains the dependency property for the template of the rows of the <see cref="ItemsTableRowGroup"/>.
        /// </summary>
        public static readonly DependencyProperty ItemTemplateProperty = DependencyProperty.Register("ItemTemplate", typeof(DataTemplate), typeof(ItemsTableRowGroup), new PropertyMetadata(null, (sender, e) => (sender as ItemsTableRowGroup)?.UpdateContent()));

        /// <summary>
        /// Gets or sets the template for the rows of the <see cref="ItemsTableRowGroup"/>.
        /// </summary>
        public DataTemplate ItemTemplate
        {
            get
            {
                return this.GetValue(ItemsTableRowGroup.ItemTemplateProperty) as DataTemplate;
            }

            set
            {
                this.SetValue(ItemsTableRowGroup.ItemTemplateProperty, value);
            }
        }

        /// <summary>
        /// Contains the dependency property for the alternation count of <see cref="ItemsTableRowGroup"/>.
        /// </summary>
        public static readonly DependencyProperty AlternationCountProperty = DependencyProperty.Register("AlternationCount", typeof(int?), typeof(ItemsTableRowGroup), new PropertyMetadata(null, (sender, e) => (sender as ItemsTableRowGroup)?.UpdateContent()));

        /// <summary>
        /// Gets or sets the alternation count of the <see cref="ItemsTableRowGroup"/>.
        /// </summary>
        public int? AlternationCount
        {
            get
            {
                return (int?)this.GetValue(ItemsTableRowGroup.AlternationCountProperty);
            }

            set
            {
                this.SetValue(ItemsTableRowGroup.AlternationCountProperty, value);
            }
        }

        #endregion

        #region Attached Properties

        /// <summary>
        /// Contains a read-only dependency property, which always contains the current alternation index. This can be used to render a background for table rows.
        /// </summary>
        private static DependencyPropertyKey alternationIndexPropertyKey = DependencyProperty.RegisterAttachedReadOnly("AlternationIndex", typeof(int), typeof(TableRow), new PropertyMetadata(0));

        /// <summary>
        /// Contains the actual alternation index dependency property, which is available in XAML.
        /// </summary>
        public static DependencyProperty AlternationIndexProperty = ItemsTableRowGroup.alternationIndexPropertyKey.DependencyProperty;

        #endregion

        #region Private Methods

        /// <summary>
        /// Updates the content of the table row group.
        /// </summary>
        private void UpdateContent()
        {
            // Clears all rows
            this.Rows.Clear();

            // Checks whether the items source and the template are provided
            if (this.ItemsSource == null || this.ItemTemplate == null)
                return;

            // Adds the table rows to the collection of rows
            int i = 0;
            foreach (object item in this.ItemsSource)
            {
                // Adds the table row
                TableRow tableRow = this.ItemTemplate.LoadContent() as TableRow;
                tableRow.SetValue(ItemsTableRowGroup.alternationIndexPropertyKey, i);
                tableRow.DataContext = item;
                this.Rows.Add(tableRow);

                // Increases the alternation counter
                i++;
                if (this.AlternationCount.HasValue)
                    i = i % this.AlternationCount.Value;
            }
        }

        #endregion
    }
}