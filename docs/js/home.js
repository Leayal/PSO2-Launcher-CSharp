import { Octokit } from "https://cdn.skypack.dev/@octokit/rest";

(function (w, d) {
    const targetRepo = {
        owner: "Leayal",
        repo: "PSO2-Launcher-CSharp"
    }, markdown = new showdown.Converter({
        strikethrough: true,
        tables: true,
        tasklists: true,
        ghCodeBlocks: true,
        ghMentions: true,
        ghMentionsLink: true,
        openLinksInNewWindow: true,
        emoji: true
    });
    markdown.setFlavor('github');

    const octokit = new Octokit({
        userAgent: "PSO2LeaLauncherWeb v1.0"
    });
    const _search = new URLSearchParams(w.location.search);
    const currentpage = (_search.get("page") || "").toLowerCase();
    if (currentpage == "downloads") {
        const content = d.getElementById("content");
        if (content) {
            RenderDownloads(content);
        }
    } else if (currentpage == "changelog") {
        //a
    } else {
        // a
    }

    async function RenderDownloads(parentDom) {
        const latestReleaseInfo = (await octokit.rest.repos.getLatestRelease(targetRepo)).data;
        console.log(latestReleaseInfo);
        const release_body = d.createElement("p"), release_title = d.createElement("h1"), release_assets = d.createElement("div"), title_download = d.createElement("h1");
        
        release_title.textContent = latestReleaseInfo.name;
        release_body.innerHTML = markdown.makeHtml(latestReleaseInfo.body);
        for (const asset of latestReleaseInfo.assets) {
            const theLink = d.createElement("a");
            theLink.href = asset.browser_download_url;
            theLink.download = theLink.textContent = asset.name;
            release_assets.appendChild(theLink);
        }
        title_download.textContent = "Downloads:"

        release_title.classList.add("git-release-title");
        release_body.classList.add("git-release-body");
        release_assets.classList.add("git-release-asset-list");

        parentDom.appendChild(release_title);
        parentDom.appendChild(release_body);
        parentDom.appendChild(title_download);
        parentDom.appendChild(release_assets);
    }     
})(window, window.document);