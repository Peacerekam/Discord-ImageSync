using System;
using System.Collections.Generic;
using System.IO;
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
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Net;
using System.ComponentModel;
using ImageFolderSync.DiscordClasses;
using ImageFolderSync.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.Loader;
using System.Collections.ObjectModel;
using System.Windows.Interop;

namespace ImageFolderSync
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ConfigData config;
        public ChannelConfig chConfig;

        public Boolean cancelSync = false;

        public static readonly DependencyProperty id = DependencyProperty.Register("Id", typeof(string), typeof(ListBoxItem));

        // later make a listbox with this, so you can add or remove extensions
        public string[] mediaExt = { "png", "gif", "jpg", "jpeg", "mp4", "webm", "webp" };

        public ObservableCollection<ImageTextPair> GuildsComboBox { get; set; }

        public MainWindow()
        {
            this.DataContext = this;
            GuildsComboBox = new ObservableCollection<ImageTextPair>();

            HandleConfig();

            InitializeComponent();

            // performance be damned, i just hate the weird refreshrate
            CompositionTarget.Rendering += Update;

            UpdateFolderList();

            Whomst();

        }

        private void Update(object sender, EventArgs e)
        {
            // made some dummy basically invis element that gets refreshed "as fast as possible" - should be monitor's refresh rate
            this._invisElement.IsEnabled = !_invisElement.IsEnabled;
        }

        public async void Whomst()
        {
            try
            {
                DiscordAPI d = new DiscordAPI();
                JToken res = await d.GetSelfAsync(config.Token);

                _windowMessage.Text = "Currently logged in as:   " + res["username"];
                this._loadServersButton.IsEnabled = true;
            }
            catch
            {
                _windowMessage.Text = "Invalid token! Scan for tokens or update config.json & restart app";
            }
        }

        public void SelectAccount(object sender, RoutedEventArgs e)
        {
            if (!(_accList.SelectedItem is ListBoxItem accountItem))
            {
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
                int howMuch = (int)(whole.Length * 0.8);
                string chunk = whole.Substring(howMuch, whole.Length - howMuch - 2);
                int aa = chunk.IndexOf("oken");
                int bb = chunk.Length - 1;

                //MessageBox.Show("aa: " + aa + "\nbb: " + bb);

                if (aa == -1)
                {
                    //MessageBox.Show("aa wynosi -1 wiec chuj");
                    continue;
                }

                string part = chunk.Substring(aa, bb - aa);
                string token = part.Split("\"")[1];

                //MessageBox.Show("token: " + token);

                try
                {
                    DiscordAPI d = new DiscordAPI();
                    JToken res = await d.GetSelfAsync(token);

                    //MessageBox.Show("User: " + res["username"]);

                    ListBoxItem item = new ListBoxItem();
                    item.Content = res["username"];
                    item.Selected += OnFolderSelected;
                    item.SetValue(id, token);
                    item.Height = 35;

                    listbox.Items.Add(item);
                }
                catch
                {
                    MessageBox.Show("Found invalid Token: " + token );
                }

            }
        }

        private void HandleConfig()
        {
            if (!File.Exists("config.json"))
            {

                ConfigData newConfig = new ConfigData()
                {
                    Token = "YOUR_TOKEN_GOES_HERE"
                };

                string json = JsonConvert.SerializeObject(newConfig, Formatting.Indented);
                byte[] fileContent = Encoding.UTF8.GetBytes(json);

                Atomic.WriteFile("config.json", new MemoryStream(fileContent));

                config = JsonConvert.DeserializeObject<ConfigData>(File.ReadAllText("config.json"));
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
                item.Content = $"{ch.Value.GuildName}\n{ch.Value.ChannelName}\n{ch.Value.SavePath}\nDownloaded Images: {ch.Value.ImagesSaved}";
                item.Selected += OnFolderSelected;
                item.Unselected += OnFolderUnselected;
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
                    ImagesSaved = 0
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
                    ImagesSaved = 0
                };
            }

            UpdateFolderList();

            string json = JsonConvert.SerializeObject(chConfig, Formatting.Indented);
            byte[] fileContent = Encoding.UTF8.GetBytes(json);

            Atomic.OverwriteFile("channels.json", new MemoryStream(fileContent), "channels.json.backup");


        }

        public Boolean IsDownloadable(string u)
        {

            for (int i = 0; i < mediaExt.Length; i++)
            {
                if (u.Contains($".{mediaExt[i]}")) // .png .gif .jpg   and so on
                {
                    return true;
                }
            }

            return false;
        }

        public void CancelSync(object sender, RoutedEventArgs e)
        {
            cancelSync = true;
        }

        public string RemoveInvalids(string p)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();

            if (p.IndexOfAny(invalidChars) > 0)
            {
                for (int i = 0; i < invalidChars.Length; i++)
                {
                    p = p.Replace(invalidChars[i], '-');
                }
            }

            return p;
        }

        public async void SyncPress(object sender, RoutedEventArgs e)
        {

            this._cancelSyncButton.IsEnabled = true;
            this._syncFolderButton.IsEnabled = false;
            this._loadServersButton.IsEnabled = false;
            this._browseFolderButton.IsEnabled = false;
            this._channelList.IsEnabled = false;
            this._serverList.IsEnabled = false;
            this._folderList.IsEnabled = false;

            string? channelID = (_folderList.SelectedItem is ListBoxItem folderItem) ? folderItem.GetValue(id).ToString() : null;
            if (channelID == null) return;

            ChannelConfig.Values thisConfig = chConfig.list[channelID];

            //DetectToken();

            if (!Directory.Exists(thisConfig.SavePath))
            {
                MessageBox.Show("Couldn't find the save path.");
                return;
            }

            WebClient wc = new WebClient();
            wc.UseDefaultCredentials = true;


            DiscordAPI d = new DiscordAPI();
            var messages = d.GetMessagesAsync(config.Token, thisConfig.ChannelId, null, thisConfig.LastMsgChecked); // this._token.Text

            //var messages = d.GetMessagesAsync(config.Token, thisConfig.ChannelId, null, thisConfig.LastMsgChecked); // this._token.Text

            int msgCount = 0;
            int mediaCount = 0;
            int failCount = 0;

            try 
            {
                await foreach (var msg in messages)
                {
                    if (cancelSync)
                    {
                        MessageBox.Show($"Canceled / Paused");
                        this._cancelSyncButton.IsEnabled = false;
                        this._syncFolderButton.IsEnabled = false;
                        this._loadServersButton.IsEnabled = true;
                        this._browseFolderButton.IsEnabled = true;
                        this._channelList.IsEnabled = true;
                        this._serverList.IsEnabled = true;
                        this._folderList.IsEnabled = true;
                        this._folderList.UnselectAll();

                        cancelSync = false;
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

                            if (!IsDownloadable(baseUrl))
                            {
                                continue;
                            }


                            string[] splitFilename = baseUrl.Split("/")[^1].Split(".");
                            string extension = splitFilename[^1].Split(":")[0].Split("?")[0]; // last array element + anti twitter garbage + anti resize garbage
                            string filename = RemoveInvalids(string.Join(".", splitFilename.Take(splitFilename.Count() - 1)));
                            string dlPath = string.Format(@"{0}/{1}.{2}", thisConfig.SavePath, filename, extension);


                            int fileIndex = 0;

                            while (File.Exists(dlPath) && new FileInfo(dlPath).Length > 0)
                            {
                                fileIndex++;
                                dlPath = string.Format(@"{0}/{1}_duplicate_{3}.{2}", thisConfig.SavePath, filename, extension, fileIndex);
                            }

                            if (msgCount == 1 && thisConfig.LastMsgChecked != null && fileIndex > 0) continue;
                            // special case if its duplicate on first dl message, should happen only when attempting to do already synced channel
                            // since the download is Atomic and last msg id is updated AFTER download, it means we have the image from last msg id
                            // (UNLESS ITS OUT FIRST TIME - LastMsgChecked null check)

                            try
                            {

                                baseUrl = baseUrl.Split("?")[0].Split("%3A")[0]; // anti resize garbage

                                await Atomic.DownloadFile(wc, dlPath, baseUrl);
                                mediaCount++;

                                chConfig.UpdateLastMessage(channelID, msg.Id, 1);

                                string json = JsonConvert.SerializeObject(chConfig, Formatting.Indented);
                                byte[] fileContent = Encoding.UTF8.GetBytes(json);

                                Atomic.OverwriteFile("channels.json", new MemoryStream(fileContent), "channels.json.backup");
                                UpdateFolderList();
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

                                    string json = JsonConvert.SerializeObject(chConfig, Formatting.Indented);
                                    byte[] fileContent = Encoding.UTF8.GetBytes(json);

                                    Atomic.OverwriteFile("channels.json", new MemoryStream(fileContent), "channels.json.backup");
                                    UpdateFolderList();
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

                            string dlPath = string.Format(@"{0}/{1}", thisConfig.SavePath, RemoveInvalids(msg.Attachments[i].FileName));

                            string baseUrl = msg.Attachments[i].Url;

                            if (!IsDownloadable(baseUrl))
                            {
                                continue;
                            }

                            int fileIndex = 0;

                            while (File.Exists(dlPath))
                            {
                                fileIndex++;
                                string[] splitFilename = msg.Attachments[i].FileName.Split(".");
                                string extension = "." + splitFilename[^1]; // last array element
                                string filename = RemoveInvalids(msg.Attachments[i].FileName.Replace(extension, ""));

                                dlPath = string.Format(@"{0}/{1}_{3}{2}", thisConfig.SavePath, filename, extension, fileIndex);
                            }

                            try
                            {
                                baseUrl = baseUrl.Split("?")[0].Split("%3A")[0];

                                await Atomic.DownloadFile(wc, dlPath, baseUrl);
                                mediaCount++;

                                chConfig.UpdateLastMessage(channelID, msg.Id, 1);

                                string json = JsonConvert.SerializeObject(chConfig, Formatting.Indented);
                                byte[] fileContent = Encoding.UTF8.GetBytes(json);

                                Atomic.OverwriteFile("channels.json", new MemoryStream(fileContent), "channels.json.backup");
                                UpdateFolderList();

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

            this._cancelSyncButton.IsEnabled = false;
            //this._syncFolderButton.IsEnabled = true; // that one is bad, cause when youre finished, you dont have any folder selected
            this._loadServersButton.IsEnabled = true;
            this._browseFolderButton.IsEnabled = true;
            this._channelList.IsEnabled = true;
            this._serverList.IsEnabled = true;
            this._folderList.IsEnabled = true;
            this._folderList.UnselectAll();

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
            var guilds = d.GetUserGuildsAsync(config.Token); // this._token.Text

            ListBox listbox = this._serverList;
            listbox.Items.Clear();
            //listbox.IsEnabled = false;

            try 
            {
                await foreach (var guild in guilds)
                {
                    ListBoxItem item = new ListBoxItem();
                    item.Content = guild.Name;
                    item.SetValue(id, guild.Id);
                    item.Selected += OnServerSelected;

                    ImageTextPair itp = new ImageTextPair{
                        Image = new BitmapImage( new Uri(guild.IconUrl) ),
                        Text = guild.Name
                    };

                    GuildsComboBox.Add(itp);
                    //GuildsComboBox = new ObservableCollection<ImageTextPair>(GuildsComboBox.OrderBy(i => i.Text));



                    listbox.Items.Add(item);
                    await Task.Delay(20); // not needed, but visualizing looks cool
                }

            } 
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }


            listbox.Items.SortDescriptions.Add(new SortDescription("Content", ListSortDirection.Ascending));
            //listbox.IsEnabled = true;

            //await Task.Delay(2000);
            this._loadServersButton.IsEnabled = true;
        }

        private void OnFolderSelected(object sender, RoutedEventArgs e)
        {
            this._syncFolderButton.IsEnabled = true;
        }
        
        private void OnFolderUnselected(object sender, RoutedEventArgs e)
        {
            if (!(_folderList.SelectedItem is ListBoxItem folder))
            {
                this._syncFolderButton.IsEnabled = false;
            }
        }

        private async void OnServerSelected(object sender, RoutedEventArgs e)
        {
            ListBoxItem lbi = e.Source as ListBoxItem;

            //MessageBox.Show(lbi.Content.ToString().Split(" | ")[0]);

            IReadOnlyList<Channel> channels;

            try
            {
                DiscordAPI d = new DiscordAPI();
                channels = await d.GetGuildChannelsAsync(config.Token, lbi.GetValue(id).ToString()); // this._token.Text
            }
            catch
            {
                MessageBox.Show("Something went wrong (token?)");
                return;
            }


            ListBox listbox = this._channelList;
            listbox.Items.Clear();

            // order by .ParentId, aka by category
            var channelList = channels.ToList().OrderBy(x => x.ParentId).ToList();

            foreach (var ch in channelList)
            {
                if (ch.Type != ChannelType.GuildTextChat)
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
