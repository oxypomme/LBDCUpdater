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
    public partial class InstallationInfo : UserControl
    {
        private bool clickable;

        public InstallationInfo()
        {
            InitializeComponent();
            Click = null;
            Mode = DisplayMode.NONE;
            clickable = false;
        }

        public enum DisplayMode
        {
            NONE,
            INFO,
            VALIDATE,
            WARNING,
            ERROR
        }

        public Action? Click { get; set; }

        public bool Clickable
        {
            get => clickable;
            set => Cursor = (clickable = value) ? Cursors.Hand : Cursors.Arrow;
        }

        public DisplayMode Mode
        {
            set
            {
                Visibility = Visibility.Visible;
                Info.Visibility = Visibility.Collapsed;
                Validate.Visibility = Visibility.Collapsed;
                Warning.Visibility = Visibility.Collapsed;
                Error.Visibility = Visibility.Collapsed;
                switch (value)
                {
                    case DisplayMode.INFO:
                        Info.Visibility = Visibility.Visible;
                        break;

                    case DisplayMode.VALIDATE:
                        Validate.Visibility = Visibility.Visible;
                        break;

                    case DisplayMode.WARNING:
                        Warning.Visibility = Visibility.Visible;
                        break;

                    case DisplayMode.ERROR:
                        Error.Visibility = Visibility.Visible;
                        break;

                    default:
                        Visibility = Visibility.Collapsed;
                        break;
                }
            }
        }

        public string Text { get => display.Text; set => display.Text = value; }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (clickable)
                Click?.Invoke();
        }
    }
}