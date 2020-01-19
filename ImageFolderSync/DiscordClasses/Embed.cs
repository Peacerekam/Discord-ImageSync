using System;
using System.Collections.Generic;
using System.Drawing;

namespace ImageFolderSync.DiscordClasses
{
    // https://discordapp.com/developers/docs/resources/channel#embed-object

    public class Embed
    {
        public string? Url { get; }

        public DateTimeOffset? Timestamp { get; }

        public EmbedImage? Thumbnail { get; }

        public EmbedImage? Image { get; }

        public EmbedImage? Video { get; }

        public Embed(string? url, DateTimeOffset? timestamp, EmbedImage? thumbnail, EmbedImage? image, EmbedImage? video)
        {
            Url = url;
            Timestamp = timestamp;
            Thumbnail = thumbnail;
            Image = image;
            Video = video;
        }

    }
}