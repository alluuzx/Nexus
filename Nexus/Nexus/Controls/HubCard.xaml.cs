using System;
using System.Windows;
using System.Windows.Controls;

namespace Nexus.Controls
{
    /// <summary>
    /// Interaction logic for HubCard.xaml
    /// </summary>
    public partial class HubCard : UserControl
    {
        public string Script = string.Empty;

        public HubCard()
        {
            InitializeComponent();
        }

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public static readonly DependencyProperty TitleProperty
            = DependencyProperty.Register(
                  "Title",
                  typeof(string),
                  typeof(HubCard)
              );

        public string Creator
        {
            get { return (string)GetValue(CreatorProperty); }
            set { SetValue(CreatorProperty, value); }
        }

        public static readonly DependencyProperty CreatorProperty
            = DependencyProperty.Register(
                  "Creator",
                  typeof(string),
                  typeof(HubCard)
              );

        public Uri Image
        {
            get { return (Uri)GetValue(ImageProperty); }
            set { SetValue(ImageProperty, value); }
        }

        public static readonly DependencyProperty ImageProperty
            = DependencyProperty.Register(
                  "Image",
                  typeof(Uri),
                  typeof(HubCard)
              );

        public int CreatorFontSize
        {
            get { return (int)GetValue(CreatorFontSizeProperty); }
            set { SetValue(CreatorFontSizeProperty, value); }
        }

        public static readonly DependencyProperty CreatorFontSizeProperty
            = DependencyProperty.Register(
                  "CreatorFontSize",
                  typeof(int),
                  typeof(HubCard)
              );

        public string EditName
        {
            get { return (string)GetValue(EditNameProperty); }
            set { SetValue(EditNameProperty, value); }
        }

        public static readonly DependencyProperty EditNameProperty
            = DependencyProperty.Register(
                  "EditName",
                  typeof(string),
                  typeof(HubCard)
              );
    }
}