using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks.Dataflow;
using System.Collections.Concurrent;

namespace GithubApi;

public class AsyncClient : IDisposable
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

        var repositoryBlock = new ActionBlock<JToken>(async token =>
        {
            var commits = await GetCommitsAsync(organization, token["name"].ToString());
            repositories.Add(new Repository(commits));
        }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 10 });

        do
        {
            var url = $"https://api.github.com/orgs/{organization}/repos?page={page}&per_page=100";
            var response = await Client.GetStringAsync(url);
            json = JArray.Parse(response);

            foreach (var token in json)
                await repositoryBlock.SendAsync(token);

            page++;
        } while (json.Count > 0);

        repositoryBlock.Complete();
        await repositoryBlock.Completion;

        return repositories;
    }

    private async Task<IEnumerable<Commit>> GetCommitsAsync(string organization, string repository)
    {
        var commits = new List<Commit>();
        var page = 1;
        JArray json;

        do
        {
            var url = $"https://api.github.com/repos/{organization}/{repository}/commits?page={page}&per_page=100";
            var response = await Client.GetStringAsync(url);
            json = JArray.Parse(response);

            foreach (var commit in json)
            {
                var message = commit["commit"]["message"].ToString();
                if (message.StartsWith("Merge pull request #"))
                    continue;

                var email = commit["commit"]["author"]?["email"]?.ToString();
                
                if (email != null)
                    commits.Add(new Commit(email));
            }

            page++;
        } while (json.Count > 0);

        return commits;
    }

    public async Task<int> GetRateLimitAsync()
    {
        var response = await Client.GetStringAsync("https://api.github.com/rate_limit");
        return JObject.Parse(response)["rate"]["remaining"].ToObject<int>();
    }

    public void Dispose() => Client.Dispose();
}