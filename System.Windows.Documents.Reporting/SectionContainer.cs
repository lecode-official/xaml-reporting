
#region Using Directives

using System.Collections.Generic;
using System.Linq;

#endregion

namespace System.Windows.Documents.Reporting
{
    /// <summary>
    /// Represents a section that can be collapsed.
    /// </summary>
    public class SectionContainer : Section
    {
        #region Private Fields

        /// <summary>
        /// Contains the storage for block that are hidden.
        /// </summary>
        private IEnumerable<Block> blocks;

        #endregion

        #region Public Properties

        /// <summary>
        /// Contains the dependency property for the visibility of the blocks of the <see cref="SectionContainer"/>.
        /// </summary>
        public static readonly DependencyProperty VisibilityProperty = DependencyProperty.Register("Visibility", typeof(Visibility), typeof(SectionContainer), new PropertyMetadata(Visibility.Visible, (sender, e) => (sender as SectionContainer)?.UpdateContent()));

        /// <summary>
        /// Gets or sets the template for the visibility of the blocks of the <see cref="SectionContainer"/>.
        /// </summary>
        public Visibility Visibility
        {
            get
            {
                return (Visibility)this.GetValue(SectionContainer.VisibilityProperty);
            }

            set
            {
                this.SetValue(SectionContainer.VisibilityProperty, value);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Updates the content of the section.
        /// </summary>
        private void UpdateContent()
        {
            // Checks the value of the visibility
            if (this.Visibility == Visibility.Visible && this.blocks != null)
            {
                this.Blocks.Clear();
                this.Blocks.AddRange(this.blocks);
                this.blocks = null;
                return;
            }

            // Checks the value of the visibility
            if (this.Visibility != Visibility.Visible && this.blocks == null)
            {
                this.blocks = this.Blocks.ToList();
                this.Blocks.Clear();
                return;
            }
        }

        #endregion
    }
}