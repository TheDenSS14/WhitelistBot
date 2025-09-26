using Discord;
using Discord.Commands;
using Discord.WebSocket;
using WhitelistBot.Services;

namespace WhitelistBot.Modules;

public class PinCommand : ModuleBase<SocketCommandContext>
{
    /// <summary>
    /// This command allows owners of forum threads to pin/unpin messages in their own threads.
    /// They must reply to the message that they would like to be pinned.
    /// (The message they are replying to must be in the same channel, of course.)
    /// </summary>
    [Command("pin")]
    [RequireContext(ContextType.Guild)]
    [RequireBotPermission(ChannelPermission.ManageMessages)] // It actually needs "pin messages", which Discord.NET doesn't have?
    public async Task Pin()
    {
        // The command must be sent by the owner of the forum thread.
        if (Context.Channel is not SocketThreadChannel thread
            || thread.ParentChannel.GetChannelType() != ChannelType.Forum
            || thread.Owner.Id != Context.User.Id)
            return;

        // The command needs to be replying to the message that you want pinned.
        if (Context.Message.Reference is not MessageReference reference
            || !reference.MessageId.IsSpecified
            || reference.ChannelId != thread.Id)
        {
            await Context.Message.ReplyAsync(text: "Please reply to the message you wish to pin.");
            return;
        }

        var repliedMessage = await thread.GetMessageAsync(reference.MessageId.Value);
        if (repliedMessage is not IUserMessage messageToPin)
        {
            // This should not happen, and if it does happen I have no idea why.
            await Context.Message.ReplyAsync(text: "Could not get replied message - this is a bug!");
            return;
        }

        if (messageToPin.IsPinned)
        {
            await messageToPin.UnpinAsync();
            await Context.Message.ReplyAsync("Unpinned message.");
            return;
        }

        await messageToPin.PinAsync();
    }
}
