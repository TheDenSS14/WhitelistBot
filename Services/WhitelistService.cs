using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

namespace WhitelistBot.Services;

public class WhitelistService
{
    private HttpClient _httpClient;
    private string ConnectAddress { get; set; }
    
    public WhitelistService(string connectAddress, string apiToken)
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"SS14Token {apiToken}");
        
        ConnectAddress = connectAddress;
    }


    public async Task<string?> WhitelistUser(string name)
    {
        var whitelistActionBody = new WhitelistActionBody(name);
        var whitelistActionBodyJson = JsonConvert.SerializeObject(whitelistActionBody);
        var httpContent = new StringContent(whitelistActionBodyJson, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync($"https://{ConnectAddress}/admin/actions/whitelist", httpContent);

        if (!response.IsSuccessStatusCode)
            return response.StatusCode.ToString();

        return "";
    }
}

public class WhitelistActionBody
{
    public string Username { get; set; }

    public WhitelistActionBody(string username) => Username = username;
}