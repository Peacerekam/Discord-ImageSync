using ImageFolderSync.DiscordClasses;
using ImageFolderSync.Helpers;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ImageFolderSync
{
    public partial class MainWindow
    {
        public DependencyProperty idProperty = DependencyProperty.Register("Id", typeof(string), typeof(ListBoxItem));
        public DependencyProperty guildId = DependencyProperty.Register("guildId", typeof(string), typeof(ListBoxItem));
        public DependencyProperty iconUrl = DependencyProperty.Register("iconUrl", typeof(string), typeof(ListBoxItem));

        public ObservableCollection<ViewGuild> GuildsComboBox { get; set; }

        public ObservableCollection<ViewGuild> AccountsComboBox { get; set; }

        // give me a break pls
        private Boolean _cancelFlag = false;

        // Buttons & Similar

        public async void LoadServers(object sender, RoutedEventArgs e)
        {
            // if list is empty and you hover over it, then load it, otherwise dont trigger loading
            if (sender is ComboBox && GuildsComboBox.Count != 0) return;
            if (_refreshServerList.IsEnabled == false) return;
            if (_username.Text == "") return;

            this._refreshServerList.IsEnabled = false;

            // read file into a string and deserialize JSON to a type
            config = JsonConvert.DeserializeObject<ConfigData>(File.ReadAllText("config.json"));

            DiscordAPI d = new DiscordAPI();
            var guilds = d.GetUserGuildsAsync(config.Token); // this._token.Text

            GuildsComboBox.Clear();
            this._channelComboBox.Items.Clear();
            this._addFolder.IsEnabled = false;

            try
            {
                await foreach (var guild in guilds)
                {
                    ViewGuild itp = new ViewGuild
                    {
                        Image = new BitmapImage(new Uri(guild.IconUrl)),
                        ImageUrl = guild.IconUrl,
                        Name = guild.Name.Length > 20 ? guild.Name.Substring(0, 20) + "..." : guild.Name,
                        FullName = guild.Name,
                        Id = guild.Id,
                        IsSelectable = true
                    };

                    GuildsComboBox.Add(itp);
                    GuildsComboBox = OrderViewGuild(GuildsComboBox);

                    // not needed, but visualizing looks cool
                    // await Task.Delay(1); 
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }


            await Task.Delay(3000); // dont spam this button 
            this._refreshServerList.IsEnabled = true;
        }

        private ObservableCollection<ViewGuild> OrderViewGuild(ObservableCollection<ViewGuild> orderThoseGroups)
        {
            ObservableCollection<ViewGuild> temp;
            temp = new ObservableCollection<ViewGuild>(orderThoseGroups.OrderBy(p => p.Name));
            orderThoseGroups.Clear();
            foreach (ViewGuild j in temp) orderThoseGroups.Add(j);
            return orderThoseGroups;
        }

        public void BrowseFolders(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                Button b = sender as Button;
                b.CommandParameter = dialog.FileName;
                AddFolder(sender, e);
            }
        }

        public void AddFolder(object sender, RoutedEventArgs e)
        {

            //ListBoxItem channelItem = _channelList.SelectedItem as ListBoxItem;
            //string channelID = channelItem.GetValue(idProperty).ToString();

            ViewGuild serverItem = _guildComboBox.SelectedItem as ViewGuild;
            ComboBoxItem channelItem = _channelComboBox.SelectedItem as ComboBoxItem;

            string path = ((Button)sender).CommandParameter.ToString();
            string channelID = channelItem.GetValue(idProperty).ToString();
            string channelName = channelItem.Content.ToString();

            //chConfig = JsonConvert.DeserializeObject<ChannelConfig>(File.ReadAllText("channels.json"));

            Random r = new Random();
            Color c = Color.FromRgb((Byte)r.Next(0, 256), (Byte)r.Next(0, 256), (Byte)r.Next(0, 256));


            string color = "#" + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");

            if (!chConfig.list.ContainsKey(channelID))
            {
                chConfig.list.Add(channelID, new ChannelConfig.Values()
                {
                    IconUrl = serverItem.ImageUrl,
                    GuildName = serverItem.Name,
                    ChannelName = channelName,
                    GuildId = serverItem.Id,
                    ChannelId = channelID,
                    LastMsgChecked = null,
                    SavePath = path,
                    ImagesSaved = 0,
                    Color = color
                });
            }
            else
            {
                MessageBoxImage mbi = new MessageBoxImage();
                string desc = $"This action will overwrite channel's save path, however the program will still download new images from where it left off.\nDo you wish to continue?";

                // should be custom messagebox... but meh
                MessageBoxResult result = MessageBox.Show(desc, "This channel already has a folder set-up", MessageBoxButton.YesNo, mbi, MessageBoxResult.Yes);

                if (result == MessageBoxResult.No) return;
                
                config.ChConfig = chConfig;

                chConfig.list[channelID] = new ChannelConfig.Values()
                {
                    IconUrl = serverItem.ImageUrl,
                    GuildName = serverItem.Name,
                    ChannelName = channelName,
                    GuildId = serverItem.Id,
                    ChannelId = channelID,
                    LastMsgChecked = chConfig.list[channelID].LastMsgChecked,
                    SavePath = path,
                    ImagesSaved = chConfig.list[channelID].ImagesSaved,
                    Color = color
                };
            
            }

            config.ChConfig = chConfig;

            UpdateFolderGrid(true);
            RefreshDropboxList();
            myTray.UpdateTrayFolders();

            string json = JsonConvert.SerializeObject(config, Formatting.Indented);
            byte[] fileContent = Encoding.UTF8.GetBytes(json);

            Atomic.OverwriteFile("config.json", new MemoryStream(fileContent), "config.json.backup");

        }

        public async void DetectTokens(object sender, RoutedEventArgs e)
        {

            // if list is empty and you hover over it, then load it, otherwise dont trigger loading
            if (sender is ComboBox && AccountsComboBox.Count != 0) return;

            AccountsComboBox.Clear();

            string tokenDir = Environment.GetEnvironmentVariable("APPDATA") + "\\Discord\\Local Storage\\leveldb";

            // i should be handling the case for database, BUT IM NOT aaa
            if (!Directory.Exists(tokenDir)) return;

            string[] files = Directory.GetFiles(tokenDir, "*.ldb", SearchOption.AllDirectories);

            // if for whatever reason you dont have *.ldb files in there
            if (files == null) return;

            try
            {
                DiscordAPI d = new DiscordAPI();
                JToken res = await d.GetSelfAsync(config.Token);

                ViewGuild itp = new ViewGuild
                {
                    Image = new BitmapImage(GetAvatarUri(res)),
                    Name = res["username"].ToString(),
                    Id = config.Token,
                    IsSelectable = true
                };

                AccountsComboBox.Add(itp);

            }
            catch
            {
                ViewGuild itp = new ViewGuild
                {
                    Image = new BitmapImage(new Uri(@"pack://application:,,,/sync.ico")),
                    Name = "INVALID-CONFIG-TOKEN",
                    Id = "INVALID-CONFIG-TOKEN",
                    IsSelectable = false
                };

                AccountsComboBox.Add(itp);
            }


            for (int i = 0; i < files.Length; i++)
            {
                foreach (string line in File.ReadLines(files[i]))
                {
                    foreach (Match match in Regex.Matches(line, @"[A-Za-z0-9_\./\\-]*") )
                    {
                        foreach (Capture capture in match.Captures)
                        {
                            // theoretically it should be (capture.Value.Length == 88 || capture.Value.Length == 59)
                            // but I don't trust it with the whole 2FA etc, so we will take more chances, doesnt matter for the user
                            if (capture.Value.Length < 95 && capture.Value.Length > 50)
                            {
                                // don't attempt to use this token if its already in the config
                                if (AccountsComboBox.Where(cb => cb.Id == capture.Value).Count() > 0) continue;

                                try
                                {
                                    DiscordAPI d = new DiscordAPI();
                                    JToken res = await d.GetSelfAsync(capture.Value);

                                    ViewGuild itp = new ViewGuild
                                    {
                                        Image = new BitmapImage(GetAvatarUri(res)),
                                        Name = res["username"].ToString(),
                                        Id = capture.Value,
                                        IsSelectable = true
                                    };

                                    AccountsComboBox.Add(itp);

                                }
                                catch
                                {
                                    // ...
                                }
                            }
                        }
                    }
                }

            }
        }

        
        // Dropdown Lists

        private void OnAccountSelected(object sender, RoutedEventArgs e)
        {
            if (!(_accountComboBox.SelectedItem is ViewGuild vg)) return;

            config.Token = ((ViewGuild)_accountComboBox.SelectedItem).Id;

            string json = JsonConvert.SerializeObject(config, Formatting.Indented);
            byte[] fileContent = Encoding.UTF8.GetBytes(json);

            Atomic.OverwriteFile("config.json", new MemoryStream(fileContent), "config.json.backup");

            _pfp.Source = vg.Image;
            _preUsername.Text = "Logged in as:";
            _username.Text = vg.Name;
        }

        private async void OnServerSelected(object sender, RoutedEventArgs e)
        {
            if (!(_guildComboBox.SelectedItem is ViewGuild vg)) return;

            this._addFolder.IsEnabled = false;

            IReadOnlyList<Channel> channels;

            try
            {
                string guildID = ((ViewGuild)_guildComboBox.SelectedItem).Id;

                DiscordAPI d = new DiscordAPI();
                channels = await d.GetGuildChannelsAsync(config.Token, guildID); // this._token.Text
            }
            catch
            {
                MessageBox.Show("Something went wrong (token?)");
                return;
            }

            ComboBox combobox = this._channelComboBox;
            combobox.Items.Clear();

            // order by .ParentId, aka by category
            List<Channel> channelList = channels.ToList().OrderBy(x => x.ParentId).ToList();

            foreach (var ch in channelList)
            {
                if (ch.Type != ChannelType.GuildTextChat)
                {
                    // skip, we list only text channels
                    continue;
                }

                ComboBoxItem item = new ComboBoxItem();
                ChannelConfig.Values _chCnf;

                chConfig.list.TryGetValue(ch.Id, out _chCnf);

                if (_chCnf.ChannelId == ch.Id)
                {
                    Color color = (Color)ColorConverter.ConvertFromString(_chCnf.Color);
                    item.BorderBrush = new SolidColorBrush(color);
                    item.Foreground = new SolidColorBrush(color);

                    //item.Background = new SolidColorBrush(color);
                    //item.IsEnabled = false;
                }

                item.BorderThickness = new Thickness(5, 0, 0, 0);

                string prefix = ch.ParentId != null ? channelList.Find(x => x.Id == ch.ParentId).Name + " > " : "";
                item.Content = $"{prefix}{ch.Name}";
                item.Selected += OnChannelSelected;
                item.SetValue(idProperty, ch.Id);

                combobox.Items.Add(item);
            }
        }

        private void OnChannelSelected(object sender, RoutedEventArgs e)
        {
            this._addFolder.IsEnabled = true;
        }


        // Misc.

        public async void TryToken()
        {
            try
            {
                DiscordAPI d = new DiscordAPI();
                JToken res = await d.GetSelfAsync(config.Token);

                _pfp.Source = new BitmapImage(GetAvatarUri(res));
                _preUsername.Text = "Logged in as:";
                _username.Text = res["username"].ToString();

                this._refreshServerList.IsEnabled = true;
            }
            catch
            {
                _accountComboBox.IsDropDownOpen = true;
                //_username.Text = "[Invalid token!]";
            }
        }

        public void RefreshDropboxList()
        {
            foreach (ComboBoxItem cb in _channelComboBox.Items)
            {
                ChannelConfig.Values _chCnf;

                chConfig.list.TryGetValue(cb.GetValue(idProperty).ToString(), out _chCnf);

                if (_chCnf.ChannelId == cb.GetValue(idProperty).ToString())
                {
                    Color color = (Color)ColorConverter.ConvertFromString(_chCnf.Color);
                    cb.BorderBrush = new SolidColorBrush(color);
                    cb.Foreground = new SolidColorBrush(color);
                }
                else {
                    cb.BorderBrush = new SolidColorBrush(Colors.Transparent);
                    cb.Foreground = new SolidColorBrush(Colors.Black);
                }

            }
        }

        private Uri GetAvatarUri(JToken jt)
        {
            string? av = (string?)jt["avatar"];

            if (av == null)
            {
                av = ((float)jt["discriminator"] % 5).ToString();
            }

            return new Uri(string.Format("https://cdn.discordapp.com/avatars/{0}/{1}.png", jt["id"], av));
        }

    }
}
