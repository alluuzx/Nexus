using System.Windows;

namespace Nexus.Controls.ScrollViewerSmooth
{
    /// <summary>
    /// Smooth scroll viewer control
    /// </summary>
    public class SmoothScrollViewer : System.Windows.Controls.ScrollViewer
    {
        public SmoothScrollViewer()
        {
            Loaded += ScrollViewer_Loaded;
        }

        private void ScrollViewer_Loaded(object sender, RoutedEventArgs e)
        {
            ScrollInfo = new ScrollInfoAdapter(ScrollInfo);
        }
    }
}