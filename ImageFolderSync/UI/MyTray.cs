using Hardcodet.Wpf.TaskbarNotification;
using ImageFolderSync.Helpers;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using System.Windows.Resources;
using System.Xml;

namespace ImageFolderSync.UI
{
    public class MyTray
    {
        private TaskbarIcon tbi;

        public MyTray()
        {

            tbi = new TaskbarIcon();
            StreamResourceInfo iconResource = Application.GetResourceStream(new Uri(@"pack://application:,,,/sync.ico"));
            TrayToNormal ttn = new TrayToNormal();

            tbi.Icon = new System.Drawing.Icon(iconResource.Stream);
            tbi.ToolTipText = "Discord Image Sync";
            tbi.LeftClickCommand = ttn;
            tbi.DoubleClickCommand = ttn;

            ContextMenu context = new ContextMenu();


            // BOLDED TOP
            MenuItem item = new MenuItem
            {
                Header = "Open Discord Image Sync",
                FontWeight = FontWeights.Bold,
                Icon = new Image {
                    Source = new BitmapImage(new Uri(@"pack://application:,,,/sync.ico"))
                }
            };
            item.Click += ShowMainWindow;
            context.Items.Add(item);



            // SYNC COLLECTION
            item = new MenuItem
            {
                Header = "Sync...",
                FontWeight = FontWeights.Normal,
            };



            // SYNC COLLECTION >> FOLDERS
            ListBox folders = MainWindow._instance._folderList;

            for (int i = 0; i < folders.Items.Count; i++)
            {
                ListBoxItem lbi = folders.Items[i] as ListBoxItem;

                MenuItem subItem  = new MenuItem();
                subItem.Header = lbi.Content.ToString().Split("\n")[1].Split(" > ")[1];
                //subItem.SetValue(MainWindow._instance.idProperty, lbi.GetValue(MainWindow._instance.idProperty));
                subItem.Icon = new Image
                {
                    Source = new BitmapImage(new Uri(lbi.GetValue(MainWindow._instance.iconUrl).ToString()))
                };


                subItem.Click += StartSyncFromTray;
                subItem.CommandParameter = lbi.GetValue(MainWindow._instance.idProperty);
                item.Items.Add(subItem);


                //FoldersComboBox.Add(cbi);
            }
            context.Items.Add(item);



            // SYNC AUTOMATICALLY
            item = new MenuItem
            {
                Header = "Sync automtically",
                FontWeight = FontWeights.Normal,
                IsCheckable = true,
                StaysOpenOnClick = true
            };
            context.Items.Add(item);


            // EXIT
            item = new MenuItem
            {
                Header = "Exit",
                FontWeight = FontWeights.Normal
            };
            item.Click += MainWindow._instance.CloseApp;
            context.Items.Add(item);



            tbi.ContextMenu = context;

        }

        /*  
         *  for later use maybe?
         *  
        public Object Clone(Object o)
        {
            string xamlCode = XamlWriter.Save(o);
            return XamlReader.Load(new XmlTextReader(new StringReader(xamlCode)));
        }
        */

        private void StartSyncFromTray(object sender, RoutedEventArgs e)
        {
            // this whole thing needs a rework
            // this whole thing needs a rework
            // this whole thing needs a rework
            MenuItem menuItem = sender as MenuItem;

            /*
            string channelID = menuItem.GetValue(MainWindow._instance.idProperty).ToString();

            MessageBox.Show(menuItem.CommandParameter.ToString());

            ListBox folders = MainWindow._instance._folderList;

            for (int index = 0; index < folders.Items.Count; index++)
            {
                ListBoxItem fbi = folders.Items[index] as ListBoxItem;
                string iteratedChannelID = fbi.GetValue(MainWindow._instance.idProperty).ToString();

                if (channelID == iteratedChannelID)
                {
                    folders.SelectedIndex = index;
                    break;
                }
            }
            */
            
            MainWindow._instance.SyncPress(sender, e);
        }

        private void ShowMainWindow(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow.Show();
            Application.Current.MainWindow.WindowState = WindowState.Normal;
            Application.Current.MainWindow.Activate();
        }
    }

    struct TrayToNormal : ICommand
    {
        public void Execute(object parameter)
        {
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
