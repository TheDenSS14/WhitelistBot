using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

namespace WhitelistBot.Services;

public class WhitelistService
{
    private HttpClient _httpClient;
    private List<string> ConnectAddresses { get; set; }
    
    public WhitelistService(List<string> connectAddresses, string apiToken)
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"SS14Token {apiToken}");
        
        ConnectAddresses = connectAddresses;
    }


    public async Task<string?> WhitelistUser(string name)
    {
        var whitelistActionBody = new WhitelistActionBody(name);
        var whitelistActionBodyJson = JsonConvert.SerializeObject(whitelistActionBody);
        var httpContent = new StringContent(whitelistActionBodyJson, Encoding.UTF8, "application/json");

        foreach (var connectAddress in ConnectAddresses)
        {
            var response = await _httpClient.PostAsync($"https://{connectAddress}/admin/actions/whitelist", httpContent);

            if (!response.IsSuccessStatusCode)
                return response.StatusCode.ToString();
        }

        return "";
    }
}

public class WhitelistActionBody(string username)
{
    public string Username { get; set; } = username;
}