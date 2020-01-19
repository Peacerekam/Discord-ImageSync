using ImageFolderSync.Helpers;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace ImageFolderSync
{
    public partial class MainWindow
    {
        private Boolean _dragging = false;

        protected override void OnStateChanged(EventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
                this.MaximizeAppButton.Content = FindResource("demaxPic");

            if (this.WindowState == WindowState.Normal)
                this.MaximizeAppButton.Content = FindResource("maxPic");

            base.OnStateChanged(e);
        }

        private void HideTitleIfNeeded()
        {
            if (this.ActualWidth < 350)
            {
                _titleText.Visibility = Visibility.Collapsed;
            }
            else
            {
                _titleText.Visibility = Visibility.Visible;
            }
        }

        public void CloseApp(object sender, RoutedEventArgs e)
        {
            /*
            MessageBoxImage mbi = new MessageBoxImage();
            string desc = "Are you sure you want to exit?\nAll the ongoing downloading will pause. (check for downloading, then warn?)";

            // should be custom messagebox... because.
            MessageBoxResult result = MessageBox.Show(desc, "Close app", MessageBoxButton.YesNo, mbi, MessageBoxResult.Yes);

            if (result == MessageBoxResult.No)
            {
                return;
            }
            */

            config.LastWidth = (int)this.ActualWidth;
            config.LastHeight = (int)this.ActualHeight;

            string json = JsonConvert.SerializeObject(config, Formatting.Indented);
            byte[] fileContent = Encoding.UTF8.GetBytes(json);

            Atomic.OverwriteFile("config.json", new MemoryStream(fileContent), "config.json.backup");

            Application.Current.Shutdown();
        }

        public void MinimizeApp(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        public void MinimizeToTray(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow.Visibility = Visibility.Hidden;
        }

        public void MaximizeApp(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }
        }

        private void TitlebarMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                _dragging = true;
                this.DragMove();
            }

            if (e.ClickCount == 2)
            {
                if (WindowState == WindowState.Maximized)
                {
                    this.WindowState = WindowState.Normal;
                }
                else
                {
                    this.WindowState = WindowState.Maximized;
                }
            }
        }

        private void TitlebarMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                _dragging = false;
            }
        }

    }
}
