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

const TAB_SCRIPT_TIMEOUT_MS = 3_000;
const COOKIE_FETCH_TIMEOUT_MS = 3_000;
const PUSH_FETCH_TIMEOUT_MS = 5_000;
const PUSH_TOTAL_TIMEOUT_MS = 10_000;

// Races a promise against a timeout. Resolves to `fallback` on timeout/error.
function withTimeout(promise, ms, fallback = null) {
    const timer = new Promise(resolve => setTimeout(() => resolve(fallback), ms));
    return Promise.race([promise, timer]).catch(() => fallback);
}

function isCrmUrl(url) {
    return typeof url === "string" &&
        (url.startsWith("http://") || url.startsWith("https://")) &&
        url.includes(".crm4.dynamics.com");
}

async function getCrmOriginsFromTabs() {
    const tabs = await withTimeout(chrome.tabs.query({}), TAB_SCRIPT_TIMEOUT_MS, []);
    const urls = [];
    const seenUrls = new Set();
    const origins = new Set();

    for (const t of tabs) {
        const url = t.url || "";
        if (!isCrmUrl(url)) continue;
        if (!seenUrls.has(url)) { seenUrls.add(url); urls.push(url); }
        try { origins.add(new URL(url).origin); } catch { /* malformed */ }
    }

    return { urls, origins: [...origins] };
}

async function getCookiesForOrigin(origin) {
    const allCookies = await withTimeout(
        chrome.cookies.getAll({ url: origin }),
        COOKIE_FETCH_TIMEOUT_MS,
        []
    );
    const picked = {};
    for (const c of allCookies) {
        if (WANTED_COOKIE_NAMES.includes(c.name) && c.value) {
            picked[c.name] = c.value;
        }
    }
    return Object.keys(picked).length > 0 ? picked : null;
}

async function getCookiesByOrigin(origins) {
    const results = await Promise.allSettled(
        origins.map(async origin => ({ origin, cookies: await getCookiesForOrigin(origin) }))
    );
    const cookiesByOrigin = {};
    for (const r of results) {
        if (r.status === "fulfilled" && r.value.cookies) {
            cookiesByOrigin[r.value.origin] = r.value.cookies;
        }
    }
    return cookiesByOrigin;
}

function readBetterwayRefreshToken() {
    return { refreshToken: localStorage.getItem("refresh_token") };
}

async function getBetterwayRefreshToken() {
    const tabs = await withTimeout(
        chrome.tabs.query({ url: "https://app.betterway.co.il/*" }),
        TAB_SCRIPT_TIMEOUT_MS,
        []
    );
    for (const tab of tabs) {
        const result = await withTimeout(
            chrome.scripting.executeScript({
                target: { tabId: tab.id },
                func: readBetterwayRefreshToken,
                world: "MAIN"
            }).then(([{ result }]) => result),
            TAB_SCRIPT_TIMEOUT_MS
        );
        if (result?.refreshToken) return result.refreshToken;
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

async function getSessionStorageForOrigin(origin) {
    const tabs = await withTimeout(
        chrome.tabs.query({ url: origin + "/*" }),
        TAB_SCRIPT_TIMEOUT_MS,
        []
    );
    for (const tab of tabs) {
        const result = await withTimeout(
            chrome.scripting.executeScript({
                target: { tabId: tab.id },
                func: readSessionStorageKeys,
                args: [WANTED_SESSION_KEYS],
                world: "MAIN"
            }).then(([{ result }]) => result),
            TAB_SCRIPT_TIMEOUT_MS
        );
        if (result && Object.keys(result).length > 0) return result;
    }
    return null;
}

async function getSessionStorageByOrigin() {
    const results = await Promise.allSettled(
        BO_ORIGINS.map(async origin => ({ origin, data: await getSessionStorageForOrigin(origin) }))
    );
    const sessionStorageByOrigin = {};
    for (const r of results) {
        if (r.status === "fulfilled" && r.value.data) {
            sessionStorageByOrigin[r.value.origin] = r.value.data;
        }
    }
    return sessionStorageByOrigin;
}

async function pushChromeState() {
    // Collect all data in parallel; each step has its own internal timeout
    const [
        { urls, origins },
        betterwayRefreshToken,
    ] = await Promise.all([
        withTimeout(getCrmOriginsFromTabs(), PUSH_TOTAL_TIMEOUT_MS, { urls: [], origins: [] }),
        withTimeout(getBetterwayRefreshToken(), PUSH_TOTAL_TIMEOUT_MS, null),
    ]);

    const [cookiesByOrigin, sessionStorageByOrigin] = await Promise.all([
        withTimeout(getCookiesByOrigin(origins), PUSH_TOTAL_TIMEOUT_MS, {}),
        withTimeout(getSessionStorageByOrigin(), PUSH_TOTAL_TIMEOUT_MS, {}),
    ]);

    // Always POST whatever we have; abort if the server is slow
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), PUSH_FETCH_TIMEOUT_MS);
    try {
        await fetch(ENDPOINT, {
            method: "POST",
            signal: controller.signal,
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
        });
    } catch {
        // network down, aborted, or server not running — silently ignore
    } finally {
        clearTimeout(timeoutId);
    }
}

chrome.tabs.onCreated.addListener(pushChromeState);
chrome.tabs.onRemoved.addListener(pushChromeState);
chrome.tabs.onUpdated.addListener((_id, info) => {
    if (info.url || info.status === "complete") pushChromeState();
});
chrome.tabs.onActivated.addListener(pushChromeState);
chrome.windows.onFocusChanged.addListener(pushChromeState);
chrome.cookies.onChanged.addListener(pushChromeState);

setInterval(pushChromeState, 5_000);