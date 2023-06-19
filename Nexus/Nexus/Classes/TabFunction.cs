using System.Windows.Controls;

namespace Nexus.Classes
{
    /// <summary>
    /// Extension to the TabControl to get the template item
    /// </summary>
    public static class TabFunction
    {
        public static T GetTemplateItem<T>(this Control elem, string name)
        {
            return elem.Template.FindName(name, elem) is T name1 ? name1 : default;
        }
    }
}