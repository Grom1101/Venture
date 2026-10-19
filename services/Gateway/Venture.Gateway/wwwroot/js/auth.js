const TOKEN_KEY = "venture.accessToken";
export function getToken() {
    return localStorage.getItem(TOKEN_KEY);
}
export function setToken(token) {
    localStorage.setItem(TOKEN_KEY, token);
}
export function clearToken() {
    localStorage.removeItem(TOKEN_KEY);
}
export function isAuthenticated() {
    return Boolean(getToken());
}
export function requireAuth() {
    if (!isAuthenticated()) {
        window.location.href = "/login.html";
    }
}
export function redirectIfAuthenticated() {
    if (isAuthenticated()) {
        window.location.href = "/";
    }
}
