using Octokit;

namespace LauncherBlazorSite.Client
{
    static class StaticResources
    {
        public static readonly GitHubClient GithubClient = new GitHubClient(new ProductHeaderValue("PSO2LeaLauncherBlazorSite"));
    }
}
