import { api, ApiError } from "./api.js";
import { redirectIfAuthenticated, setToken } from "./auth.js";
import { applyTheme } from "./theme.js";
import { showToast } from "./utils.js";
redirectIfAuthenticated();
void applyTheme();
const form = document.getElementById("login-form");
const submitBtn = document.getElementById("submit-btn");
form.addEventListener("submit", async (event) => {
    event.preventDefault();
    const email = form.querySelector("#email").value.trim();
    const password = form.querySelector("#password").value;
    submitBtn.disabled = true;
    try {
        const { accessToken } = await api.login({ email, password });
        setToken(accessToken);
        window.location.href = "/";
    }
    catch (error) {
        const message = error instanceof ApiError
            ? error.message
            : error instanceof Error
                ? error.message
                : "Не удалось войти";
        showToast(message, "error");
    }
    finally {
        submitBtn.disabled = false;
    }
});
