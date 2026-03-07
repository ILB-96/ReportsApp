const ENDPOINT = "http://127.0.0.1:8765/tabs";
const TOKEN = "CHANGE_ME_TO_RANDOM"; // must match WPF

async function pushTabs() {
    const tabs = await chrome.tabs.query({});

    const urls = [];
    const seen = new Set();

    for (const t of tabs) {
        const u = t.url || "";
        if (!u.startsWith("http://") && !u.startsWith("https://")) continue;
        if (seen.has(u)) continue;
        seen.add(u);
        urls.push(u);
    }

    fetch(ENDPOINT, {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
            "X-TabToken": TOKEN
        },
        body: JSON.stringify({ updated_at: new Date().toISOString(), urls })
    }).catch(() => {});
}

// update on tab/window events
chrome.tabs.onCreated.addListener(pushTabs);
chrome.tabs.onRemoved.addListener(pushTabs);
chrome.tabs.onUpdated.addListener((_id, info) => { if (info.url || info.status === "complete") pushTabs(); });
chrome.tabs.onActivated.addListener(pushTabs);
chrome.windows.onFocusChanged.addListener(pushTabs);

// also refresh periodically (MV3 service worker can sleep; this helps)
setInterval(pushTabs, 15000);