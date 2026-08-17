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
const BO_ORIGINS = [
    "https://car2gobo.gototech.co",
    "https://prodautotelbo.gototech.co"
];

const WANTED_SESSION_KEYS = ["ngStorage-credentials"];
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

function isBetterwayAppUrl(url) {
    return typeof url === "string" && url.startsWith("https://app.betterway.co.il");
}

// Runs in the page's context. Reads exactly the refresh token.
// If the key turns out to be something else, change the key name here.
function readBetterwayRefreshToken() {
    return {
        refreshToken: localStorage.getItem("refresh_token"),
        allKeys: Object.keys(localStorage) // for one-time debugging; remove later
    };
}

async function getBetterwayRefreshToken() {
    const tabs = await chrome.tabs.query({ url: "https://app.betterway.co.il/*" });
    for (const tab of tabs) {
        try {
            const [{ result }] = await chrome.scripting.executeScript({
                target: { tabId: tab.id },
                func: readBetterwayRefreshToken,
                world: "MAIN"
            });
            if (result?.refreshToken) return result.refreshToken;
        } catch {
            // tab closed, navigated, restricted page — try the next one
        }
    }
    return null;
}

function readSessionStorageKeys(keys) {
    const out = {};
    for (const k of keys) {
        const v = window.sessionStorage.getItem(k);
        if (v != null) out[k] = v;
    }
    return out;
}

async function getSessionStorageByOrigin() {
    const sessionStorageByOrigin = {};

    for (const origin of BO_ORIGINS) {
        const tabs = await chrome.tabs.query({ url: origin + "/*" });

        for (const tab of tabs) {
            try {
                const [{ result }] = await chrome.scripting.executeScript({
                    target: { tabId: tab.id },
                    func: readSessionStorageKeys,
                    args: [WANTED_SESSION_KEYS],
                    world: "MAIN"
                });
                if (result && Object.keys(result).length > 0) {
                    sessionStorageByOrigin[origin] = result;
                    break;
                }
            } catch {
                // tab closed, navigated, restricted page — try the next one
            }
        }
    }

    return sessionStorageByOrigin;
}

async function pushChromeState() {
    const { urls, origins } = await getCrmOriginsFromTabs();
    const cookiesByOrigin = await getCookiesByOrigin(origins);
    const betterwayRefreshToken = await getBetterwayRefreshToken();
    const sessionStorageByOrigin = await getSessionStorageByOrigin();

    await fetch(ENDPOINT, {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
            "X-TabToken": TOKEN
        },
        body: JSON.stringify({
            updatedAt: new Date().toISOString(),
            urls,
            cookiesByOrigin,
            betterwayRefreshToken,
            sessionStorageByOrigin
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


