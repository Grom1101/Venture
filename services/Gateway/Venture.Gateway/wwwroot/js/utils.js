export function escapeHtml(value) {
    return value
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#39;");
}
export function initials(name) {
    const parts = name.trim().split(/\s+/).filter(Boolean);
    if (parts.length === 0)
        return "?";
    if (parts.length === 1)
        return parts[0].slice(0, 2);
    return `${parts[0][0] ?? ""}${parts[1][0] ?? ""}`;
}
export function formatRelativeTime(iso) {
    const date = new Date(iso);
    const diffMs = Date.now() - date.getTime();
    const sec = Math.max(0, Math.floor(diffMs / 1000));
    if (sec < 60)
        return "только что";
    const min = Math.floor(sec / 60);
    if (min < 60)
        return `${min}м`;
    const hours = Math.floor(min / 60);
    if (hours < 24)
        return `${hours}ч`;
    const days = Math.floor(hours / 24);
    if (days < 7)
        return `${days}д`;
    return date.toLocaleDateString("ru-RU", {
        day: "numeric",
        month: "short",
        year: "numeric",
    });
}
export function formatNumber(value) {
    return new Intl.NumberFormat("ru-RU").format(value);
}
const toastDurationMs = 3800;
export function showToast(message, kind = "info") {
    document.querySelectorAll(".toast").forEach((el) => el.remove());
    const toast = document.createElement("div");
    toast.className = `toast${kind === "error" ? " toast--error" : ""}`;
    const text = document.createElement("div");
    text.className = "toast-text";
    text.textContent = message;
    const progress = document.createElement("div");
    progress.className = "toast-progress";
    toast.append(text, progress);
    document.body.appendChild(toast);
    window.setTimeout(() => toast.remove(), toastDurationMs);
}
export function parseHash() {
    const raw = window.location.hash.replace(/^#\/?/, "") || "feed";
    const [view, param] = raw.split("/");
    return { view: view || "feed", param };
}
export function navigate(path) {
    window.location.hash = path.startsWith("#") ? path : `#/${path.replace(/^\//, "")}`;
}
