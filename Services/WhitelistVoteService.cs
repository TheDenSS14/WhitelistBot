using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace WhitelistBot.Services;

public class WhitelistVoteService(IServiceProvider services)
{
    private readonly DiscordSocketClient _discord = services.GetRequiredService<DiscordSocketClient>();
    private Dictionary<string, IMessage> _messages = new();

    private const ulong GuildId = 1301753657024319488;
    private const ulong WhitelistChannel = 1302308802619773062;
    private const ulong WhitelistVoteChannel = 1315318721836879942;
    private const ulong WhitelistedRoleId = 1302305187137847447;

    private const string NameApi = "https://auth.spacestation14.com/api/query/name?name=";
    
    public Task InitializeAsync()
    {
        _discord.MessageReceived += MessageReceived;
        _discord.MessageUpdated += MessageUpdated;
        _discord.MessageDeleted += (msg, cacheable1) => MessageDeleted(msg);
        
        return Task.CompletedTask;
    }

    private async Task MessageReceived(SocketMessage message) =>
        await WhitelistMessageSent(message);

    private async Task MessageUpdated(Cacheable<IMessage, ulong> before, SocketMessage after, ISocketMessageChannel channel)
    {
        // Scuffed solution to discord re-sending already accepted applicants.
        if (after.Author is not SocketGuildUser user
            || user.Roles.Any(r => r.Id == WhitelistedRoleId))
            return;
        
        await WhitelistMessageSent(after, false);
    }

    private async Task MessageDeleted(Cacheable<IMessage, ulong> msg)
    {
        if (!msg.HasValue || msg.Value.Channel.Id != WhitelistVoteChannel)
            return;
        
        if (_messages.ContainsKey(msg.Value.Author.Id.ToString()))
        {
            _messages.Remove(msg.Value.Author.Id.ToString(), out var errorMsg);
            
            if (errorMsg == null)
                return;
            
            await errorMsg.DeleteAsync();
        }
    }

    public async Task WhitelistMessageSent(IMessage message, bool sent = true)
    {
        if (message.Author is not IGuildUser guildUser)
            return;
        
        var content = message.Content.Replace("\\", "")
            .Replace("*", "")
            .Replace("`", "")
            .ToLower();
        
        if (message.Channel is not SocketTextChannel 
            || message.Channel.Id.ToString() != WhitelistChannel.ToString()
            || message.Author.IsBot
            || !(content.StartsWith("ss14 username") || content.StartsWith("username")))
            return;
        
        var response = await GetWhitelistResponse(message);
        
        if (!response.IsValid && !string.IsNullOrWhiteSpace(response.Error))
        {
            await HandleError(message, 
                $"``{response.Error}``\nYou can dismiss this message and update your existing message to try again.");
        }
        
        if (response.IsValid)
        {
            var success = new Emoji("\ud83d\udd50");
            await SendWhitelistVote(message, response);
            await message.AddReactionAsync(success);
            await HandleSuccess(message);
        }
    }

    private async Task HandleError(IMessage message, string error)
    {
        var messageAuthorId = message.Author.Id.ToString();
        var deleteLast = _messages.ContainsKey(messageAuthorId);

        if (deleteLast)
        {
            var savedError = _messages.Remove(messageAuthorId, out var lastMessage);

            if (savedError && lastMessage is not null)
                await lastMessage.DeleteAsync();
        }

        if (message is not IUserMessage userMessage)
        {
            var buggedErrorMessage = await message.Channel.SendMessageAsync(error, messageReference: message.Reference);
            _messages.Add(messageAuthorId, buggedErrorMessage);
            return;
        }

        var newErrorMessage = await userMessage.ReplyAsync(error);
        _messages.Add(messageAuthorId, newErrorMessage);
    }

    private async Task HandleSuccess(IMessage message)
    {
        var messageAuthorId = message.Author.Id.ToString();
        var deleteLast = _messages.ContainsKey(messageAuthorId);
        
        if (deleteLast)
        {
            var lastError = _messages.Remove(messageAuthorId, out var lastMessage);
            
            if (!lastError || lastMessage is null)
                return;
            
            await lastMessage.DeleteAsync();
        }
    }

    private async Task<WhitelistResponse> GetWhitelistResponse(IMessage message)
    {
        List<string> lines = message.Content.Split("\n").ToList();
        
        if (lines.Count < 4)
            return new WhitelistResponse("Form must be completed.");

        var usernameCandidate = lines[0].Split(":");
        
        if (usernameCandidate.Length < 2)
            return new WhitelistResponse("Username must be provided.");
        
        var username = usernameCandidate[1].Trim();
        
        if (lines.Count < 4)
            return new WhitelistResponse("Please follow the format.");

        var usernameValid = await IsUsernameValid(username);
        
        if (!usernameValid)
            return new WhitelistResponse("Username is invalid.");
        
        return new WhitelistResponse
        {
            Username = username,
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
            Duration = 72
        };

        try
        {
            if (channel == null)
                return;
            
            var reference = new MessageReference(message.Id, message.Channel.Id, guildId: GuildId, referenceType: MessageReferenceType.Forward);
            var voteMessage = await channel.SendMessageAsync(messageReference: reference);
            
            await channel.SendMessageAsync(poll: poll); 
            await message.Channel.SendMessageAsync($"For admins: the vote for {message.Author} can be found [here]({voteMessage.GetJumpUrl()})");
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

        public WhitelistResponse(string error)
        {
            Error = error;
        }

        public bool IsValid => Error == null && !string.IsNullOrEmpty(Username);
    }
}