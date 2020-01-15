using System;
using System.Collections.Generic;
using System.Drawing;

namespace ImageFolderSync.DiscordClasses
{
    // https://discordapp.com/developers/docs/resources/channel#embed-object

    public class Embed
    {
        //public string? Title { get; }

        public string? Url { get; }

        public DateTimeOffset? Timestamp { get; }

        //public Color? Color { get; }

        //public EmbedAuthor? Author { get; }

        //public string? Description { get; }

        //public IReadOnlyList<EmbedField> Fields { get; }

        public EmbedImage? Thumbnail { get; }

        public EmbedImage? Image { get; }

        public EmbedImage? Video { get; }

        //public EmbedFooter? Footer { get; }

        public Embed(string? url, DateTimeOffset? timestamp, EmbedImage? thumbnail, EmbedImage? image, EmbedImage? video)
        {
            //Title = title;
            Url = url;
            Timestamp = timestamp;
            Thumbnail = thumbnail;
            Image = image;
            Video = video;
        }

    }
}