namespace ImageFolderSync.DiscordClasses
{
    public partial class Channel 
    {
        public string Id { get; }

        public string? ParentId { get; }

        public string GuildId { get; }

        public string Name { get; }

        public ChannelType Type { get; }

        public Channel(string id, string? parentId, string guildId, string name, ChannelType type)
        {
            Id = id;
            ParentId = parentId;
            GuildId = guildId;
            Name = name;
            Type = type;
        }

        public override string ToString() => Name;
    }

    public partial class Channel
    {
        public static Channel CreateDeletedChannel(string id) =>
            new Channel(id, null, "unknown-guild", "deleted-channel", ChannelType.GuildTextChat);
    }
}