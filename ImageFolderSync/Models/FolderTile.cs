using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace ImageFolderSync.Helpers
{
    public class FolderTile : StackPanel
    {
        public ChannelConfig.Values MyConfig;
        private Image Image;

        private BitmapImage syncIcon;
        private BitmapImage folderIcon;

        //DropShadowEffect highlightedEffect;
        //DropShadowEffect normalEffect;

        public FolderTile(ChannelConfig.Values data) 
        {

            MyConfig = data;

            folderIcon = new BitmapImage(new Uri(MyConfig.IconUrl));
            syncIcon = new BitmapImage(new Uri(@"pack://application:,,,/Images/icon.png"));
            EllipseGeometry clipGeometry = new EllipseGeometry(new Point(25, 25), 25, 25);

            /*
            normalEffect = new DropShadowEffect
            {
                ShadowDepth = 0,
                Opacity = 0.2,
                BlurRadius = 40,
                Color = (Color)ColorConverter.ConvertFromString(MyConfig.Color)
            };

            highlightedEffect = new DropShadowEffect
            {
                ShadowDepth = 0,
                Opacity = 0.8,
                BlurRadius = 40,
                Color = (Color)ColorConverter.ConvertFromString(MyConfig.Color)
            };
            */

            Image = new Image
            {
                Source = folderIcon,
                Width = 50,
                Height = 50,
                Margin = new Thickness(27, 15, 27, 5),
                //Effect = normalEffect,
                Clip = clipGeometry
            };

            string rawChannelName = MyConfig.ChannelName.Split(" > ")[^1];
            TextBlock tb = new TextBlock
            {
                Text = rawChannelName.Length > 17 ? rawChannelName.Substring(0,15) + "..." : rawChannelName,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 0, 0, 10)
                //Effect = highlightedEffect
            };

            this.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(MyConfig.Color));
            this.Background.Opacity = 0;
            this.HorizontalAlignment = HorizontalAlignment.Left;

            this.ToolTip = "2x LMB : Start sync\n1x RMB : Open explorer";
            this.Width = 104;
            this.Cursor = Cursors.Hand;
            this.Children.Add(Image);
            this.Children.Add(tb);

            this.MouseEnter += OnMouseEnter;
            this.MouseDown += OnMouseDown;
            this.MouseUp += OnMouseUp;
            this.MouseLeave += OnMouseLeave;
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                //Image.Effect = highlightedEffect;

                if (Image.Source == syncIcon)
                {
                    MainWindow._instance.SyncPress(sender, e);
                    return;
                }

                Image.Source = syncIcon;
            }
            else if (e.RightButton == MouseButtonState.Pressed)
            {
                if (!Directory.Exists(MyConfig.SavePath))
                {
                    MessageBox.Show("Couldn't find the directory");
                }
                else
                {
                    Process.Start("explorer.exe", MyConfig.SavePath);
                }
            }
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            // ... nothing really?
        }

        private void OnMouseEnter(object sender, MouseEventArgs e)
        {
            // testing some stuff
            //MainWindow._instance._folderDetails.Text = $"{MyConfig.SavePath} > {MyConfig.ImagesSaved}";
            //MainWindow._instance._folderDetails.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(MyConfig.Color));

            this.Background.Opacity = 0.5;
            //Image.Effect = highlightedEffect;
        }

        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            this.Background.Opacity = 0;
            //Image.Effect = normalEffect;

            if (Image.Source == syncIcon) Image.Source = folderIcon;
            
        }
    }
}
