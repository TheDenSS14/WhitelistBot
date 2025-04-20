using Discord;
using Discord.Commands;
using Discord.WebSocket;
using WhitelistBot.Services;

namespace WhitelistBot.Modules;

public class WhitelistCommand : ModuleBase<SocketCommandContext>
{
    public WhitelistService WhitelistService { get; set; } = null!;

    [Command("whitelist")]
    [RequireContext(ContextType.Guild)]
    [RequireUserPermission(GuildPermission.ManageMessages)]
    public async Task Whitelist(string username, IUser? user = null)
    {
        var response = await WhitelistService.WhitelistUser(username);
        var role = Context.Guild.Roles.FirstOrDefault(x => x.Name == "Whitelisted");
        await Context.Message.DeleteAsync();

        if (response == "UnprocessableEntity")
        {
            await ReplyAsync($":x: ``{username}`` is not a valid SS14 account.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(response))
        {
            await ReplyAsync(response);
            return;
        }
        
        await ReplyAsync($":white_check_mark: Successfully whitelisted ``{username}``.");

        if (user is not IGuildUser guildUser || role is null)
            return;
        
        await guildUser.AddRoleAsync(role);
    }
    
    [Command("whitelistPost")]
    [RequireContext(ContextType.Guild)]
    [RequireUserPermission(GuildPermission.ManageMessages)]
    public async Task WhitelistPost()
    {
        if (Context.Message.ReferencedMessage is not { } reply)
            return;
        
        var username = GetUsernameFromPost(reply);
        
        if (username is null)
            return;
        
        var response = await WhitelistService.WhitelistUser(username);
        
        await Context.Message.DeleteAsync();

        if (response == "UnprocessableEntity")
        {
            await reply.ReplyAsync($":x: ``{username}`` is not a valid SS14 account.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(response))
        {
            await ReplyAsync(response);
            return;
        }

        var role = Context.Guild.Roles.FirstOrDefault(x => x.Name == "Whitelisted");

        if (reply.Author is not IGuildUser guildUser
            || role is null)
            return;
        
        await guildUser.AddRoleAsync(role);
        await reply.ReplyAsync(":white_check_mark: You have been whitelisted!");
    }

    private string? GetUsernameFromPost(IUserMessage message)
    {
        var lines = message.Content.Split("\n");
        
        if (lines.Length == 0 || string.IsNullOrWhiteSpace(lines[0]))
            return null;
        
        var content = lines[0].Replace("\\", "")
            .Replace("*", "")
            .Replace("`", "");

        if (!content.StartsWith("SS14 Username: "))
            return null;

        return content.Substring(15);
    }
}