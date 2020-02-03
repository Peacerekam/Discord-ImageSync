using ImageFolderSync.Helpers;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace ImageFolderSync
{
    public partial class MainWindow
    {
        private int _old;
        public Dictionary<string, string> Counters = new Dictionary<string, string>(); // to access tile's notification counter by its' channelID from anywhere
        public Dictionary<string, FolderTile> Tiles = new Dictionary<string, FolderTile>(); // to access tile's object  by its' channelID from anywhere

        private void WindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            HideTitleIfNeeded();

            // dont update it on the very first call
            if (_folderStackPanel.ActualWidth == 0) return;

            UpdateFolderGrid();
            myTray.UpdateTrayFolders();
        }

        private void UpdateFolderGrid(bool forceUpdate = false)
        {
            int itemsPerRow = (int)((float)_folderStackPanel.ActualWidth / 104);

            if (_old == itemsPerRow && !forceUpdate) return; // dont update if its not needed, duh.

            Tiles.Clear();
            _folderStackPanel.Children.Clear();
            StackPanel row = NewFolderRow();

            int itemsCount = chConfig.list.Count;
            int index = 0;

            foreach (var ch in chConfig.list)
            {
                FolderTile ft = new FolderTile(ch.Value);
                row.Children.Add(ft);
                Tiles.Add(ch.Value.ChannelId, ft);

                int col = index % itemsPerRow;

                if (col == (itemsPerRow - 1))
                {
                    _folderStackPanel.Children.Add(row);
                    row = NewFolderRow();
                }

                if (index == (itemsCount - 1))
                {
                    _folderStackPanel.Children.Add(row);
                }

                index++;
            }

            _old = itemsPerRow;
        }


        public void RemoveFolder(object sender, RoutedEventArgs e)
        {

            Button b = sender as Button;
            b.Opacity = 0.1;

            MessageBoxImage mbi = new MessageBoxImage();
            string desc = $"Do you wish to {_deleteButtonText.Text}";

            // should be custom messagebox... but meh
            MessageBoxResult result = MessageBox.Show(desc, "Remove from the list", MessageBoxButton.YesNo, mbi, MessageBoxResult.Yes);

            if (result == MessageBoxResult.Yes)
            {
                Tiles.Remove(b.CommandParameter.ToString());
                Counters.Remove(b.CommandParameter.ToString());
                chConfig.list.Remove(b.CommandParameter.ToString());
                config.ChConfig = chConfig;

                UpdateFolderGrid(true);
                RefreshDropboxList();
                myTray.UpdateTrayFolders();

                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                byte[] fileContent = Encoding.UTF8.GetBytes(json);

                Atomic.OverwriteFile("config.json", new MemoryStream(fileContent), "config.json.backup");
            }

        }

        private StackPanel NewFolderRow()
        {
            StackPanel row = new StackPanel();
            //row.Margin = new Thickness(5, 5, 5, 0);
            row.Orientation = Orientation.Horizontal;

            return row;
        }
    }
}

