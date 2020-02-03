
using System;
using System.Collections.Generic;
using ImageFolderSync.Helpers;

namespace ImageFolderSync.Helpers
{
    public class ChannelConfig
    {

        public Dictionary<string, Values> list = new Dictionary<string, Values>();

        public ChannelConfig()
        {

        }

        public void UpdateLastMessage(string key, string? lastMsg, int pics) // DateTimeOffset lastMsg)
        {
            list[key] = new Values()
            {
                IconUrl = list[key].IconUrl,
                GuildName = list[key].GuildName,
                ChannelName = list[key].ChannelName,
                GuildId = list[key].GuildId,
                ChannelId = list[key].ChannelId,
                LastMsgChecked = lastMsg,
                SavePath = list[key].SavePath,
                ImagesSaved = list[key].ImagesSaved + pics,
                Color = list[key].Color
            };
        }

        public struct Values
        {
            public string IconUrl;
            public string GuildName;
            public string ChannelName;
            public string GuildId;
            public string ChannelId;
            public string? LastMsgChecked;
            public string SavePath;
            public string Color;
            public int ImagesSaved;
        }
    }
}
