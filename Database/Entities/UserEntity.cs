namespace WhitelistBot.Database.Entities;

public class UserEntity
{
    public ulong Id { get; set; }
    public ulong LastWhitelistVoteMessage { get; set; }
}