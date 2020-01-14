namespace ImageFolderSync.DiscordClasses
{
    public partial class Guild
    {
        public string Id { get; }

        public string Name { get; }

        public string? IconHash { get; }

        public string IconUrl { get; }

        public Guild(string id, string name, string? iconHash)
        {
            Id = id;
            Name = name;
            IconHash = iconHash;

            IconUrl = GetIconUrl(id, iconHash);
        }

        public override string ToString() => Name;
    }

    public partial class Guild
    {
        public static string GetIconUrl(string id, string? iconHash)
        {
            return !string.IsNullOrWhiteSpace(iconHash)
                ? $"https://cdn.discordapp.com/icons/{id}/{iconHash}.png"
                : "https://cdn.discordapp.com/embed/avatars/0.png";
        }
    }
}
