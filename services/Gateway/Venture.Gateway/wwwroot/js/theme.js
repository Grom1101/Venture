const BG_STORAGE_KEY = "venture.backgroundId";
const PANEL_STORAGE_KEY = "venture.panelStyle";
const BRIGHTNESS_STORAGE_KEY = "venture.backgroundBrightness";
const THEME_STORAGE_KEY = "venture.theme";
const DEFAULT_BRIGHTNESS = 50;
const MANIFEST_URL = "/assets/backgrounds/manifest.json";
const BACKGROUNDS_BASE = "/assets/backgrounds";
const DEFAULT_OPTION = {
    id: "default",
    name: "По-умолчанию",
    type: "color",
};
let cachedOptions = null;
function titleFromFile(file) {
    const base = file.replace(/\.[^.]+$/, "").replace(/[-_]+/g, " ").trim();
    if (!base)
        return file;
    return base.charAt(0).toUpperCase() + base.slice(1);
}
export function getSelectedBackgroundId() {
    return localStorage.getItem(BG_STORAGE_KEY) || "default";
}
export function setSelectedBackgroundId(id) {
    localStorage.setItem(BG_STORAGE_KEY, id);
}
export function getPanelStyle() {
    const value = localStorage.getItem(PANEL_STORAGE_KEY);
    if (value === "solid" || value === "outline" || value === "glass")
        return value;
    return "outline";
}
export function setPanelStyle(style) {
    localStorage.setItem(PANEL_STORAGE_KEY, style);
}
export function getTheme() {
    const value = localStorage.getItem(THEME_STORAGE_KEY);
    return value === "light" ? "light" : "dark";
}
export function setTheme(theme) {
    const next = theme === "light" ? "light" : "dark";
    localStorage.setItem(THEME_STORAGE_KEY, next);
    applyThemeMode(next);
    return next;
}
export function toggleTheme() {
    const current = getTheme();
    const next = current === "light" ? "dark" : "light";
    return setTheme(next);
}
export function applyThemeMode(theme) {
    const selected = theme ?? getTheme();
    const { body, documentElement } = document;
    if (selected === "light") {
        documentElement.setAttribute("data-theme", "light");
        documentElement.classList.add("theme-light");
        documentElement.classList.remove("theme-dark");
        if (body) {
            body.setAttribute("data-theme", "light");
            body.classList.add("theme-light");
            body.classList.remove("theme-dark");
        }
    }
    else {
        documentElement.setAttribute("data-theme", "dark");
        documentElement.classList.add("theme-dark");
        documentElement.classList.remove("theme-light");
        if (body) {
            body.setAttribute("data-theme", "dark");
            body.classList.add("theme-dark");
            body.classList.remove("theme-light");
        }
    }
}
export function getBackgroundBrightness() {
    const raw = localStorage.getItem(BRIGHTNESS_STORAGE_KEY);
    if (raw == null || raw === "")
        return DEFAULT_BRIGHTNESS;
    const value = Number(raw);
    if (!Number.isFinite(value))
        return DEFAULT_BRIGHTNESS;
    return Math.min(100, Math.max(0, value));
}
export function applyBackgroundBrightness(value) {
    const brightness = value ?? getBackgroundBrightness();
    let scrim = 0.72;
    let extraDark = 0;
    let extraLight = 0;
    if (brightness <= DEFAULT_BRIGHTNESS) {
        const t = brightness / DEFAULT_BRIGHTNESS;
        scrim = 0.98 + (0.72 - 0.98) * t;
        extraDark = (DEFAULT_BRIGHTNESS - brightness) / DEFAULT_BRIGHTNESS * 0.5;
    }
    else {
        const t = (brightness - DEFAULT_BRIGHTNESS) / (100 - DEFAULT_BRIGHTNESS);
        scrim = 0.72 * (1 - t);
        extraLight = t * 0.45;
    }
    const root = document.documentElement;
    root.style.setProperty("--bg-scrim", scrim.toFixed(3));
    root.style.setProperty("--bg-extra-dark", extraDark.toFixed(3));
    root.style.setProperty("--bg-extra-light", extraLight.toFixed(3));
}
export function setBackgroundBrightness(value) {
    const brightness = Math.min(100, Math.max(0, Math.round(Number(value))));
    localStorage.setItem(BRIGHTNESS_STORAGE_KEY, String(brightness));
    applyBackgroundBrightness(brightness);
}
async function imageExists(url) {
    try {
        const head = await fetch(url, { method: "HEAD", cache: "no-store" });
        if (head.ok)
            return true;
        if (head.status !== 405 && head.status !== 501)
            return false;
    }
    catch {
        return false;
    }
    try {
        const get = await fetch(url, { method: "GET", cache: "no-store" });
        return get.ok;
    }
    catch {
        return false;
    }
}
export async function loadBackgroundOptions() {
    if (cachedOptions)
        return cachedOptions;
    const options = [DEFAULT_OPTION];
    try {
        const response = await fetch(`${MANIFEST_URL}?t=${Date.now()}`, { cache: "no-store" });
        if (response.ok) {
            const manifest = (await response.json());
            const images = Array.isArray(manifest.images) ? manifest.images : [];
            const existing = await Promise.all(images.map(async (file) => {
                const trimmed = String(file).trim();
                if (!trimmed)
                    return null;
                const url = `${BACKGROUNDS_BASE}/${encodeURIComponent(trimmed)}`;
                if (!(await imageExists(url)))
                    return null;
                return {
                    id: `img:${trimmed}`,
                    name: titleFromFile(trimmed),
                    type: "image",
                    file: trimmed,
                    url,
                };
            }));
            for (const option of existing) {
                if (option)
                    options.push(option);
            }
        }
    }
    catch {
        // Keep default only if manifest is missing/unreadable.
    }
    cachedOptions = options;
    return options;
}
export function invalidateBackgroundCache() {
    cachedOptions = null;
}
export function applyPanelStyle(style) {
    const selected = style ?? getPanelStyle();
    const { body, documentElement } = document;
    body.classList.remove("panels-solid", "panels-outline", "panels-glass");
    documentElement.classList.remove("panels-solid", "panels-outline", "panels-glass");
    const className = selected === "outline"
        ? "panels-outline"
        : selected === "glass"
            ? "panels-glass"
            : "panels-solid";
    body.classList.add(className);
    documentElement.classList.add(className);
}
export async function applyBackground(preferredId) {
    const options = await loadBackgroundOptions();
    const requested = preferredId ?? getSelectedBackgroundId();
    const selected = options.find((option) => option.id === requested) ?? DEFAULT_OPTION;
    if (selected.id !== requested) {
        setSelectedBackgroundId(selected.id);
    }
    const { body, documentElement } = document;
    body.classList.remove("has-bg-image");
    documentElement.classList.remove("has-bg-image");
    body.style.removeProperty("--app-bg-image");
    documentElement.style.removeProperty("--app-bg-image");
    if (selected.type === "image") {
        const value = `url("${selected.url}")`;
        body.style.setProperty("--app-bg-image", value);
        documentElement.style.setProperty("--app-bg-image", value);
        body.classList.add("has-bg-image");
        documentElement.classList.add("has-bg-image");
    }
}
export async function applyTheme() {
    applyThemeMode();
    applyPanelStyle();
    applyBackgroundBrightness();
    await applyBackground();
}
export async function resetAppearance() {
    localStorage.removeItem(BG_STORAGE_KEY);
    localStorage.removeItem(PANEL_STORAGE_KEY);
    localStorage.removeItem(BRIGHTNESS_STORAGE_KEY);
    localStorage.removeItem(THEME_STORAGE_KEY);
    applyThemeMode("dark");
    applyPanelStyle();
    applyBackgroundBrightness();
    await applyBackground();
}
export async function selectBackground(id) {
    setSelectedBackgroundId(id);
    invalidateBackgroundCache();
    await applyBackground(id);
}
export function selectPanelStyle(style) {
    setPanelStyle(style);
    applyPanelStyle(style);
}
