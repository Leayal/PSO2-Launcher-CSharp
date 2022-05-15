import { Octokit } from "https://cdn.skypack.dev/@octokit/rest";

(function (w, d) {
    DarkReader.auto({
        brightness: 100,
        contrast: 90,
        sepia: 10
    });

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
    const content = d.getElementById("content");
    if (content) {
        const _search = new URLSearchParams(w.location.search), currentpage = (_search.get("page") || "").toLowerCase();
        if (currentpage == "downloads") {
            RenderDownloads(content);
        } else if (currentpage == "changelog") {
            RenderChangelog(content);
        } else {
            RenderHome(content);
        }
    }

    function AddLoadingElement(parentDom) {
        const loader = d.createElement("div");
        const animation = d.createElement("div");
        animation.classList.add("cssload-tetrominos");
        for (let i = 1; i <= 4; i++) {
            const loader_box = d.createElement("div");
            loader_box.classList.add("cssload-tetromino");
            loader_box.classList.add("cssload-box" + i.toString());
            animation.appendChild(loader_box);
        }
        loader.classList.add("loader3d");
        loader.appendChild(animation);
        parentDom.appendChild(loader);
    }

    function RemoveLoadingElement(parentDom) {
        const loader = parentDom.querySelector(".loader3d");
        if (loader) {
            parentDom.removeChild(loader);
        }
    }
    
    function RenderHome(parentDom) {
        const home_body = d.createElement("p"), home_title = d.createElement("h2");
        home_title.textContent = "Hello there.";
        home_body.textContent = "There's nothing to describe the launcher yet. But you can visit Downloads and Changelog page.";
        parentDom.appendChild(home_title);
        parentDom.appendChild(home_body);
    }     

    async function RenderDownloads(parentDom) {
        AddLoadingElement(parentDom);
        const latestReleaseInfo = (await octokit.rest.repos.getLatestRelease(targetRepo)).data;
        RemoveLoadingElement(parentDom);
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

    async function RenderChangelog(parentDom) {
        AddLoadingElement(parentDom);
        const commitlogs = (await octokit.rest.repos.listCommits(targetRepo)).data;
        RemoveLoadingElement(parentDom);
        const commit_list = d.createElement("div"), page_description = d.createElement("h1");
        
        for (const commitData of commitlogs) {
            const commit_item = d.createElement("div"), commit_header = d.createElement("div");
            const commit_title = d.createElement("h2"), commit_author = d.createElement("span"), commit_summary = d.createElement("p");
            const commit_viewMoreDetail = d.createElement("a");

            commit_item.classList.add("git-commit-item");
            commit_header.classList.add("git-commit-header");
            commit_viewMoreDetail.classList.add("git-commit-viewdetail");

            commit_title.textContent = commitData.commit.committer.date;
            commit_author.textContent = "by " + commitData.commit.committer.name;

            commit_viewMoreDetail.href = commitData.html_url;
            commit_viewMoreDetail.target = "_blank";
            commit_viewMoreDetail.textContent = "View more details on Github";

            commit_summary.innerHTML = markdown.makeHtml(commitData.commit.message);
            
            commit_header.appendChild(commit_title);
            commit_header.appendChild(commit_author);
            commit_header.appendChild(commit_viewMoreDetail);
            commit_item.appendChild(commit_header);
            commit_item.appendChild(commit_summary);
            commit_list.appendChild(commit_item);
        }
        
        page_description.textContent = "Below are the 30 recent commits. You can view all commits at "
        const page_description_link = d.createElement("a");
        page_description_link.href = "https://github.com/Leayal/PSO2-Launcher-CSharp/commits/main";
        page_description_link.target = "_blank";
        page_description_link.textContent = "Github commit log";
        page_description.appendChild(page_description_link);

        commit_list.classList.add("git-commit-list");
        
        parentDom.appendChild(page_description);
        parentDom.appendChild(commit_list);
    }
})(window, window.document);