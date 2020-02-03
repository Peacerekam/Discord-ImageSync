using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Collections.ObjectModel;
using ImageFolderSync.Helpers;
using ImageFolderSync.UI;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace ImageFolderSync
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow _instance;

        private CustomTaskbar myTray;
        public ChannelConfig chConfig;
        public ConfigData config;

        public MainWindow()
        {
            _instance = this;
            DataContext = this;
            myTray = new CustomTaskbar();
            GuildsComboBox = new ObservableCollection<ViewGuild>();
            AccountsComboBox = new ObservableCollection<ViewGuild>();

            InitializeComponent();
            HandleConfig();
            TryToken();

            chConfig = config.ChConfig;
            _deleteButton.Click += RemoveFolder;
            CompositionTarget.Rendering += ForceRefreshUI; // performance be damned, i just hate the weird refresh rate

            // what the fuck - if i dont do it with a delay it wont properly refresh at initial start of the app
            // if i do it with too small delay, it will properly refresh on initial but will erease icons on resize...
            // so... in the end it stays at 1000ms and i dont freaking care lol, i'll look into it one day...
            new Action(async () => { await Task.Delay(1000); UpdateFolderGrid(); })();
        }


        private void ForceRefreshUI(object sender, EventArgs e)
        {
            if (!_dragging)
            {
                // made some dummy invis element that gets refreshed "as fast as possible" - should be monitor's refresh rate
                this._invisElement.IsEnabled = !_invisElement.IsEnabled;
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

                ChannelConfig chConf = new ChannelConfig();

                newConfig.ChConfig = chConf;
                newConfig.LastHeight = (int)this.ActualHeight;
                newConfig.LastWidth = (int)this.ActualWidth;

                string json = JsonConvert.SerializeObject(newConfig, Formatting.Indented);
                byte[] fileContent = Encoding.UTF8.GetBytes(json);

                Atomic.WriteFile("config.json", new MemoryStream(fileContent));

                config = JsonConvert.DeserializeObject<ConfigData>(File.ReadAllText("config.json"));
            }
            else
            {
                // this is also loaded on sync channel button, in case you ever change your token while program is still running i guess?
                // but please dont change token while program is still running wtf u doin
                config = JsonConvert.DeserializeObject<ConfigData>(File.ReadAllText("config.json"));

                this.Height = config.LastHeight;
                this.Width = config.LastWidth;
            }

        }

    }
}
