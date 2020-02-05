using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ImageFolderSync.Helpers
{
    public class FolderTile : StackPanel
    {
        public ChannelConfig.Values MyConfig;
        private Image Image;
        private TextBlock Counter;
        private Border CounterBg;

        private BitmapImage syncIcon;
        private BitmapImage folderIcon;

        //DropShadowEffect highlightedEffect;
        //DropShadowEffect normalEffect;

        public FolderTile(ChannelConfig.Values data) 
        {

            MyConfig = data;

            folderIcon = new BitmapImage(new Uri(MyConfig.IconUrl));
            syncIcon = new BitmapImage(new Uri(@"pack://application:,,,/Images/icon.png"));
            EllipseGeometry imageClip = new EllipseGeometry(new Point(25, 25), 25, 25);

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
                Clip = imageClip
            };

            Counter = new TextBlock
            {
                Text = "",
                FontSize = 13,
                Foreground = new SolidColorBrush(Colors.White),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };

            CounterBg = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 10, 25, 0),
                Background = new SolidColorBrush(Colors.Red),
                CornerRadius = new CornerRadius(4),
                Height = 18,
                Width = Counter.Text.Length == 0 ? 0 : (7 + Counter.Text.Length * 7)
            };

            CounterBg.Child = Counter;

            Grid imageGrid = new Grid();
            imageGrid.Children.Add(Image);
            imageGrid.Children.Add(CounterBg);

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
            this.ToolTip = $"2x LMB : Start sync\n1x RMB : Open {MyConfig.SavePath}";

            CheckForNewImages(false);

            this.Width = 104;
            this.Cursor = Cursors.Hand;
            this.Children.Add(imageGrid);
            this.Children.Add(tb);

            this.MouseEnter += OnMouseEnter;
            this.MouseDown += OnMouseDown;
            this.MouseUp += OnMouseUp;
            this.MouseLeave += OnMouseLeave;

        }

        public async void CheckForNewImages(bool forced = true)
        {
            int newMedia = 0;
            var dict = MainWindow._instance.Counters;

            if (forced == false) {

                //var myFt = dict.Keys.Single(tile => tile.MyConfig.ChannelId == MyConfig.ChannelId);

                if (!dict.ContainsKey(MyConfig.ChannelId))
                {
                    DiscordAPI d = new DiscordAPI();
                    newMedia = await d.SearchMediaInChannel(MainWindow._instance.config.Token, MainWindow._instance.chConfig.list[MyConfig.ChannelId] );

                    // check again cause async stuff
                    if (!dict.ContainsKey(MyConfig.ChannelId))
                    {
                        dict.Add(MyConfig.ChannelId, newMedia.ToString());
                    }
                }
                else
                {
                    newMedia = int.Parse(dict[MyConfig.ChannelId]);
                }
            }
            else
            {
                DiscordAPI d = new DiscordAPI();
                newMedia = await d.SearchMediaInChannel(MainWindow._instance.config.Token, MainWindow._instance.chConfig.list[MyConfig.ChannelId] );

                dict[MyConfig.ChannelId] = newMedia.ToString();
            }


            if (newMedia > 0)
            {
                string s = $"{newMedia}";

                Counter.Text = s;
                CounterBg.Width = s.Length == 0 ? 0 : (7 + s.Length * 7);

                this.ToolTip = $"Estimated around {newMedia} new files to download\n\n2x LMB : Start sync\n1x RMB : Open {MyConfig.SavePath}";
            }
            else
            {
                Counter.Text = "";
                CounterBg.Width = 0;

                this.ToolTip = $"This folder seems to be up to date\n\n2x LMB : Start sync\n1x RMB : Open {MyConfig.SavePath}";
            }

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

                PopRemoveButton();
                Image.Source = syncIcon;
            }
            else if (e.RightButton == MouseButtonState.Pressed)
            {
                // to-do:
                // on right change icon to "browsing" icon
                // also show a "delete" icon in the corner of the tile
                // i'd like to show "last downloaded timestamp" in here

                if (!Directory.Exists(MyConfig.SavePath))
                {
                    MessageBox.Show($"Couldn't find the directory\n{MyConfig.SavePath}");
                }
                else
                {
                    Process.Start("explorer.exe", MyConfig.SavePath);
                }
            }
        }

        private async void PopRemoveButton()
        {
            Button b = MainWindow._instance._deleteButton;
            TextBlock tb = MainWindow._instance._deleteButtonText;

            b.CommandParameter = MyConfig.ChannelId;
            b.Opacity = 1.75;
            tb.Text = $"remove #{MyConfig.ChannelName.Split(" > ")[^1]}?";

            if (b.Visibility == Visibility.Hidden)
            {

                b.Visibility = Visibility.Visible;

                while (b.Opacity > 0)
                {
                    b.Opacity = b.Opacity - 0.01;
                    await Task.Delay(10);
                }

                b.Visibility = Visibility.Hidden;
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

            //CheckForNewImages();

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
