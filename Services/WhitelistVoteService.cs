using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace WhitelistBot.Services;

public class WhitelistVoteService
{
    private readonly DiscordSocketClient _discord;

    private const ulong GuildId = 1301753657024319488;
    private const ulong WhitelistChannel = 1302308802619773062;
    private const ulong WhitelistVoteChannel = 1315318721836879942;

    private const string NameApi = "https://auth.spacestation14.com/api/query/name?name=";
    
    public WhitelistVoteService(IServiceProvider services)
    {
        _discord = services.GetRequiredService<DiscordSocketClient>();
    }

    public Task InitializeAsync()
    {
        _discord.MessageReceived += MessageReceived;
        _discord.MessageUpdated += MessageUpdated;
        
        return Task.CompletedTask;
    }

    private async Task MessageReceived(SocketMessage message) =>
        await WhitelistMessageSent(message);

    private async Task MessageUpdated(Cacheable<IMessage, ulong> before, SocketMessage after, ISocketMessageChannel channel) =>
        await WhitelistMessageSent(after);

    public async Task WhitelistMessageSent(IMessage message)
    {
        var content = message.Content.Replace("\\", "")
            .Replace("*", "")
            .Replace("`", "");
        
        if (message.Channel is not SocketTextChannel 
            || message.Channel.Id.ToString() != WhitelistChannel.ToString()
            || message.Author.IsBot
            || !content.StartsWith("SS14 Username"))
            return;
        
        var response = await GetWhitelistResponse(message);
        
        if (!response.IsValid && !string.IsNullOrWhiteSpace(response.Error))
            await message.Channel.SendMessageAsync( $"``{response.Error}``\nYou can dismiss this message and update your existing message to try again.");
        
        if (response.IsValid)
            await SendWhitelistVote(message, response);
    }

    private async Task<WhitelistResponse> GetWhitelistResponse(IMessage message)
    {
        List<string> lines = message.Content.Split("\n").ToList();
        List<string> responses = new List<string>();
        
        if (lines.Count < 3)
            return new WhitelistResponse("Form must be completed.");

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var sides = line.Split(": ");
            
            if (sides.Length < 2)
                continue;

            responses.Add(sides[1]);
        }
        
        if (responses.Count < 3)
            return new WhitelistResponse("Please follow the format.");

        var usernameValid = await IsUsernameValid(responses[0]);
        
        if (!usernameValid)
            return new WhitelistResponse("Username is invalid.");
        
        return new WhitelistResponse()
        {
            Username = responses[0],
            Age = responses[1],
            AboutMePost = responses[2]
        };
    }

    private async Task<bool> IsUsernameValid(string username)
    {
        var client = new HttpClient();
        var response = await client.GetAsync(NameApi + username);
        
        return response.IsSuccessStatusCode;
    }

    public async Task SendWhitelistVote(IMessage message, WhitelistResponse whitelist)
    {
        var channel = await _discord.GetChannelAsync(WhitelistVoteChannel) as IMessageChannel;
        
        var poll = new PollProperties()
        {
            Question = new()
            {
                Text = whitelist.Username ?? "no username"
            },
            Answers =
            [
                new PollMediaProperties()
                {
                    Text = "Yes"
                },
                new PollMediaProperties()
                {
                    Text = "No"
                }
            ],
            AllowMultiselect = false,
            LayoutType = PollLayout.Default,
            Duration = 24
        };

        try
        {
            if (channel == null)
                return;
            
            var reference = new MessageReference(message.Id, message.Channel.Id, guildId: GuildId, referenceType: MessageReferenceType.Forward);
            await channel.SendMessageAsync(poll: poll);
            await channel.SendMessageAsync(messageReference: reference);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    public struct WhitelistResponse
    {
        public readonly string? Error;
        public string? Username;
        public string? Age;
        public string? AboutMePost;

        public WhitelistResponse(string error)
        {
            Error = error;
        }

        public bool IsValid => Error == null && !string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(Age) && !string.IsNullOrEmpty(AboutMePost);
    }
}