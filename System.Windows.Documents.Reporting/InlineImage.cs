
#region Using Directives

using System.Windows.Controls;
using System.Windows.Media;

#endregion

namespace System.Windows.Documents.Reporting
{
    /// <summary>
    /// Represents an inline flow element control element, which displays an image.
    /// </summary>
    public class InlineImage : InlineUIContainer
    {
        #region Public Properties

        /// <summary>
        /// Gets or sets the width of the image.
        /// </summary>
        public double Width
        {
            get
            {
                return (double)this.GetValue(InlineImage.WidthProperty);
            }

            set
            {
                this.SetValue(InlineImage.WidthProperty, value);
            }
        }

        /// <summary>
        /// Contains the width dependency property of the image.
        /// </summary>
        public static readonly DependencyProperty WidthProperty = DependencyProperty.Register("Width", typeof(double), typeof(InlineImage), new FrameworkPropertyMetadata(Double.NaN));
        
        /// <summary>
        /// Gets or sets the height of the image.
        /// </summary>
        public double Height
        {
            get
            {
                return (double)this.GetValue(InlineImage.HeightProperty);
            }

            set
            {
                this.SetValue(InlineImage.HeightProperty, value);
            }
        }

        /// <summary>
        /// Contains the height dependency property of the image.
        /// </summary>
        public static readonly DependencyProperty HeightProperty = DependencyProperty.Register("Height", typeof(double), typeof(InlineImage), new FrameworkPropertyMetadata(Double.NaN));
        
        /// <summary>
        /// Gets or sets the stretch behavior of the image.
        /// </summary>
        public Stretch Stretch
        {
            get
            {
                return (Stretch)this.GetValue(InlineImage.StretchProperty);
            }

            set
            {
                this.SetValue(InlineImage.StretchProperty, value);
            }
        }

        /// <summary>
        /// Contains the stretch behavior dependency property of the image.
        /// </summary>
        public static readonly DependencyProperty StretchProperty = DependencyProperty.Register("Stretch", typeof(Stretch), typeof(InlineImage), new FrameworkPropertyMetadata(Stretch.Uniform));
        
        /// <summary>
        /// Gets or sets the stretch direction of the image.
        /// </summary>
        public StretchDirection StretchDirection
        {
            get
            {
                return (StretchDirection)this.GetValue(InlineImage.StretchDirectionProperty);
            }

            set
            {
                this.SetValue(InlineImage.StretchDirectionProperty, value);
            }
        }

        /// <summary>
        /// Contains the stretch direction dependency property of the image.
        /// </summary>
        public static readonly DependencyProperty StretchDirectionProperty = DependencyProperty.Register("StretchDirection", typeof(StretchDirection), typeof(InlineImage), new FrameworkPropertyMetadata(StretchDirection.Both));
        
        /// <summary>
        /// Gets or sets the source URI of the image.
        /// </summary>
        public ImageSource Source
        {
            get
            {
                return (ImageSource)this.GetValue(InlineImage.SourceProperty);
            }

            set
            {
                this.SetValue(InlineImage.SourceProperty, value);
            }
        }

        /// <summary>
        /// Contains the source URI dependency property of the image.
        /// </summary>
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register("Source", typeof(ImageSource), typeof(InlineImage), new FrameworkPropertyMetadata(null, (sender, e) =>
        {
            // Converts the sender into the inline image, that triggered the event
            InlineImage inlineImage = sender as InlineImage;
            
            // Creates a new image
            Image image = new Image
            {
                Source = inlineImage.Source,
                Stretch = inlineImage.Stretch,
                StretchDirection = inlineImage.StretchDirection,
            };

            // Sets the width and height of the image
            if (!double.IsNaN(inlineImage.Width))
                image.Width = inlineImage.Width;
            if (!double.IsNaN(inlineImage.Height))
                image.Height = inlineImage.Height;

            // Sets the image as the only child of the inline UI container that the inline image derived from
            inlineImage.Child = image;
        }));
        
        #endregion
    }
}