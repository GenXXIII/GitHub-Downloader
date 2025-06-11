using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public class GitHubDownload
{
    private static readonly HttpClient client = new HttpClient();
    private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(20); // Increase for faster downloads

    public async Task DownloadFolderRecursive(string owner, string repo, string branch, string githubPath, string localPath, string githubToken)
    {
        // Set up headers
        client.DefaultRequestHeaders.UserAgent.Clear();
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("GitHubDownloader", "1.0"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", githubToken);

        // Ensure path is encoded properly
        string escapedPath = string.IsNullOrWhiteSpace(githubPath) ? "" : Uri.EscapeUriString(githubPath);
        string apiUrl = $"https://api.github.com/repos/{owner}/{repo}/contents/{escapedPath}?ref={branch}";

        Console.WriteLine("Fetching: " + apiUrl); // Debug info

        // Make API request
        var response = await client.GetAsync(apiUrl);
        if (!response.IsSuccessStatusCode)
        {
            string details = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to fetch '{apiUrl}': {response.StatusCode} - {details}");
        }

        string json = await response.Content.ReadAsStringAsync();
        var items = JsonSerializer.Deserialize<JsonElement>(json);

        // Create local folder if it doesn't exist
        Directory.CreateDirectory(localPath);

        var allTasks = new List<Task>();

        foreach (var item in items.EnumerateArray())
        {
            string type = item.GetProperty("type").GetString()!;
            string name = item.GetProperty("name").GetString()!;

            if (type == "file")
            {
                string downloadUrl = item.GetProperty("download_url").GetString()!;
                string filePath = Path.Combine(localPath, name);

                allTasks.Add(DownloadFileWithLimitAsync(downloadUrl, filePath));
            }
            else if (type == "dir")
            {
                string subfolderGithubPath = githubPath + "/" + name;
                string subfolderLocalPath = Path.Combine(localPath, name);

                // Run subfolder downloads in parallel
                allTasks.Add(Task.Run(() =>
                    DownloadFolderRecursive(owner, repo, branch, subfolderGithubPath, subfolderLocalPath, githubToken)));
            }
        }

        await Task.WhenAll(allTasks); // Await all tasks
    }


    private async Task DownloadFileWithLimitAsync(string url, string filePath)
    {
        await semaphore.WaitAsync();
        try
        {
            Console.WriteLine("Downloading: " + url); // Optional console log
            var bytes = await client.GetByteArrayAsync(url);
            await File.WriteAllBytesAsync(filePath, bytes);
        }
        finally
        {
            semaphore.Release();
        }
    }
}
