using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using WhitelistBot.Database.Contexts;
using WhitelistBot.Services;

namespace WhitelistBot;

public class Program
{
    private static IConfiguration _configuration = null!;
    private static IServiceProvider _services = null!;
    
    private const string ConfigurationFileName = "WhitelistBot.json";

    private static readonly DiscordSocketConfig SocketConfig = new()
    {
        GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers | GatewayIntents.MessageContent,
        AlwaysDownloadUsers = true,
    };

    public static async Task Main(string[] args)
    {
        _configuration = new ConfigurationBuilder()
            .AddJsonFile(ConfigurationFileName, false, false)
            .Build();
        
        var databaseHost = _configuration["database.host"];
        var databaseUsername = _configuration["database.username"];
        var databasePassword = _configuration["database.password"];
        var databasePort = _configuration["database.port"];
        var databaseTable = _configuration["database.table"];
        var connectionString = $"Server={databaseHost};Port={databasePort};Database={databaseTable};User Id={databaseUsername};Password={databasePassword};";
        
        _services = new ServiceCollection()
            .AddSingleton(_configuration)
            .AddSingleton(SocketConfig)
            .AddSingleton<DiscordSocketClient>()
            .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
            .AddSingleton<InteractionHandler>()
            .AddSingleton<WhitelistService>()
            .AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString))
            .BuildServiceProvider();

        var client = _services.GetRequiredService<DiscordSocketClient>();
        client.Log += LogAsync;

        // Here we can initialize the service that will register and execute our commands
        await InitializeServices(_services);

        // Bot token can be provided from the Configuration object we set up earlier
        await client.LoginAsync(TokenType.Bot, _configuration["token"]);
        await client.StartAsync();

        // Never quit the program until manually forced to.
        await Task.Delay(Timeout.Infinite);
    }

    private static async Task InitializeServices(IServiceProvider services)
    {
        await _services.GetRequiredService<InteractionHandler>()
            .InitializeAsync();
        await _services.GetRequiredService<WhitelistService>()
            .InitializeAsync();
    }

    private static Task LogAsync(LogMessage message)
    {
        Console.WriteLine(message.ToString());
        return Task.CompletedTask;
    }
}