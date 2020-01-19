using ImageFolderSync.Helpers;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ImageFolderSync
{
    public partial class MainWindow
    {
        int _old;

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

            _folderStackPanel.Children.Clear();
            StackPanel row = NewFolderRow();

            int itemsCount = chConfig.list.Count;
            int index = 0;

            foreach (var ch in chConfig.list)
            {
                FolderTile ft = new FolderTile(ch.Value);
                row.Children.Add(ft);

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

        private StackPanel NewFolderRow()
        {
            StackPanel row = new StackPanel();
            //row.Margin = new Thickness(5, 5, 5, 0);
            row.Orientation = Orientation.Horizontal;

            return row;
        }
    }
}

