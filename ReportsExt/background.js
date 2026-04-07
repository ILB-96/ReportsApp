const ENDPOINT = "http://127.0.0.1:8765/chrome-sync";
const TOKEN = "CHANGE_ME_TO_RANDOM";

const WANTED_COOKIE_NAMES = [
    "CrmOwinAuth",
    "CrmOwinAuthC1",
    "CrmOwinAuthC2",
    "CrmOwinAuthC3",
    "CrmOwinAuthC4",
    "CrmOwinAuthC5"
];

function isCrmUrl(url) {
    return typeof url === "string" &&
        (url.startsWith("http://") || url.startsWith("https://")) &&
        url.includes(".crm4.dynamics.com");
}

async function getCrmOriginsFromTabs() {
    const tabs = await chrome.tabs.query({});
    const urls = [];
    const seenUrls = new Set();
    const origins = new Set();

    for (const t of tabs) {
        const url = t.url || "";
        if (!isCrmUrl(url)) continue;

        if (!seenUrls.has(url)) {
            seenUrls.add(url);
            urls.push(url);
        }

        try {
            origins.add(new URL(url).origin);
        } catch {
            // ignore malformed
        }
    }

    return { urls, origins: [...origins] };
}

async function getCookiesByOrigin(origins) {
    const cookiesByOrigin = {};

    for (const origin of origins) {
        const allCookies = await chrome.cookies.getAll({ url: origin });
        const picked = {};

        for (const c of allCookies) {
            if (WANTED_COOKIE_NAMES.includes(c.name) && c.value) {
                picked[c.name] = c.value;
            }
        }

        if (Object.keys(picked).length > 0) {
            cookiesByOrigin[origin] = picked;
        }
    }

    return cookiesByOrigin;
}

async function pushChromeState() {
    const { urls, origins } = await getCrmOriginsFromTabs();
    const cookiesByOrigin = await getCookiesByOrigin(origins);

    await fetch(ENDPOINT, {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
            "X-TabToken": TOKEN
        },
        body: JSON.stringify({
            updatedAt: new Date().toISOString(),
            urls,
            cookiesByOrigin
        })
    }).catch(() => {});
}

chrome.tabs.onCreated.addListener(pushChromeState);
chrome.tabs.onRemoved.addListener(pushChromeState);
chrome.tabs.onUpdated.addListener((_id, info) => {
    if (info.url || info.status === "complete") pushChromeState();
});
chrome.tabs.onActivated.addListener(pushChromeState);
chrome.windows.onFocusChanged.addListener(pushChromeState);

chrome.cookies.onChanged.addListener(() => {
    pushChromeState();
});

setInterval(pushChromeState, 15000);