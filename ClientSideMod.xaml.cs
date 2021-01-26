using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LBDCUpdater
{
    /// <summary>
    /// Logique d'interaction pour ClientSideMod.xaml
    /// </summary>
    public partial class ClientSideMod : UserControl
    {
        public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register("Description", typeof(string), typeof(ClientSideMod));

        public static readonly DependencyProperty IconProperty = DependencyProperty.Register("Icon", typeof(ImageSource), typeof(ClientSideMod));

        public static readonly DependencyProperty IsCheckedProperty = DependencyProperty.Register("IsChecked", typeof(bool), typeof(ClientSideMod));

        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register("Title", typeof(string), typeof(ClientSideMod));

        public ClientSideMod(Func<Task<ImageSource?>> loadImageAction)
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                App.LogStream.Log(new(ex.ToString(), LogSeverity.Critical, ex));
                throw;
            }
            this.loadImage = loadImageAction;
        }

        public string? Description
        {
            get => (string?)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        public ImageSource? Icon
        {
            get => (ImageSource?)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public bool IsChecked
        {
            get => (bool)GetValue(IsCheckedProperty);
            set => SetValue(IsCheckedProperty, value);
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        private Func<Task<ImageSource?>> loadImage { get; set; }

        private async void Checkbox_ToolTipOpening(object sender, ToolTipEventArgs e)
        {
            try
            {
                modImage.Source = await loadImage();
            }
            catch (Exception ex) { App.LogStream.Log(new(ex.ToString(), LogSeverity.Error, ex)); }
        }
    }
}