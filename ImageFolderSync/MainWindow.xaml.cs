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
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Net;
using System.ComponentModel;
using ImageFolderSync.DiscordClasses;
using System.IO;
using ImageFolderSync.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ImageFolderSync
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ConfigData config;
        public ChannelConfig chConfig;

        public static readonly DependencyProperty id = DependencyProperty.Register("Id", typeof(string), typeof(ListBoxItem));

        public MainWindow()
        {
            InitializeComponent();

            HandleConfig();

            UpdateFolderList();

            Whomst();
        }

        public async void Whomst()
        {
            try
            {
                DiscordAPI d = new DiscordAPI();
                JToken res = await d.GetSelfAsync(config.Token);

                _windowMessage.Text = "Currently logged in as:   " + res["username"];
            }
            catch
            {
                _windowMessage.Text = "Invalid token! Check config.json";
            }
        }

        public void SelectAccount(object sender, RoutedEventArgs e)
        {
            ListBoxItem? accountItem = _accList.SelectedItem as ListBoxItem;

            if (accountItem == null) {
                return;
            }

            string accToken = accountItem.GetValue(id).ToString();

            config.Token = accToken;

            string json = JsonConvert.SerializeObject(config, Formatting.Indented);
            byte[] fileContent = Encoding.UTF8.GetBytes(json);

            Atomic.OverwriteFile("config.json", new MemoryStream(fileContent), "config.json.backup");
            Whomst();

            this._serverList.Items.Clear();
            this._channelList.Items.Clear();
            this._syncFolderButton.IsEnabled = false;
        }

        public async void DetectToken(object sender, RoutedEventArgs e)
        {
            ListBox listbox = this._accList;
            listbox.Items.Clear();
            listbox.HorizontalContentAlignment = HorizontalAlignment.Center;

            string ss = Environment.GetEnvironmentVariable("APPDATA") + "\\Discord\\Local Storage\\leveldb";

            string[] files = Directory.GetFiles(ss, "*.ldb", SearchOption.AllDirectories);

            for (int i = 0; i < files.Length; i++)
            {
                string whole = File.ReadAllText(files[i]);
                int aa = whole.IndexOf("oken");
                int bb = whole.Length - 1;

                if (aa == -1) {
                    continue;
                }

                string part = whole.Substring(aa, bb - aa);
                string token = part.Split("\"")[1];

                try
                {
                    DiscordAPI d = new DiscordAPI();
                    //JToken res = await d.GetSelfAsync(token);

                    JToken res = await d.GetSelfAsync(token);

                    //MessageBox.Show("Found token for: " + token.Substring(0, 10) + "... for: " + res["username"]);

                    ListBoxItem item = new ListBoxItem();
                    item.Content = res["username"];
                    item.Selected += OnFolderSelected;
                    item.SetValue(id, token);
                    item.Height = 35;

                    listbox.Items.Add(item);
                }
                catch 
                {
                    //MessageBox.Show("Invalid Token: " + token.Substring(0, 15) + "(...)" );
                }
                
            }
        }

        private void HandleConfig()
        {
            if (!File.Exists("config.json"))
            {

                ConfigData newConfig = new ConfigData()
                {
                    Token = "YOUR TOKEN GOES HERE"
                };

                string json = JsonConvert.SerializeObject(newConfig, Formatting.Indented);
                byte[] fileContent = Encoding.UTF8.GetBytes(json);

                Atomic.WriteFile("config.json", new MemoryStream(fileContent));
            }
            else
            {
                // this is also loaded on sync channel button, in case you ever change your token while program is still running i guess
                config = JsonConvert.DeserializeObject<ConfigData>(File.ReadAllText("config.json"));
            }

            if (!File.Exists("channels.json"))
            {
                chConfig = new ChannelConfig();

                string json = JsonConvert.SerializeObject(chConfig, Formatting.Indented);
                byte[] fileContent = Encoding.UTF8.GetBytes(json);

                Atomic.WriteFile("channels.json", new MemoryStream(fileContent));
            }
            else
            {
                chConfig = JsonConvert.DeserializeObject<ChannelConfig>(File.ReadAllText("channels.json"));
            }
        }

        private void UpdateFolderList()
        {
            ListBox listbox = this._folderList;
            listbox.Items.Clear();

            foreach (var ch in chConfig.list)
            {
                ListBoxItem item = new ListBoxItem();
                item.Content = $"{ch.Value.GuildName}\n{ch.Value.ChannelName}\n{ch.Value.SavePath}\nStatus: ---";
                item.Selected += OnFolderSelected;
                item.SetValue(id, ch.Key);
                item.Height = 75;

                listbox.Items.Add(item);
            }

        }

        public void AddChannel(object sender, RoutedEventArgs e)
        {

            ListBoxItem serverItem = _serverList.SelectedItem as ListBoxItem;
            ListBoxItem channelItem = _channelList.SelectedItem as ListBoxItem;

            string channelID = channelItem.GetValue(id).ToString();

            chConfig = JsonConvert.DeserializeObject<ChannelConfig>(File.ReadAllText("channels.json"));

            if (!chConfig.list.ContainsKey(channelID))
            {
                chConfig.list.Add(channelID, new ChannelConfig.Values()
                {
                    GuildName = serverItem.Content.ToString(),
                    ChannelName = channelItem.Content.ToString(),
                    GuildId = serverItem.GetValue(id).ToString(),
                    ChannelId = channelID,
                    LastMsgChecked = null,
                    SavePath = this._path.Text,
                    //ImagesSaved = 0
                });
            }
            else
            {
                chConfig.list[channelID] = new ChannelConfig.Values()
                {
                    GuildName = serverItem.Content.ToString(),
                    ChannelName = channelItem.Content.ToString(),
                    GuildId = serverItem.GetValue(id).ToString(),
                    ChannelId = channelID,
                    LastMsgChecked = null,
                    SavePath = this._path.Text,
                    //ImagesSaved = 0
                };
            }

            UpdateFolderList();

            string json = JsonConvert.SerializeObject(chConfig, Formatting.Indented);
            byte[] fileContent = Encoding.UTF8.GetBytes(json);

            Atomic.OverwriteFile("channels.json", new MemoryStream(fileContent), "channels.json.backup");


        }

        public async void SyncFolder(object sender, RoutedEventArgs e)
        {
            this._syncFolderButton.IsEnabled = false;
            this._browseFolderButton.IsEnabled = false;
            this._channelList.IsEnabled = false;
            this._serverList.IsEnabled = false;
            this._loadServersButton.IsEnabled = false;

            ListBoxItem channelItem = _folderList.SelectedItem as ListBoxItem;
            string channelID = channelItem.GetValue(id).ToString();

            ChannelConfig.Values thisConfig = chConfig.list[channelID];

            //DetectToken();

            DiscordAPI d = new DiscordAPI();

            var messages = d.GetMessagesAsync(config.Token, thisConfig.ChannelId, null, thisConfig.LastMsgChecked); // this._token.Text

            WebClient wc = new WebClient();
            wc.UseDefaultCredentials = true;

            int msgCount = 0;
            int mediaCount = 0;
            int failCount = 0;

            await foreach (var msg in messages)
            {
                msgCount++;

                if (msg.Embeds.Count > 0)
                {

                    for (int i = 0; i < msg.Embeds.Count; i++)
                    {
                        if (msg.Embeds[i].Image == null) // && msg.Embeds[i].Thumbnail == null
                        {
                            continue;
                        }

                        //string[] splitFilename = msg.Embeds[i].Image.Url.Split("/")[^1].Split(".");
                        string[] splitFilename = msg.Embeds[i].Image.Url.Split("/")[^1].Split(".");
                        string extension = splitFilename[^1]; // last array element
                        string filename = splitFilename[^2];
                        string dlPath = string.Format(@"{0}/{1}.{2}", thisConfig.SavePath, filename, extension);

                        int index = 0;

                        while (File.Exists(dlPath))
                        {
                            index++;

                            dlPath = string.Format(@"{0}/{1}_{3}.{2}", thisConfig.SavePath, filename, extension, index);
                        }

                        try {
                            await wc.DownloadFileTaskAsync(new Uri(msg.Embeds[i].Image.Url), dlPath);
                            mediaCount++;
                        }
                        catch
                        {
                            try
                            {
                                await wc.DownloadFileTaskAsync(new Uri(msg.Embeds[i].Image.ProxyUrl.Split("?")[0]), dlPath);
                                mediaCount++;

                                chConfig.UpdateLastMessage(channelID, msg.Id);

                                string json = JsonConvert.SerializeObject(chConfig, Formatting.Indented);
                                byte[] fileContent = Encoding.UTF8.GetBytes(json);

                                Atomic.OverwriteFile("channels.json", new MemoryStream(fileContent), "channels.json.backup");
                            }
                            catch (Exception ex)
                            {
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

                        string dlPath = string.Format(@"{0}/{1}", thisConfig.SavePath, msg.Attachments[i].FileName);

                        int index = 0;

                        while (File.Exists(dlPath))
                        {
                            index++;
                            string[] splitFilename = msg.Attachments[i].FileName.Split(".");
                            string extension = "." + splitFilename[^1]; // last array element
                            string filename = msg.Attachments[i].FileName.Replace(extension, "");

                            dlPath = string.Format(@"{0}/{1}_{3}{2}", thisConfig.SavePath, filename, extension, index);
                        }

                        try
                        {
                            await wc.DownloadFileTaskAsync(new Uri(msg.Attachments[i].Url), dlPath);
                            mediaCount++;

                            chConfig.UpdateLastMessage(channelID, msg.Id);

                            string json = JsonConvert.SerializeObject(chConfig, Formatting.Indented);
                            byte[] fileContent = Encoding.UTF8.GetBytes(json);

                            Atomic.OverwriteFile("channels.json", new MemoryStream(fileContent), "channels.json.backup");

                        }
                        catch (Exception ex)
                        {
                            File.Delete(dlPath);
                            failCount++;
                        }
                    }
                }

            }

            MessageBox.Show($"Downloaded {mediaCount} media from {msgCount} messages.\nFailed to download: {failCount} (most likely dead gelbooru links)");
            this._syncFolderButton.IsEnabled = true;
            this._browseFolderButton.IsEnabled = true;
            this._channelList.IsEnabled = true;
            this._serverList.IsEnabled = true;
            this._loadServersButton.IsEnabled = true;
        }

        public void BrowseFolders(object sender, RoutedEventArgs e)
        {

            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            //dialog.InitialDirectory = "C:\\Users";
            dialog.IsFolderPicker = true;


            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                
                if (null != this._channelList.SelectedItem)
                {
                    this._addChannelButton.IsEnabled = true;
                }
                
                this._path.Text = dialog.FileName;
            }

        }

        public static void dlProgress(object o, DownloadProgressChangedEventArgs args)
        {
            //Console.WriteLine("Completed"); 
            //MessageBox.Show(args.BytesReceived.ToString());
        }

        public static void dlComplete(object o, AsyncCompletedEventArgs args)
        {
            //Console.WriteLine("Completed"); 
            //MessageBox.Show("Completed");
        }

        public async void LoadServers(object sender, RoutedEventArgs e)
        {
            //MessageBox.Show("Sync.");
            this._syncFolderButton.IsEnabled = false;
            this._loadServersButton.IsEnabled = false;
            this._channelList.Items.Clear();

            // read file into a string and deserialize JSON to a type
            config = JsonConvert.DeserializeObject<ConfigData>(File.ReadAllText("config.json"));

            DiscordAPI d = new DiscordAPI();

            //          await?
            var guilds = d.GetUserGuildsAsync(config.Token); // this._token.Text


            ListBox listbox = this._serverList;
            listbox.Items.Clear();
            listbox.IsEnabled = false;

            await foreach (var guild in guilds)
            {
                ListBoxItem item = new ListBoxItem();
                item.Content = guild.Name;
                item.SetValue(id, guild.Id);
                item.Selected += OnServerSelected;

                listbox.Items.Add(item);
                await Task.Delay(50);
            }

            listbox.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("Content", System.ComponentModel.ListSortDirection.Ascending));
            listbox.IsEnabled = true;

            await Task.Delay(2000);
            this._loadServersButton.IsEnabled = true;
        }

        private void OnFolderSelected(object sender, RoutedEventArgs e)
        {
            this._syncFolderButton.IsEnabled = true;
        }

        private async void OnServerSelected(object sender, RoutedEventArgs e)
        {
            ListBoxItem lbi = e.Source as ListBoxItem;
            
            //MessageBox.Show(lbi.Content.ToString().Split(" | ")[0]);

            DiscordAPI d = new DiscordAPI();
            var channels = await d.GetGuildChannelsAsync(config.Token, lbi.GetValue(id).ToString()); // this._token.Text

            ListBox listbox = this._channelList;
            listbox.Items.Clear();

            var channelList = channels.ToList().OrderBy(x => x.ParentId).ToList();

            foreach (var ch in channelList)
            {

                if (ch.Type != DiscordClasses.ChannelType.GuildTextChat)
                {
                    // skip, we list only text channels
                    continue;
                    //item.Visibility = Visibility.Collapsed;
                }

                ListBoxItem item = new ListBoxItem();

                string prefix = ch.ParentId != null ? channelList.Find(x => x.Id == ch.ParentId).Name + " > " : "";

                //item.Content = $"{ch.Id} | {parent} > {ch.Name}";
                item.Content = $"{prefix}{ch.Name}";
                item.Selected += OnChannelSelected;
                item.SetValue(id, ch.Id);

                listbox.Items.Add(item);

                // to jest syf i musze jakos wpasc na to dlaczego await nie dziala przy GetUserGuildsAsync, inaczej dupa chuj
                //await Task.Delay(50);
            }
        }
        private void OnChannelSelected(object sender, RoutedEventArgs e)
        {
            if (this._path.Text != "")
            {
                this._addChannelButton.IsEnabled = true;
            }
            
        }
    }
}
