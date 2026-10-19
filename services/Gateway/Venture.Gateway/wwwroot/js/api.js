import { clearToken, getToken } from "./auth.js";
export class ApiError extends Error {
    status;
    problem;
    constructor(message, status, problem) {
        super(message);
        this.status = status;
        this.problem = problem;
        this.name = "ApiError";
    }
}
async function request(path, options = {}, auth = true) {
    const headers = new Headers(options.headers);
    if (options.body !== undefined && !headers.has("Content-Type")) {
        headers.set("Content-Type", "application/json");
    }
    if (auth) {
        const token = getToken();
        if (!token) {
            clearToken();
            window.location.href = "/login.html";
            throw new ApiError("Не авторизован", 401);
        }
        headers.set("Authorization", `Bearer ${token}`);
    }
    const response = await fetch(path, { ...options, headers });
    if (response.status === 401 && auth) {
        clearToken();
        window.location.href = "/login.html";
        throw new ApiError("Сессия истекла", 401);
    }
    if (response.status === 204) {
        return undefined;
    }
    const text = await response.text();
    const data = text ? JSON.parse(text) : undefined;
    if (!response.ok) {
        const problem = data;
        const message = problem?.errors?.map((e) => e.description).filter(Boolean).join(". ") ||
            problem?.detail ||
            problem?.title ||
            `Ошибка ${response.status}`;
        throw new ApiError(message, response.status, problem);
    }
    return data;
}
export const api = {
    register(body) {
        return request("/users", {
            method: "POST",
            body: JSON.stringify(body),
        }, false);
    },
    login(body) {
        return request("/users/login", {
            method: "POST",
            body: JSON.stringify(body),
        }, false);
    },
    getMyProfile() {
        return request("/users/me");
    },
    getUserProfile(username) {
        return request(`/users/${encodeURIComponent(username)}`, {}, false);
    },
    updateProfile(body) {
        return request("/users/me", {
            method: "PATCH",
            body: JSON.stringify(body),
        });
    },
    addSkill(body) {
        return request("/users/me/skills", {
            method: "POST",
            body: JSON.stringify(body),
        });
    },
    getFeed(page = 1, pageSize = 20) {
        return request(`/feed?page=${page}&pageSize=${pageSize}`);
    },
    createPost(content) {
        return request("/posts", {
            method: "POST",
            body: JSON.stringify({ content }),
        });
    },
    editPost(postId, content) {
        return request(`/posts/${postId}`, {
            method: "PUT",
            body: JSON.stringify({ content }),
        });
    },
    deletePost(postId) {
        return request(`/posts/${postId}`, { method: "DELETE" });
    },
    likePost(postId) {
        return request(`/posts/${postId}/likes`, { method: "POST" });
    },
    unlikePost(postId) {
        return request(`/posts/${postId}/likes`, { method: "DELETE" });
    },
    followUser(userId) {
        return request(`/users/${userId}/follow`, { method: "POST" });
    },
    unfollowUser(userId) {
        return request(`/users/${userId}/follow`, { method: "DELETE" });
    },
    getAchievementGroups(userId) {
        return request(`/internal/users/${userId}/achievement-groups`);
    },
    async uploadAvatar(file) {
        const form = new FormData();
        form.append("file", file);
        return uploadMedia("/users/me/avatar", form);
    },
    async uploadBanner(file) {
        const form = new FormData();
        form.append("file", file);
        return uploadMedia("/users/me/banner", form);
    },
};
async function uploadMedia(path, form) {
    const token = getToken();
    if (!token) {
        clearToken();
        window.location.href = "/login.html";
        throw new ApiError("Не авторизован", 401);
    }
    const response = await fetch(path, {
        method: "POST",
        headers: { Authorization: `Bearer ${token}` },
        body: form,
    });
    if (response.status === 401) {
        clearToken();
        window.location.href = "/login.html";
        throw new ApiError("Сессия истекла", 401);
    }
    const text = await response.text();
    const data = text ? JSON.parse(text) : undefined;
    if (!response.ok) {
        const problem = data;
        const message = problem?.detail ||
            problem?.title ||
            `Ошибка ${response.status}`;
        throw new ApiError(message, response.status, problem);
    }
    return data;
}
