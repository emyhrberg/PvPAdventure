using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

public static class MatchmakerClient
{
    private static readonly HttpClient http = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(5)
    };

    public static string BaseUrl = "https://hentzer.com/your-server";
    public static string QueueId;  // set after join

    public sealed class JoinResp { public string queueId; public int online; public int queuing; public string pollUrl; }
    public sealed class PollResp { public string status; public int online; public int queuing; public string matchId; public Server server; public string token; public sealed class Server { public string host; public int port; } }

    public static async Task<JoinResp> JoinAsync(string playerId, string characterHash)
    {
        var body = JsonConvert.SerializeObject(new { playerId, characterHash });
        var resp = await http.PostAsync($"{BaseUrl}/queue/join", new StringContent(body, Encoding.UTF8, "application/json"));
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync();
        var data = JsonConvert.DeserializeObject<JoinResp>(json);
        QueueId = data.queueId;
        return data;
    }

    public static async Task LeaveAsync()
    {
        if (QueueId == null) return;
        var body = JsonConvert.SerializeObject(new { queueId = QueueId });
        await http.PostAsync($"{BaseUrl}/queue/leave", new StringContent(body, Encoding.UTF8, "application/json"));
        QueueId = null;
    }

    public static async Task<PollResp> PollAsync(string pollUrl, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}{pollUrl}");
        using var resp = await http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<PollResp>(json);
    }
}
