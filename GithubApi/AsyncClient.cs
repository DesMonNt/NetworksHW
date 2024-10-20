using System.Collections.Concurrent;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;

namespace GithubApi;

public class AsyncClient: IDisposable
{
    private HttpClient Client { get; }

    public AsyncClient(string token)
    {
        Client = new HttpClient();
        Client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Mozilla", "5.0"));
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<IEnumerable<Repository>> GetRepositoriesAsync(string organization)
    {
        var repositories = new ConcurrentBag<Repository>();
        var page = 1;
        JArray json;

        do
        {
            var url = $"https://api.github.com/orgs/{organization}/repos?page={page}&per_page=100";
            var response = await Client.GetStringAsync(url);
            json = JArray.Parse(response);

            var tasks = json.Select(token => GetCommitsAsync(organization, token["name"].ToString())
                .ContinueWith(commitsTask => repositories.Add(new Repository(commitsTask.Result))));

            await Task.WhenAll(tasks);

            page++;
        } while (json.Count > 0);

        return repositories; 
    }

    private async Task<IEnumerable<Commit>> GetCommitsAsync(string organization, string repository)
    {
        var commits = new ConcurrentDictionary<string, Commit>();
        var page = 1;
        JArray json;

        do
        {
            var url = $"https://api.github.com/repos/{organization}/{repository}/commits?page={page}&per_page=100";
            var response = await Client.GetStringAsync(url);
            json = JArray.Parse(response);

            foreach (var commit in json)
            {
                var sha = commit["sha"].ToString();
                var message = commit["commit"]["message"].ToString();

                if (commits.ContainsKey(sha) || message.StartsWith("Merge pull request #"))
                    continue;
                
                commits[sha] = new Commit(commit["commit"]["author"]["email"].ToString());
            }

            page++;
        } while (json.Count > 0);
        
        return commits.Values;
    }
    
    public async Task<int> GetRateLimitAsync()
    {
        var response = await Client.GetStringAsync("https://api.github.com/rate_limit");
        return JObject.Parse(response)["rate"]["remaining"].ToObject<int>();
    }
    
    public void Dispose() => Client.Dispose();
}