
#region Using Directives

using System;

#endregion

namespace System.Windows.Documents.Reporting
{
    /// <summary>
    /// Represents an attribute which is used to specify the view model of a view.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ViewModelAttribute : Attribute
    {
        #region Constructors

        /// <summary>
        /// Initializes a new <see cref="ViewModelAttribute"/> instance.
        /// </summary>
        /// <param name="viewModelType">The type of the view model for the view.</param>
        public ViewModelAttribute(Type viewModelType)
        {
            this.ViewModelType = viewModelType;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the type of the view model for the view.
        /// </summary>
        public Type ViewModelType { get; private set; }

        #endregion
    }
}
