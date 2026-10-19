const cache = new Map();
const defaultAvatarUrl = "/assets/defaults/default-avatar.jpg";
export function invalidateProfileMedia(userId) {
    cache.delete(userId);
}
export async function getProfileMedia(userId) {
    if (cache.has(userId)) {
        return cache.get(userId) ?? null;
    }
    try {
        const response = await fetch(`/users/${encodeURIComponent(userId)}/media`, { cache: "no-store" });
        if (response.status === 404) {
            cache.set(userId, null);
            return null;
        }
        if (!response.ok) {
            cache.set(userId, null);
            return null;
        }
        const data = (await response.json());
        cache.set(userId, data);
        return data;
    }
    catch {
        cache.set(userId, null);
        return null;
    }
}
export function avatarMarkup(userId, displayName, sizeClass, extras = {}) {
    const customUrl = extras.media?.avatarUrl;
    const url = customUrl || defaultAvatarUrl;
    const tag = extras.button ? "button" : "div";
    const openAttr = extras.openUser
        ? `data-open-user="${extras.openUser}" type="button" title="@${extras.openUser}"`
        : extras.button
            ? `type="button"`
            : "";
    const fallbackClass = customUrl ? "" : " avatar--default";
    return `<${tag} class="avatar ${sizeClass} avatar--image${fallbackClass}" ${openAttr} data-user-id="${userId}">
      <img src="${url}" alt="${String(displayName ?? "").replace(/"/g, "&quot;")}" />
    </${tag}>`;
}
export function bannerStyle(media) {
    if (!media?.bannerUrl)
        return "";
    return `style="background-image:url('${media.bannerUrl}'); background-size:cover; background-position:center; background-repeat:no-repeat;"`;
}

