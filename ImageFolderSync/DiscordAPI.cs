﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using Polly;
using ImageFolderSync.DiscordClasses;
using ImageFolderSync.Helpers;

namespace ImageFolderSync
{
    public partial class DiscordAPI : IDisposable
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private readonly IAsyncPolicy<HttpResponseMessage> _httpPolicy;

        public DiscordAPI()
        {
            _httpClient.BaseAddress = new Uri("https://discordapp.com/api/v6");

            // Discord seems to always respond 429 on our first request with unreasonable wait time (10+ minutes).
            // For that reason the policy will start respecting their retry-after header only after Nth failed response.
            _httpPolicy = Policy
                .HandleResult<HttpResponseMessage>(m => m.StatusCode == HttpStatusCode.TooManyRequests)
                .OrResult(m => m.StatusCode >= HttpStatusCode.InternalServerError)
                .WaitAndRetryAsync(6,
                    (i, result, ctx) =>
                    {
                        if (i <= 3)
                            return TimeSpan.FromSeconds(1 * i);

                        if (i <= 5)
                            return TimeSpan.FromSeconds(5 * i);

                        return result.Result.Headers.RetryAfter.Delta ?? TimeSpan.FromSeconds(10 * i);
                    },
                    (response, timespan, retryCount, context) => Task.CompletedTask);
        }

        private async Task<JToken> GetApiResponseAsync(string token, string route)
        {
            using var response = await _httpPolicy.ExecuteAsync(async () =>
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, route);

                request.Headers.Authorization = new AuthenticationHeaderValue(token);

                return await _httpClient.SendAsync(request);
            });

            // We throw our own exception here because default one doesn't have status code
            if (!response.IsSuccessStatusCode)
                throw new Exception(response.StatusCode + " - " + response.ReasonPhrase);

            var jsonRaw = await response.Content.ReadAsStringAsync();

            return JToken.Parse(jsonRaw);
        }

        public async IAsyncEnumerable<Guild> GetUserGuildsAsync(string token)
        {
            var afterId = "";

            while (true)
            {
                var route = "users/@me/guilds?limit=100";
                if (!string.IsNullOrWhiteSpace(afterId))
                    route += $"&after={afterId}";

                var response = await GetApiResponseAsync(token, route);

                if (!response.HasValues)
                    yield break;

                foreach (var guild in response.Select(ParseGuild))
                {
                    yield return guild;
                    afterId = guild.Id;
                }
            }
        }

        public async Task<JToken> GetSelfAsync(string token)
        {
            var response = await GetApiResponseAsync(token, $"users/@me");
            // cant be bothered to parse it, i only need the username anywayyyys

            return response;
        }

        public async Task<IReadOnlyList<Channel>> GetGuildChannelsAsync(string token, string guildId)
        {
            // Special case for direct messages pseudo-guild
            //if (guildId == Guild.DirectMessages.Id)
            //    return Array.Empty<Channel>();

            var response = await GetApiResponseAsync(token, $"guilds/{guildId}/channels");
            var channels = response.Select(ParseChannel).ToArray();

            return channels;
        }

        private async Task<Message> GetLastMessageAsync(string token, string channelId, DateTimeOffset? before = null)
        {
            var route = $"channels/{channelId}/messages?limit=1";
            if (before != null)
                route += $"&before={before.Value.ToSnowflake()}";

            var response = await GetApiResponseAsync(token, route);

            return response.Select(ParseMessage).FirstOrDefault();
        }

        public async IAsyncEnumerable<Message> GetMessagesAsync(string token, string channelId,
            DateTimeOffset? after = null, string? startingPoint = null, DateTimeOffset? before = null, IProgress<double>? progress = null)
        {
            // Get the last message
            var lastMessage = await GetLastMessageAsync(token, channelId, before);

            // If the last message doesn't exist or it's outside of range - return
            if (lastMessage == null || lastMessage.Timestamp < after)
            {
                progress?.Report(1);
                yield break;
            }

            // Get other messages
            var firstMessage = default(Message);

            //var nextId = after?.ToSnowflake() ?? "0";
            var nextId = startingPoint ?? "0";

            while (true)
            {
                // Get message batch
                //var route = $"channels/{channelId}/messages?limit=100&after={nextId}";
                var route = $"channels/{channelId}/messages?limit=100&after={nextId}";
                var response = await GetApiResponseAsync(token, route);

                // Parse
                var messages = response
                    .Select(ParseMessage)
                    .Reverse() // reverse because messages appear newest first
                    .ToArray();

                // Break if there are no messages (can happen if messages are deleted during execution)
                if (!messages.Any())
                    break;

                // Trim messages to range (until last message)
                var messagesInRange = messages
                    .TakeWhile(m => m.Id != lastMessage.Id && m.Timestamp < lastMessage.Timestamp)
                    .ToArray();

                // Yield messages
                foreach (var message in messagesInRange)
                {
                    // Set first message if it's not set
                    firstMessage ??= message;

                    // Report progress (based on the time range of parsed messages compared to total)
                    progress?.Report((message.Timestamp - firstMessage.Timestamp).TotalSeconds /
                                     (lastMessage.Timestamp - firstMessage.Timestamp).TotalSeconds);

                    yield return message;
                    nextId = message.Id;
                }

                // Break if messages were trimmed (which means the last message was encountered)
                if (messagesInRange.Length != messages.Length)
                    break;
            }

            // Yield last message
            yield return lastMessage;
            progress?.Report(1);
        }

        public void Dispose() => _httpClient.Dispose();
    }
}
