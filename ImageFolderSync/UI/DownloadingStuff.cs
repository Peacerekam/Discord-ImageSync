using ImageFolderSync.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace ImageFolderSync
{
    public partial class MainWindow
    {
        public enum CustomWindowStates
        {
            Syncing,
            AfterSyncing
        }

        private void ChangeMainWindowState(CustomWindowStates s)
        {
            if (s == CustomWindowStates.Syncing)
            {
                // show the cancel button
                _cancelSyncButton.IsEnabled = true;
                _cancelSyncButton.Visibility = Visibility.Visible;

                // disable token related stuff just in case
                _accountComboBox.IsEnabled = false;
                _refreshServerList.IsEnabled = false;

                // disable folder grid navigation
                _folderStackPanel.IsEnabled = false;
                _folderStackPanel.Opacity = 0.5;
            }
            else if (s == CustomWindowStates.AfterSyncing)
            {
                // hide the cancel button
                _cancelSyncButton.IsEnabled = false;
                _cancelSyncButton.Visibility = Visibility.Hidden;

                // re-enable token related stuff
                _accountComboBox.IsEnabled = true;
                _refreshServerList.IsEnabled = true;

                // enable folder grid navigation again
                _folderStackPanel.IsEnabled = true;
                _folderStackPanel.Opacity = 1;

                // fix stuff that syncing process changed
                myTray.tbi.ToolTipText = "Discord Image Sync";
                _progressBar.Value = 0;
                _bgImage.Source = null;
                _cancelFlag = false;
                myTray.EnableSyncButtons();
            }
        }

        public async void SyncPress(object sender, RoutedEventArgs e)
        {
            ChangeMainWindowState(CustomWindowStates.Syncing);

            string channelID;

            if (sender is MenuItem mi)
            {
                channelID = mi.CommandParameter.ToString();
            }
            else if (sender is FolderTile ft)
            {
                channelID = ft.MyConfig.ChannelId;
                _bgImage.Source = new BitmapImage(new Uri(ft.MyConfig.IconUrl));
            }
            else
            {
                MessageBox.Show("Something went wrong");
                ChangeMainWindowState(CustomWindowStates.AfterSyncing);
                return;
            }

            ChannelConfig.Values thisConfig = chConfig.list[channelID];
            this._cancelSyncButton.Content = $"Syncing #{thisConfig.ChannelName.Split(" > ")[^1]}...\nClick here to pause/cancel";

            //DetectToken();

            if (!Directory.Exists(thisConfig.SavePath))
            {
                MessageBox.Show("Couldn't find the save path");
                ChangeMainWindowState(CustomWindowStates.AfterSyncing);
                return;
            }

            myTray.tbi.ToolTipText = "Syncing " + thisConfig.ChannelName;
            myTray.foldersListitem.Header = $"Syncing #{thisConfig.ChannelName.Split(" > ")[^1]}...";
            myTray.foldersListitem.IsEnabled = false;

            WebClient wc = new WebClient();
            wc.UseDefaultCredentials = true;
            wc.DownloadProgressChanged += DlProgress;
            wc.DownloadFileCompleted += DlComplete;


            DiscordAPI d = new DiscordAPI();
            var messages = d.GetMessagesAsync(config.Token, thisConfig.ChannelId, null, thisConfig.LastMsgChecked); // this._token.Text

            int msgCount = 0;
            int mediaCount = 0;
            int failCount = 0;

            try
            {
                await foreach (var msg in messages)
                {
                    if (_cancelFlag)
                    {
                        ChangeMainWindowState(CustomWindowStates.AfterSyncing);
                        return;
                    }

                    msgCount++;

                    if (msg.Embeds.Count > 0)
                    {

                        for (int i = 0; i < msg.Embeds.Count; i++)
                        {

                            string baseUrl = "";

                            if (msg.Embeds[i].Image != null)
                            {
                                baseUrl = msg.Embeds[i].Image.Url;
                            }
                            else if (msg.Embeds[i].Video != null)
                            {
                                baseUrl = msg.Embeds[i].Video.Url;
                            }
                            else if (msg.Embeds[i].Thumbnail != null)
                            {
                                // hard-coded anti-pixiv logo thing when posting r-18 stuff
                                if (msg.Embeds[i].Thumbnail.Url.Contains("pixiv_logo")) continue;

                                baseUrl = msg.Embeds[i].Thumbnail.Url;
                            }
                            else
                            {
                                continue;
                            }

                            if (!Stuff.IsDownloadable(baseUrl))
                            {
                                continue;
                            }

                            string[] splitFilename = baseUrl.Split("/")[^1].Split(".");
                            string extension = Stuff.RemoveInvalids(splitFilename[^1]);
                            string filename = Stuff.RemoveInvalids(string.Join(".", splitFilename.Take(splitFilename.Count() - 1)));
                            string dlPath = Stuff.CreateValidFilepath(thisConfig.SavePath, filename, extension); //string.Format(@"{0}/{1}.{2}", thisConfig.SavePath, filename, extension);

                            int fileIndex = 0;

                            while (File.Exists(dlPath) && new FileInfo(dlPath).Length > 0)
                            {
                                fileIndex++;
                                dlPath = Stuff.CreateValidFilepath(thisConfig.SavePath, filename, extension, "" + fileIndex);
                            }

                            if (msgCount == 1 && fileIndex > 0) continue;
                            // special case if its duplicate on first dl message, should happen only when attempting to do already synced channel
                            // since the download is Atomic and last msg id is updated AFTER download, it means we have the image from last msg id
                            // (UNLESS ITS OUT FIRST TIME - LastMsgChecked null check) // thisConfig.LastMsgChecked != null &&  ????

                            try
                            {

                                baseUrl = baseUrl.Split("?")[0].Split("%3A")[0]; // anti resize garbage

                                await Atomic.DownloadFile(wc, dlPath, baseUrl);
                                mediaCount++;

                                chConfig.UpdateLastMessage(channelID, msg.Id, 1);
                                config.ChConfig = chConfig;

                                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                                byte[] fileContent = Encoding.UTF8.GetBytes(json);


                                Atomic.OverwriteFile("config.json", new MemoryStream(fileContent), "config.json.backup");
                                ApplyValidBgImg(extension, dlPath, thisConfig.IconUrl);

                                //UpdateFolderGrid();

                            }
                            catch (Exception ex)
                            {
                                //_windowMessage.Text = "111: " + ex.Message;
                                try
                                {
                                    string proxyDlLink = "";

                                    if (msg.Embeds[i].Image != null)
                                    {
                                        proxyDlLink = msg.Embeds[i].Image.ProxyUrl.Split("?")[0].Split("%3A")[0];
                                    }
                                    else if (msg.Embeds[i].Video != null)
                                    {
                                        proxyDlLink = msg.Embeds[i].Video.ProxyUrl.Split("?")[0].Split("%3A")[0];
                                    }
                                    else if (msg.Embeds[i].Thumbnail != null)
                                    {
                                        proxyDlLink = msg.Embeds[i].Thumbnail.ProxyUrl.Split("?")[0].Split("%3A")[0];
                                    }
                                    else
                                    {
                                        continue;
                                    }

                                    await Atomic.DownloadFile(wc, dlPath, proxyDlLink);
                                    mediaCount++;

                                    chConfig.UpdateLastMessage(channelID, msg.Id, 1);
                                    config.ChConfig = chConfig;

                                    string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                                    byte[] fileContent = Encoding.UTF8.GetBytes(json);


                                    Atomic.OverwriteFile("config.json", new MemoryStream(fileContent), "config.json.backup");
                                    ApplyValidBgImg(extension, dlPath, thisConfig.IconUrl);

                                    //UpdateFolderGrid();

                                }
                                catch (Exception exc)
                                {
                                    //_windowMessage.Text = "222: " + exc.Message;
                                    File.Delete(dlPath);
                                    failCount++;
                                }
                            }
                        }

                    }

                    if (msg.Attachments.Count > 0)
                    {
                        for (int i = 0; i < msg.Attachments.Count; i++)
                        {

                            string dlPath = string.Format(@"{0}/{1}", thisConfig.SavePath, Stuff.RemoveInvalids(msg.Attachments[i].FileName));

                            string baseUrl = msg.Attachments[i].Url;

                            if (!Stuff.IsDownloadable(baseUrl))
                            {
                                continue;
                            }

                            int fileIndex = 0;

                            string[] splitFilename = msg.Attachments[i].FileName.Split(".");
                            string extension = Stuff.RemoveInvalids("." + splitFilename[^1]); // last array element

                            while (File.Exists(dlPath))
                            {
                                fileIndex++;
                                splitFilename = msg.Attachments[i].FileName.Split(".");
                                extension = Stuff.RemoveInvalids("." + splitFilename[^1]); // last array element
                                string filename = Stuff.RemoveInvalids(msg.Attachments[i].FileName.Replace(extension, ""));

                                dlPath = string.Format(@"{0}/{1}-duplicate-{3}{2}", thisConfig.SavePath, filename, extension, fileIndex);
                            }

                            if (msgCount == 1 && fileIndex > 0) continue;
                            // special case if its duplicate on first dl message, should happen only when attempting to do already synced channel
                            // since the download is Atomic and last msg id is updated AFTER download, it means we have the image from last msg id
                            // (UNLESS ITS OUT FIRST TIME - LastMsgChecked null check) // thisConfig.LastMsgChecked != null &&  ????

                            try
                            {
                                baseUrl = baseUrl.Split("?")[0].Split("%3A")[0];

                                await Atomic.DownloadFile(wc, dlPath, baseUrl);
                                mediaCount++;

                                chConfig.UpdateLastMessage(channelID, msg.Id, 1);
                                config.ChConfig = chConfig;

                                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                                byte[] fileContent = Encoding.UTF8.GetBytes(json);


                                Atomic.OverwriteFile("config.json", new MemoryStream(fileContent), "config.json.backup");
                                ApplyValidBgImg(extension, dlPath, thisConfig.IconUrl);

                                //UpdateFolderGrid();

                            }
                            catch (Exception ex)
                            {
                                File.Delete(dlPath);
                                failCount++;
                            }
                        }
                    }

                }

            }
            catch (Exception mainEx)
            {
                MessageBox.Show(mainEx.Message);
            }


            if (mediaCount == 0)
            {
                MessageBox.Show($"Couldn't find any new images to download\nThis means you're most likely up to date");
            }
            else
            {
                MessageBox.Show($"Downloaded {mediaCount} media from {msgCount} messages.\nFailed to download: {failCount} (e.g. dead gelbooru links)");
            }

            ChangeMainWindowState(CustomWindowStates.AfterSyncing);

        }

        private void ApplyValidBgImg(string ext, string sourcePath, string alter)
        {
            if (Application.Current.MainWindow.Visibility == Visibility.Hidden || this.WindowState == WindowState.Minimized)
            {
                // dont bother doing this if app is hidden or minimized
                // this probably helps performance in some way? it should, right?
                return;
            }

            string[] imageExts = { "png", "gif", "jpg", "jpeg" };


            /*  this obviously looks way better but extensions can be weird
             *  in theory i already take care of twitter "img.png:large" garbo and "img.png?height=..." stuff, but idk, ill see tomorrow

            if (imageExts.Contains(ext.ToLower()))
            {
                _bgImage.Source = new BitmapImage(new Uri(sourcePath));
            }
            else
            {
                _bgImage.Source = new BitmapImage(new Uri(alter));
            }

            */

            try
            {
                for (int i = 0; i < imageExts.Length; i++)
                {
                    if (ext.ToLower().Contains(imageExts[i]))
                    {
                        _bgImage.Source = new BitmapImage(new Uri(sourcePath));
                        return;
                    }
                }
                _bgImage.Source = new BitmapImage(new Uri(alter));
            } 
            catch (Exception ex)
            {
                // this whole try catch is a failsafe because very old gelbooru links look like proper direct image links
                // yet they lead to their regular site, this app will still download the file thats prob a .html and it would freak out
                // ill look into a better solution later
                _bgImage.Source = new BitmapImage(new Uri(alter));
            }

        }

        public void CancelSync(object sender, RoutedEventArgs e)
        {
            if (sender is Button b) b.Content = "Stopping the sync...";

            _cancelFlag = true;
        }

        public void DlProgress(object o, DownloadProgressChangedEventArgs args)
        {
            _progressBar.Value = args.ProgressPercentage;
        }

        public void DlComplete(object o, AsyncCompletedEventArgs args)
        {
            //_progressBar.Value = 0;
        }

    }
}
