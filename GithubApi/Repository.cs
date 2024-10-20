namespace GithubApi;

public class Repository(IEnumerable<Commit> commits)
{
    public Dictionary<string, int> GetActivity()
    {
        var authorCommits = new Dictionary<string, int>();
        
        foreach (var commit in commits)
        {
            if (string.IsNullOrEmpty(commit.Author)) 
                continue;
            
            if (!authorCommits.TryAdd(commit.Author, 1))
                authorCommits[commit.Author]++;
        }

        return authorCommits;
    }
}