namespace GithubApi;

public static class Program
{
    public static async Task Main()
    {
        var client = new AsyncClient("YOUR_TOKEN_HERE");
        
        Console.WriteLine($"Remained requests count: {await client.GetRateLimitAsync()}");
        
        var repositories = await client.GetRepositoriesAsync("openai");
        var result = repositories.Select(repository => repository.GetActivity())
            .SelectMany(dict => dict)
            .GroupBy(pair => pair.Key)
            .ToDictionary(group => group.Key, group => group.Sum(pair => pair.Value));
        
        await using var writer = new StreamWriter("activity_report.txt");
        
        foreach (var (email, count) in result.OrderByDescending(x => x.Value).Take(100))
            await writer.WriteLineAsync($"Author: {email}, Commits: {count}");
    }
}