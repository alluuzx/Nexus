using System.Windows;
using System.Windows.Controls;

namespace Nexus.Controls
{
    /// <summary>
    /// A wrap panel which can apply a margin to each child item.
    /// </summary>
    public class ItemMarginWrapPanel : WrapPanel
    {
        /// <summary>
        /// ItemMargin static DP.
        /// </summary>
        public static readonly DependencyProperty ItemMarginProperty =
            DependencyProperty.Register(
            "ItemMargin",
            typeof(Thickness),
            typeof(ItemMarginWrapPanel),
            new FrameworkPropertyMetadata(
                new Thickness(),
                FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>
        /// The margin that will be applied to each Item in the wrap panel.
        /// </summary>
        public Thickness ItemMargin
        {
            get
            {
                return (Thickness)GetValue(ItemMarginProperty);
            }
            set
            {
                SetValue(ItemMarginProperty, value);
            }
        }

        /// <summary>
        /// Overridden. Sets item margins before calling base implementation.
        /// </summary>
        /// <param name="constraint"></param>
        /// <returns></returns>
        protected override Size MeasureOverride(Size constraint)
        {
            RefreshItemMargin();

            return base.MeasureOverride(constraint);
        }

        /// <summary>
        /// Overridden. Sets item margins before calling base implementation.
        /// </summary>
        /// <param name="finalSize"></param>
        /// <returns></returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            RefreshItemMargin();

            return base.ArrangeOverride(finalSize);
        }

        /// <summary>
        /// Refresh the child item margins.
        /// </summary>
        private void RefreshItemMargin()
        {
            UIElementCollection children = InternalChildren;
            for (int i = 0, count = children.Count; i < count; i++)
            {
                if (children[i] is FrameworkElement ele)
                    ele.Margin = ItemMargin;
            }
        }
    }
}