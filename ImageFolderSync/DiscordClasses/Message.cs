using System;
using System.Collections.Generic;

namespace ImageFolderSync.DiscordClasses
{
    // https://discordapp.com/developers/docs/resources/channel#message-object

    public class Message
    {
        public string Id { get; }

        public string ChannelId { get; }

        public MessageType Type { get; }

        public DateTimeOffset Timestamp { get; }

        public DateTimeOffset? EditedTimestamp { get; }

        public string? Content { get; }

        public IReadOnlyList<Attachment> Attachments { get; }

        public IReadOnlyList<Embed> Embeds { get; }

        public Message(string id, string channelId, MessageType type,
            DateTimeOffset timestamp, DateTimeOffset? editedTimestamp,
            string content, IReadOnlyList<Attachment> attachments, IReadOnlyList<Embed> embeds)
        {
            Id = id;
            ChannelId = channelId;
            Type = type;
            Timestamp = timestamp;
            EditedTimestamp = editedTimestamp;
            Content = content;
            Attachments = attachments;
            Embeds = embeds;
        }

        public override string ToString() => Content ?? "<message without content>";
    }
}