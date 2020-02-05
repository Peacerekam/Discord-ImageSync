using Hardcodet.Wpf.TaskbarNotification;
using ImageFolderSync.Helpers;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace ImageFolderSync.UI
{
    public class CustomTaskbar
    {
        public TaskbarIcon tbi;

        private MenuItem titleItem;
        public MenuItem foldersListitem;
        private MenuItem syncAutoItem;
        private MenuItem exitItem;

        public CustomTaskbar()
        {
            tbi = new TaskbarIcon();
            TrayToNormal ttn = new TrayToNormal();

            tbi.IconSource = new BitmapImage(new Uri(@"pack://application:,,,/sync.ico"));
            tbi.ToolTipText = "Discord Image Sync";
            tbi.LeftClickCommand = ttn;
            tbi.DoubleClickCommand = ttn;

            ContextMenu context = new ContextMenu();
            tbi.ContextMenu = context;


            // BOLDED TOP
            titleItem = new MenuItem
            {
                Header = "Open Discord Image Sync",
                FontWeight = FontWeights.Bold,
                Icon = new Image {
                    Source = new BitmapImage(new Uri(@"pack://application:,,,/sync.ico"))
                }
            };
            titleItem.Click += ShowMainWindow;
            context.Items.Add(titleItem);


            // SYNC COLLECTION
            foldersListitem = new MenuItem
            {
                Header = "Sync...",
                FontWeight = FontWeights.Normal,
            };
            context.Items.Add(foldersListitem);

            // SYNC AUTOMATICALLY
            syncAutoItem = new MenuItem
            {
                Header = "Sync automtically [Placeholder]",
                FontWeight = FontWeights.Normal,
                IsCheckable = true,
                StaysOpenOnClick = true
            };
            context.Items.Add(syncAutoItem);


            // EXIT
            exitItem = new MenuItem
            {
                Header = "Exit",
                FontWeight = FontWeights.Normal
            };
            exitItem.Click += MainWindow._instance.CloseApp;
            context.Items.Add(exitItem);

        }

        public void UpdateTrayFolders()
        {
            // SYNC COLLECTION >> FOLDERS
            StackPanel rows = MainWindow._instance._folderStackPanel as StackPanel;

            int count = 0;

            for (int i = 0; i < rows.Children.Count; i++)
            {
                StackPanel folders = rows.Children[i] as StackPanel;

                for (int j = 0; j < folders.Children.Count; j++)
                {
                    count++;
                }
            }

            // update only if we counted a diferent amount of folders (aka something was added or removed)
            if (count != foldersListitem.Items.Count)
            {
                foldersListitem.Items.Clear();

                for (int i = 0; i < rows.Children.Count; i++)
                {
                    StackPanel folders = rows.Children[i] as StackPanel;

                    for (int j = 0; j < folders.Children.Count; j++)
                    {
                        FolderTile ft = folders.Children[j] as FolderTile;

                        MenuItem subItem = new MenuItem();
                        subItem.Header = ft.MyConfig.ChannelName.Split(" > ")[^1];

                        subItem.Icon = new Image
                        {
                            Source = new BitmapImage(new Uri(ft.MyConfig.IconUrl))
                        };

                        subItem.Click += StartSyncFromTray;
                        subItem.CommandParameter = ft.MyConfig.ChannelId;
                        foldersListitem.Items.Add(subItem);
                    }
                }
            }
        }

        public void EnableSyncButtons()
        {
            foldersListitem.Header = "Sync...";
            foldersListitem.IsEnabled = true;
        }

        private void StartSyncFromTray(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;

            //foldersListitem.Header = $"Syncing #{item.Header}...";
            //foldersListitem.IsEnabled = false;
            //MenuItem menuItem = sender as MenuItem;
            MainWindow._instance.SyncPress(sender, e);
        }

        private void ShowMainWindow(object sender, RoutedEventArgs e)
        {
            MainWindow._instance.UpdateNewImagesNotification();
            Application.Current.MainWindow.Show();
            Application.Current.MainWindow.WindowState = WindowState.Normal;
            Application.Current.MainWindow.Activate();
        }
    }

    struct TrayToNormal : ICommand
    {
        public void Execute(object parameter)
        {
            MainWindow._instance.UpdateNewImagesNotification();
            Application.Current.MainWindow.Show();
            Application.Current.MainWindow.WindowState = WindowState.Normal;
            Application.Current.MainWindow.Activate();
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;
    }
}
