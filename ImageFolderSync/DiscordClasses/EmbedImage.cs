namespace ImageFolderSync.DiscordClasses
{
    public class EmbedImage
    {
        public string? Url { get; }

        public string? ProxyUrl { get; }

        public int? Width { get; }

        public int? Height { get; }

        public EmbedImage(string? url, string? proxyUrl, int? width, int? height)
        {
            Url = url;
            ProxyUrl = proxyUrl;
            Height = height;
            Width = width;
        }
    }
}