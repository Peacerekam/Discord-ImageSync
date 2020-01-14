using Newtonsoft.Json.Linq;
using ImageFolderSync.DiscordClasses;
using System;
using System.Linq;
using ImageFolderSync.Helpers;

namespace ImageFolderSync
{
    public partial class DiscordAPI
    {
        private User ParseUser(JToken json)
        {
            var id = json["id"]!.Value<string>();
            var discriminator = json["discriminator"]!.Value<int>();
            var name = json["username"]!.Value<string>();
            var avatarHash = json["avatar"]!.Value<string>();
            var isBot = json["bot"]?.Value<bool>() ?? false;

            return new User(id, discriminator, name, avatarHash, isBot);
        }

        private Attachment ParseAttachment(JToken json)
        {
            var id = json["id"]!.Value<string>();
            var url = json["url"]!.Value<string>();
            var width = json["width"]?.Value<int>();
            var height = json["height"]?.Value<int>();
            var fileName = json["filename"]!.Value<string>();

            //var fileSizeBytes = json["size"]!.Value<long>();
            //var fileSize = new FileSize(fileSizeBytes);

            return new Attachment(id, width, height, url, fileName);
        }

        private EmbedImage ParseEmbedImage(JToken json)
        {
            var url = json["url"]?.Value<string>();
            var proxuUrl = json["proxy_url"]?.Value<string>();
            var width = json["width"]?.Value<int>();
            var height = json["height"]?.Value<int>();

            return new EmbedImage(url, proxuUrl, width, height);
        }

        private Embed ParseEmbed(JToken json)
        {
            // Get basic data
            var url = json["url"]?.Value<string>();
            var timestamp = json["timestamp"]?.Value<DateTime>().ToDateTimeOffset();

            // Get thumbnail
            var thumbnail = json["thumbnail"] != null ? ParseEmbedImage(json["thumbnail"]!) : null;

            // Get image
            var image = json["image"] != null ? ParseEmbedImage(json["image"]!) : null;

            return new Embed(url, timestamp, thumbnail, image);
        }

        private Message ParseMessage(JToken json)
        {
            // Get basic data
            var id = json["id"]!.Value<string>();
            var channelId = json["channel_id"]!.Value<string>();
            var timestamp = json["timestamp"]!.Value<DateTime>().ToDateTimeOffset();
            var editedTimestamp = json["edited_timestamp"]?.Value<DateTime?>()?.ToDateTimeOffset();
            var content = json["content"]!.Value<string>();
            var type = (MessageType)json["type"]!.Value<int>();

            // Workarounds for non-default types
            if (type == MessageType.RecipientAdd)
                content = "Added a recipient.";
            else if (type == MessageType.RecipientRemove)
                content = "Removed a recipient.";
            else if (type == MessageType.Call)
                content = "Started a call.";
            else if (type == MessageType.ChannelNameChange)
                content = "Changed the channel name.";
            else if (type == MessageType.ChannelIconChange)
                content = "Changed the channel icon.";
            else if (type == MessageType.ChannelPinnedMessage)
                content = "Pinned a message.";
            else if (type == MessageType.GuildMemberJoin)
                content = "Joined the server.";

            // Get attachments
            var attachments = (json["attachments"] ?? Enumerable.Empty<JToken>()).Select(ParseAttachment).ToArray();

            // Get embeds
            var embeds = (json["embeds"] ?? Enumerable.Empty<JToken>()).Select(ParseEmbed).ToArray();

            return new Message(id, channelId, type, timestamp, editedTimestamp, content, attachments, embeds);
        }

        public Channel ParseChannel(JToken json)
        {
            // Get basic data
            var id = json["id"]!.Value<string>();
            var parentId = json["parent_id"]?.Value<string>();
            var type = (ChannelType)json["type"]!.Value<int>();

            // Try to extract guild ID
            var guildId = json["guild_id"]?.Value<string>();

            // If the guild ID is blank, it's direct messages
            //if (string.IsNullOrWhiteSpace(guildId))
            //    guildId = Guild.DirectMessages.Id;

            // Try to extract name
            var name = json["name"]?.Value<string>();

            // If the name is blank, it's direct messages
            //if (string.IsNullOrWhiteSpace(name))
            //    name = json["recipients"]?.Select(ParseUser).Select(u => u.Name).JoinToString(", ");

            // If the name is still blank for some reason, fallback to ID
            // (blind fix to https://github.com/Tyrrrz/DiscordChatExporter/issues/227)
            if (string.IsNullOrWhiteSpace(name))
                name = id;

            return new Channel(id, parentId, guildId, name, type);
        }

        public Guild ParseGuild(JToken json)
        {
            var id = json["id"]!.Value<string>();
            var name = json["name"]!.Value<string>();
            var iconHash = json["icon"]!.Value<string>();

            return new Guild(id, name, iconHash);
        }
    }
}
