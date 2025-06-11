using GitHubDownloader.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace GitHubDownloader.Controllers   // <--- IMPORTANT!
{
    public class GitHubDownloadController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View(new Models.GitHubDownloadModel());
        }

        [HttpPost]
        public async Task<IActionResult> Index(Models.GitHubDownloadModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var downloader = new GitHubDownload();
            await downloader.DownloadFolderRecursive(
                model.RepoOwner,
                model.RepoName,
                model.Branch,
                model.GithubFolderPath,
                model.LocalRootFolder,
                model.GithubToken);

            ViewBag.Message = "Download completed successfully!";
            return View(model);
        }
    }
}
