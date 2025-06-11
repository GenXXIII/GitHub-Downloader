namespace GitHubDownloader.Models
{
    public class GitHubDownloadModel
    {
        public string RepoOwner { get; set; }
        public string RepoName { get; set; }
        public string Branch { get; set; }
        public string GithubFolderPath { get; set; }
        public string LocalRootFolder { get; set; }
        public string GithubToken { get; set; }
    }
}
