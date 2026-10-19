import { api, ApiError } from "./api.js";
import { clearToken, requireAuth } from "./auth.js";
import { icons } from "./icons.js";
import { SkillLevelLabels } from "./types.js";
import { avatarMarkup, bannerStyle, getProfileMedia, invalidateProfileMedia, } from "./profileMedia.js";
import { applyTheme, getPanelStyle, getSelectedBackgroundId, getTheme, loadBackgroundOptions, resetAppearance, selectBackground, selectPanelStyle, toggleTheme, } from "./theme.js";
import { escapeHtml, formatNumber, formatRelativeTime, navigate, parseHash, showToast, } from "./utils.js";
requireAuth();
void applyTheme();
const state = {
    me: null,
    feed: [],
    feedPage: 1,
    feedLoading: false,
    feedDone: false,
    profile: null,
    profileTab: "posts",
    followingIds: new Set(),
    searchQuery: "",
    mediaByUserId: new Map(),
};
const root = document.getElementById("app");
if (!root)
    throw new Error("#app not found");
async function ensureMedia(userId) {
    if (state.mediaByUserId.has(userId)) {
        return state.mediaByUserId.get(userId) ?? null;
    }
    const media = await getProfileMedia(userId);
    state.mediaByUserId.set(userId, media);
    return media;
}
async function ensureMediaMany(userIds) {
    const unique = [...new Set(userIds)].filter((id) => !state.mediaByUserId.has(id));
    await Promise.all(unique.map((id) => ensureMedia(id)));
}
function mediaOf(userId) {
    return state.mediaByUserId.get(userId) ?? null;
}
function handleError(error) {
    if (error instanceof ApiError) {
        showToast(error.message, "error");
        return;
    }
    showToast(error instanceof Error ? error.message : "Неизвестная ошибка", "error");
}
function renderShell() {
    const route = parseHash();
    const activeNav = route.view === "appearance"
        ? "appearance"
        : route.view === "profile" || route.view === "edit"
            ? "profile"
            : "feed";
    const currentTheme = getTheme();
    const isLight = currentTheme === "light";
    root.innerHTML = `
    <div class="app-shell">
      <aside class="sidebar">
        <div class="brand">Venture</div>
        <div class="sidebar-menu">
          <nav class="sidebar-nav" style="display:flex;flex-direction:column;gap:0.2rem;">
            ${navButton("feed", icons.feed, "Лента", activeNav === "feed")}
            ${navButton("profile", icons.profile, "Профиль", activeNav === "profile")}
            ${navDisabled(icons.path, "Мой путь")}
            ${navDisabled(icons.tasks, "Граф развития")}
            ${navDisabled(icons.messages, "Сообщения")}
            ${navDisabled(icons.bell, "Уведомления")}
            ${navDisabled(icons.people, "Исследование")}
            ${navDisabled(icons.communities, "Сообщества")}
            ${navDisabled(icons.sprints, "Спринты")}
            ${navDisabled(icons.leaders, "Лидеры")}
            ${navButton("appearance", icons.appearance, "Оформление", activeNav === "appearance")}
            ${navDisabled(icons.settings, "Настройки")}
          </nav>
          <button class="nav-item" id="logout-btn" style="margin-top:1.5rem;color:#f87171;">
            ${icons.logout}<span class="label">Выйти</span>
          </button>
        </div>
      </aside>

      <div class="topbar">
        <form class="search-box" id="search-form">
          ${icons.search}
          <input id="search-input" type="search" placeholder="Поиск по username" value="${escapeHtml(state.searchQuery)}" autocomplete="off" />
        </form>
        <button class="btn-icon" title="Скоро" disabled>${icons.terminal}</button>
        <button class="btn-icon" title="Скоро" disabled>${icons.mail}</button>
        <button class="btn-icon" title="Скоро" disabled>${icons.globe}</button>
        <button class="btn-icon theme-toggle" id="theme-toggle" type="button" title="${isLight ? "Переключить на тёмную тему" : "Переключить на светлую тему"}" aria-label="${isLight ? "Переключить на тёмную тему" : "Переключить на светлую тему"}">
          ${isLight ? icons.moon : icons.sun}
        </button>
      </div>

      <main>
        <div id="main-content"></div>
      </main>

      <aside class="aside-panel" id="aside-panel"></aside>
    </div>
  `;
    const themeBtn = root.querySelector("#theme-toggle");
    themeBtn?.addEventListener("click", () => {
        const nextTheme = toggleTheme();
        const nextIsLight = nextTheme === "light";
        themeBtn.innerHTML = nextIsLight ? icons.moon : icons.sun;
        const title = nextIsLight ? "Переключить на тёмную тему" : "Переключить на светлую тему";
        themeBtn.setAttribute("title", title);
        themeBtn.setAttribute("aria-label", title);
    });
    root.querySelector("#logout-btn")?.addEventListener("click", () => {
        clearToken();
        window.location.href = "/login.html";
    });
    root.querySelectorAll("[data-nav]").forEach((el) => {
        el.addEventListener("click", () => {
            const target = el.dataset.nav;
            if (target === "profile")
                navigate("profile");
            else if (target === "appearance")
                navigate("appearance");
            else
                navigate("feed");
        });
    });
    root.querySelector("#search-form")?.addEventListener("submit", (e) => {
        e.preventDefault();
        const input = root.querySelector("#search-input");
        const username = input.value.trim().replace(/^@/, "");
        state.searchQuery = username;
        if (!username)
            return;
        navigate(`u/${encodeURIComponent(username)}`);
    });
}
function navButton(id, icon, label, active) {
    return `
    <button class="nav-item ${active ? "active" : ""}" data-nav="${id}" type="button">
      ${icon}<span class="label">${label}</span>
    </button>
  `;
}
function navDisabled(icon, label) {
    return `
    <button class="nav-item nav-item--disabled" type="button" disabled title="Пока нет в API">
      ${icon}<span class="label">${label}</span>
    </button>
  `;
}
function renderAside(profile) {
    const p = profile ?? state.me;
    const aside = document.getElementById("aside-panel");
    if (!aside || !p)
        return;
    const interests = p.bio?.interests ?? [];
    const isOwn = Boolean(state.me && p.id === state.me.id);
    const canAddInterest = isOwn && interests.length < 10;
    aside.innerHTML = `
    <div style="display:flex;flex-direction:column;gap:1rem;">
      <section class="card" style="padding:1.1rem 1.15rem;">
        <div style="display:flex;align-items:center;gap:0.5rem;margin-bottom:1rem;font-weight:700;">
          ${icons.pulse} Статистика
        </div>
        <div class="stat-grid">
          <div class="stat-chip">
            <div class="stat-value">0</div>
            <div class="stat-label">Результатов</div>
          </div>
          <div class="stat-chip">
            <div class="stat-value">+0<span class="stat-trend">${icons.trendUp}</span></div>
            <div class="stat-label">Результатов за месяц</div>
          </div>
          <div class="stat-chip">
            <div class="stat-value">0</div>
            <div class="stat-label">Дней на платформе</div>
          </div>
          <div class="stat-chip">
            <div class="stat-value">0</div>
            <div class="stat-label">Направлений</div>
          </div>
        </div>
      </section>

      <section class="card aside-actions" style="padding:0.65rem;">
        <div style="display:flex;align-items:center;gap:0.5rem;padding:0.5rem 0.5rem 0.75rem;font-weight:700;">
          ${icons.stack} Действия
        </div>
        <button class="action-row" type="button" disabled title="Пока нет в API">
          <span style="display:inline-flex;align-items:center;gap:0.65rem;">${icons.tasks} Добавить результат</span>
          ${icons.chevron}
        </button>
        <button class="action-row" type="button" disabled title="Пока нет в API">
          <span style="display:inline-flex;align-items:center;gap:0.65rem;">${icons.path} Создать ветку развития</span>
          ${icons.chevron}
        </button>
      </section>

      <section class="card" style="padding:1.1rem 1.15rem;">
        <div style="display:flex;align-items:center;gap:0.5rem;margin-bottom:0.85rem;font-weight:700;">
          ${icons.bolt} Интересы
        </div>
        ${!isOwn && interests.length === 0
        ? `<div style="color:var(--text-muted);font-size:0.9rem;">Пока не указаны</div>`
        : `<div class="interest-list">
                ${interests
            .map((interest) => `<span class="interest-pill">${escapeHtml(interest)}</span>`)
            .join("")}
                ${isOwn
            ? `<button class="interest-add" type="button" data-add-interest ${canAddInterest ? "" : "disabled"} title="${canAddInterest ? "Добавить интерес" : "Нельзя добавить больше 10 интересов"}" aria-label="Добавить интерес">+</button>`
            : ""}
              </div>`}
      </section>
    </div>
  `;
    aside.querySelector("[data-add-interest]")?.addEventListener("click", () => openAddInterestModal());
}
function renderPostCard(post, currentUserId) {
    const mine = post.authorId === currentUserId;
    const media = mediaOf(post.authorId);
    return `
    <article class="card" style="padding:1.15rem 1.25rem;" data-post-id="${post.postId}">
      <div style="display:flex;gap:0.85rem;">
        ${avatarMarkup(post.authorId, post.authorDisplayName, "avatar--md", {
        button: true,
        openUser: escapeHtml(post.authorUsername),
        media,
    })}
        <div style="flex:1;min-width:0;">
          <div style="display:flex;align-items:center;gap:0.45rem;flex-wrap:wrap;">
            <button data-open-user="${escapeHtml(post.authorUsername)}" type="button" style="background:none;border:none;color:var(--text);font-weight:700;cursor:pointer;padding:0;">
              ${escapeHtml(post.authorDisplayName)}
            </button>
            <span style="color:var(--text-dim);font-size:0.85rem;">· ${formatRelativeTime(post.createdAtUtc)}</span>
            ${mine
        ? `<div style="margin-left:auto;display:flex;gap:0.35rem;">
                    <button class="btn-icon" data-edit-post="${post.postId}" title="Редактировать" type="button">${icons.edit}</button>
                    <button class="btn-icon" data-delete-post="${post.postId}" title="Удалить" type="button" style="color:#f87171;">${icons.close}</button>
                  </div>`
        : ""}
          </div>
          <p style="margin:0.65rem 0 1rem;white-space:pre-wrap;line-height:1.55;">${escapeHtml(post.content)}</p>
          <div class="post-actions">
            <button class="post-action ${post.likedByMe ? "liked" : ""}" data-like="${post.postId}" type="button">
              ${post.likedByMe ? icons.heartFilled : icons.heart}
              <span>${formatNumber(post.likesCount)}</span>
            </button>
            <button class="post-action" type="button" disabled title="Комментарии пока не в API">${icons.comment}</button>
            <button class="post-action" type="button" disabled title="Репосты пока не в API">${icons.repost}</button>
            <button class="post-action" type="button" disabled title="Закладки пока не в API" style="margin-left:auto;">${icons.bookmark}</button>
          </div>
        </div>
      </div>
    </article>
  `;
}
function bindPostEvents(container) {
    container.querySelectorAll("[data-open-user]").forEach((el) => {
        el.addEventListener("click", () => {
            const username = el.dataset.openUser;
            navigate(`u/${encodeURIComponent(username)}`);
        });
    });
    container.querySelectorAll("[data-like]").forEach((el) => {
        el.addEventListener("click", async () => {
            const postId = el.dataset.like;
            const post = state.feed.find((p) => p.postId === postId);
            if (!post)
                return;
            try {
                if (post.likedByMe) {
                    await api.unlikePost(postId);
                    post.likedByMe = false;
                    post.likesCount = Math.max(0, post.likesCount - 1);
                }
                else {
                    await api.likePost(postId);
                    post.likedByMe = true;
                    post.likesCount += 1;
                }
                await renderRoute();
            }
            catch (error) {
                handleError(error);
            }
        });
    });
    container.querySelectorAll("[data-delete-post]").forEach((el) => {
        el.addEventListener("click", async () => {
            const postId = el.dataset.deletePost;
            if (!confirm("Удалить этот пост?"))
                return;
            try {
                await api.deletePost(postId);
                state.feed = state.feed.filter((p) => p.postId !== postId);
                showToast("Пост удалён");
                await renderRoute();
            }
            catch (error) {
                handleError(error);
            }
        });
    });
    container.querySelectorAll("[data-edit-post]").forEach((el) => {
        el.addEventListener("click", () => {
            const postId = el.dataset.editPost;
            const post = state.feed.find((p) => p.postId === postId);
            if (!post)
                return;
            openEditPostModal(post);
        });
    });
}
function openEditPostModal(post) {
    const backdrop = document.createElement("div");
    backdrop.className = "modal-backdrop";
    backdrop.innerHTML = `
    <div class="card modal">
      <h2 style="margin:0 0 1rem;font-size:1.15rem;">Редактировать пост</h2>
      <div class="field">
        <textarea id="edit-post-content" maxlength="5000">${escapeHtml(post.content)}</textarea>
      </div>
      <div style="display:flex;justify-content:flex-end;gap:0.6rem;margin-top:1rem;">
        <button class="btn btn-ghost" type="button" data-cancel>Отмена</button>
        <button class="btn btn-primary" type="button" data-save>Сохранить</button>
      </div>
    </div>
  `;
    document.body.appendChild(backdrop);
    backdrop.querySelector("[data-cancel]")?.addEventListener("click", () => backdrop.remove());
    backdrop.addEventListener("click", (e) => {
        if (e.target === backdrop)
            backdrop.remove();
    });
    backdrop.querySelector("[data-save]")?.addEventListener("click", async () => {
        const content = backdrop.querySelector("#edit-post-content").value.trim();
        if (!content) {
            showToast("Текст поста не может быть пустым", "error");
            return;
        }
        try {
            await api.editPost(post.postId, content);
            post.content = content;
            backdrop.remove();
            showToast("Пост обновлён");
            await renderRoute();
        }
        catch (error) {
            handleError(error);
        }
    });
}
async function loadFeed(reset = false) {
    if (state.feedLoading)
        return;
    if (!reset && state.feedDone)
        return;
    state.feedLoading = true;
    try {
        if (reset) {
            state.feedPage = 1;
            state.feedDone = false;
            state.feed = [];
        }
        const page = await api.getFeed(state.feedPage, 20);
        state.feed = reset ? [...page] : [...state.feed, ...page];
        if (page.length < 20)
            state.feedDone = true;
        else
            state.feedPage += 1;
    }
    finally {
        state.feedLoading = false;
    }
}
function renderComposer(gradientPost = false) {
    return `
    <section class="card" style="padding:1.1rem 1.2rem;" id="composer">
      <textarea id="post-content" placeholder="Что нового сегодня?" maxlength="5000"
        style="width:100%;min-height:72px;background:transparent;border:none;outline:none;color:var(--text);resize:vertical;line-height:1.5;"></textarea>
      <div class="composer-toolbar">
        <button class="btn-icon" type="button" disabled title="Медиа пока не в API">${icons.photo}</button>
        <button class="btn-icon" type="button" disabled title="Ссылки пока не в API">${icons.link}</button>
        <button class="btn-icon" type="button" disabled title="Файлы пока не в API">${icons.file}</button>
        <button class="btn-icon" type="button" disabled title="Задачи пока не в API">${icons.tasks}</button>
        <button class="btn ${gradientPost ? "btn-gradient" : "btn-primary"}" id="create-post-btn" type="button">
          ${icons.send} Post
        </button>
      </div>
    </section>
  `;
}
async function renderFeedView() {
    const main = document.getElementById("main-content");
    if (state.feed.length === 0) {
        await loadFeed(true);
    }
    await ensureMediaMany(state.feed.map((p) => p.authorId));
    if (state.me)
        await ensureMedia(state.me.id);
    main.innerHTML = `
    <div style="display:flex;flex-direction:column;gap:1rem;">
      ${renderComposer()}
      <div id="feed-list" style="display:flex;flex-direction:column;gap:1rem;">
        ${state.feed.length === 0
        ? `<div class="card empty-state">Лента пуста. Напишите первый пост или подпишитесь на кого-нибудь.</div>`
        : state.feed.map((p) => renderPostCard(p, state.me.id)).join("")}
      </div>
      ${!state.feedDone && state.feed.length > 0
        ? `<button class="btn btn-ghost" id="load-more" type="button">Загрузить ещё</button>`
        : ""}
    </div>
  `;
    bindPostEvents(main);
    renderAside(state.me);
    main.querySelector("#create-post-btn")?.addEventListener("click", async () => {
        const textarea = main.querySelector("#post-content");
        const content = textarea.value.trim();
        if (!content) {
            showToast("Введите текст поста", "error");
            return;
        }
        const btn = main.querySelector("#create-post-btn");
        btn.disabled = true;
        try {
            await api.createPost(content);
            textarea.value = "";
            showToast("Пост опубликован");
            await loadFeed(true);
            await renderFeedView();
        }
        catch (error) {
            handleError(error);
        }
        finally {
            btn.disabled = false;
        }
    });
    main.querySelector("#load-more")?.addEventListener("click", async () => {
        try {
            await loadFeed(false);
            await renderFeedView();
        }
        catch (error) {
            handleError(error);
        }
    });
}
function renderProfileHeader(profile, isMe) {
    const following = state.followingIds.has(profile.id);
    const media = mediaOf(profile.id);
    const hasBanner = Boolean(media?.bannerUrl);
    return `
    <section class="card profile-card" style="overflow:hidden;">
      <div class="profile-banner ${hasBanner ? "profile-banner--image" : ""}" ${bannerStyle(media)}>
        ${isMe
        ? `<label class="banner-upload-btn" title="Изменить шапку">
                ${icons.photo}
                <span>Изменить шапку</span>
                <input id="banner-file" type="file" accept="image/jpeg,image/png,image/webp,image/gif" hidden />
              </label>`
        : ""}
      </div>
      <div style="padding:0 1.35rem 1.1rem;">
        <div class="profile-avatar-row">
          <div class="avatar-wrap">
            ${avatarMarkup(profile.id, profile.displayName, "avatar--xl", { media })}
            ${isMe
        ? `<label class="avatar-upload-btn" title="Изменить аватар">
                    ${icons.photo}
                    <input id="avatar-file" type="file" accept="image/jpeg,image/png,image/webp,image/gif" hidden />
                  </label>`
        : ""}
          </div>
          <div style="display:flex;gap:0.5rem;padding-bottom:0.35rem;">
            ${isMe
        ? `<button class="btn btn-ghost" id="edit-profile-btn" type="button">${icons.edit} Редактировать</button>`
        : `<button class="btn ${following ? "btn-ghost" : "btn-primary"}" id="follow-btn" type="button">
                    ${following ? "Снять оценку" : "Оценить"}
                  </button>`}
          </div>
        </div>
        <div style="margin-top:0.85rem;">
          <div style="display:flex;align-items:center;gap:0.4rem;">
            <h1 style="margin:0;font-size:1.55rem;letter-spacing:-0.02em;">${escapeHtml(profile.displayName)}</h1>
          </div>
          <div style="color:var(--text-muted);margin-top:0.15rem;">@${escapeHtml(profile.username)}</div>
          ${profile.bio.about
        ? `<div style="margin-top:0.55rem;font-weight:560;">${escapeHtml(profile.bio.about)}</div>`
        : ""}
          <div style="display:flex;gap:1.25rem;margin-top:0.85rem;font-size:0.95rem;">
            <div><strong>${formatNumber(profile.followersCount)}</strong> <span style="color:var(--text-muted);">ценителей</span></div>
            <div><strong>${formatNumber(profile.followingCount)}</strong> <span style="color:var(--text-muted);">ориентиров</span></div>
          </div>
          <div class="profile-skills">
            <div class="profile-skills__label">Навыки</div>
            <div class="profile-skills__list">
              ${profile.skills.length === 0
        ? `<span style="color:var(--text-muted);font-size:0.9rem;">Пока не указаны</span>`
        : profile.skills
            .map((skill) => `<span class="skill-pill">${escapeHtml(skill.name)} · ${escapeHtml(skill.level)}</span>`)
            .join("")}
              ${isMe
        ? `<button class="interest-add" type="button" data-add-skill ${profile.skills.length < 30 ? "" : "disabled"} title="${profile.skills.length < 30 ? "Добавить навык" : "Нельзя добавить больше 30 навыков"}" aria-label="Добавить навык">+</button>`
        : ""}
            </div>
          </div>
        </div>
      </div>
      <div class="tabs">
        <button class="tab tab--disabled" type="button" disabled title="Пока нет в API">Граф развития</button>
        <button class="tab tab--disabled" type="button" disabled title="Пока нет в API">Мой путь</button>
        <button class="tab ${state.profileTab === "posts" ? "active" : ""}" data-tab="posts" type="button">Посты</button>
        <button class="tab ${state.profileTab === "achievements" ? "active" : ""}" data-tab="achievements" type="button">Награды</button>
        <button class="tab ${state.profileTab === "about" ? "active" : ""}" data-tab="about" type="button">Обо мне</button>
      </div>
    </section>
  `;
}
const achievementGroupTitles = {
    FirstSteps: "Первые шаги",
    Regularity: "Регулярность",
    GoalsAndProgress: "Цели и прогресс",
    Community: "Сообщество",
    Special: "Особые",
};
function renderAchievementGroups(groups) {
    if (!groups || groups.length === 0) {
        return `<div class="card empty-state">Пока нет достижений</div>`;
    }
    return `
    <div class="achievement-groups">
      ${groups.map((group) => `
        <section>
          <h2 class="achievement-group-title">${escapeHtml(achievementGroupTitles[group.type] ?? group.type)}</h2>
          <div class="achievement-grid">
            ${group.achievements.map((item) => {
        const locked = !item.earnedOnUtc;
        return `
              <div class="card achievement-card${locked ? " achievement-card--locked" : ""}">
                <div style="font-weight:700;">${escapeHtml(item.name)}</div>
                <div style="color:var(--text-muted);font-size:0.9rem;margin-top:0.25rem;">${escapeHtml(item.description)}</div>
                <div style="color:var(--text-dim);font-size:0.8rem;margin-top:0.55rem;">
                  ${locked ? "Не получено" : new Date(item.earnedOnUtc).toLocaleDateString("ru-RU")}
                </div>
              </div>`;
    }).join("")}
          </div>
        </section>`).join("")}
    </div>
  `;
}
function renderProfileTabContent(profile, isMe, achievementGroups) {
    if (state.profileTab === "achievements") {
        return renderAchievementGroups(achievementGroups);
    }
    if (state.profileTab === "about") {
        return `
      <div class="card" style="padding:1.25rem;display:flex;flex-direction:column;gap:1.15rem;">
        <div>
          <div style="color:var(--text-muted);font-size:0.8rem;margin-bottom:0.35rem;">О себе</div>
          <div>${escapeHtml(profile.bio.about || "—")}</div>
        </div>
        <div>
          <div style="color:var(--text-muted);font-size:0.8rem;margin-bottom:0.35rem;">Комментарий</div>
          <div style="white-space:pre-wrap;">${escapeHtml(profile.bio.comment || "—")}</div>
        </div>
        <div>
          <div style="display:flex;align-items:center;justify-content:space-between;gap:0.75rem;margin-bottom:0.5rem;">
            <div style="color:var(--text-muted);font-size:0.8rem;">Навыки</div>
            ${isMe ? `<button class="btn btn-ghost" id="add-skill-btn" type="button" style="padding:0.35rem 0.7rem;font-size:0.85rem;">+ Навык</button>` : ""}
          </div>
          <div style="display:flex;flex-wrap:wrap;gap:0.45rem;">
            ${profile.skills.length === 0
            ? "—"
            : profile.skills
                .map((s) => `<span class="skill-pill">${escapeHtml(s.name)} · ${escapeHtml(s.level)}</span>`)
                .join("")}
          </div>
        </div>
      </div>
    `;
    }
    // posts tab — feed API returns own + following; filter by author when viewing a profile
    const posts = state.feed.filter((p) => p.authorId === profile.id);
    if (isMe) {
        return `
      <div style="display:flex;flex-direction:column;gap:1rem;">
        ${renderComposer(true)}
        <div id="profile-posts" style="display:flex;flex-direction:column;gap:1rem;">
          ${posts.length === 0
            ? `<div class="card empty-state">Пока нет постов в ленте</div>`
            : posts.map((p) => renderPostCard(p, state.me.id)).join("")}
        </div>
      </div>
    `;
    }
    return `
    <div id="profile-posts" style="display:flex;flex-direction:column;gap:1rem;">
      ${posts.length === 0
        ? `<div class="card empty-state">Нет постов этого пользователя в вашей ленте. Подпишитесь, чтобы видеть их здесь.</div>`
        : posts.map((p) => renderPostCard(p, state.me.id)).join("")}
    </div>
  `;
}
function alignProfileCard() {
    const profile = document.querySelector(".profile-card");
    const tabs = profile?.querySelector(".tabs");
    const aside = document.querySelector(".aside-panel");
    const actions = document.querySelector(".aside-actions");
    if (!profile || !tabs)
        return;
    tabs.style.marginTop = "0px";
    if (!aside || !actions || getComputedStyle(aside).display === "none")
        return;
    const delta = Math.round(actions.getBoundingClientRect().bottom - profile.getBoundingClientRect().bottom);
    if (delta > 0)
        tabs.style.marginTop = `${delta}px`;
}
let profileAlignBound = false;
function bindProfileAlign() {
    if (profileAlignBound)
        return;
    profileAlignBound = true;
    window.addEventListener("resize", () => alignProfileCard());
    document.fonts?.ready.then(() => alignProfileCard());
}
async function renderProfileView(username) {
    const main = document.getElementById("main-content");
    const isMe = !username || username === state.me?.username;
    const profile = isMe
        ? state.me
        : state.profile?.username === username
            ? state.profile
            : await api.getUserProfile(username);
    if (!isMe)
        state.profile = profile;
    else
        state.profile = profile;
    if (state.feed.length === 0) {
        try {
            await loadFeed(true);
        }
        catch {
            /* feed may fail independently */
        }
    }
    await ensureMedia(profile.id);
    await ensureMediaMany(state.feed.map((p) => p.authorId));
    let achievementGroups = null;
    if (state.profileTab === "achievements") {
        try {
            achievementGroups = await api.getAchievementGroups(profile.id);
        }
        catch (error) {
            handleError(error);
            achievementGroups = [];
        }
    }
    main.innerHTML = `
    <div style="display:flex;flex-direction:column;gap:1rem;">
      ${renderProfileHeader(profile, isMe)}
      ${renderProfileTabContent(profile, isMe, achievementGroups)}
    </div>
  `;
    renderAside(profile);
    bindProfileAlign();
    alignProfileCard();
    bindPostEvents(main);
    bindProfileMediaUploads(main, profile.id, () => renderProfileView(username));
    main.querySelectorAll("[data-tab]").forEach((el) => {
        el.addEventListener("click", async () => {
            state.profileTab = el.dataset.tab;
            await renderProfileView(username);
        });
    });
    main.querySelector("#edit-profile-btn")?.addEventListener("click", () => navigate("edit"));
    main.querySelector("#follow-btn")?.addEventListener("click", async () => {
        const following = state.followingIds.has(profile.id);
        try {
            if (following) {
                await api.unfollowUser(profile.id);
                state.followingIds.delete(profile.id);
                profile.followersCount = Math.max(0, profile.followersCount - 1);
                showToast("Оценка снята");
            }
            else {
                await api.followUser(profile.id);
                state.followingIds.add(profile.id);
                profile.followersCount += 1;
                showToast("Вы оценили");
                await loadFeed(true);
            }
            await renderProfileView(username);
        }
        catch (error) {
            if (error instanceof ApiError && error.status === 409) {
                state.followingIds.add(profile.id);
                await renderProfileView(username);
                return;
            }
            handleError(error);
        }
    });
    main.querySelector("#create-post-btn")?.addEventListener("click", async () => {
        const textarea = main.querySelector("#post-content");
        const content = textarea.value.trim();
        if (!content) {
            showToast("Введите текст поста", "error");
            return;
        }
        try {
            await api.createPost(content);
            showToast("Пост опубликован");
            await loadFeed(true);
            state.me = await api.getMyProfile();
            await renderProfileView(username);
        }
        catch (error) {
            handleError(error);
        }
    });
    main.querySelector("#add-skill-btn")?.addEventListener("click", () => openAddSkillModal());
    main.querySelector("[data-add-skill]")?.addEventListener("click", () => openAddSkillModal());
}
function bindProfileMediaUploads(rootEl, userId, onDone) {
    rootEl.querySelector("#avatar-file")?.addEventListener("change", async (event) => {
        const input = event.target;
        const file = input.files?.[0];
        if (!file)
            return;
        try {
            const result = await api.uploadAvatar(file);
            invalidateProfileMedia(userId);
            state.mediaByUserId.set(userId, result.media);
            showToast("Аватар обновлён");
            await onDone();
        }
        catch (error) {
            handleError(error);
        }
        finally {
            input.value = "";
        }
    });
    rootEl.querySelector("#banner-file")?.addEventListener("change", async (event) => {
        const input = event.target;
        const file = input.files?.[0];
        if (!file)
            return;
        try {
            const result = await api.uploadBanner(file);
            invalidateProfileMedia(userId);
            state.mediaByUserId.set(userId, result.media);
            showToast("Шапка обновлена");
            await onDone();
        }
        catch (error) {
            handleError(error);
        }
        finally {
            input.value = "";
        }
    });
}
function openAddSkillModal() {
    const backdrop = document.createElement("div");
    backdrop.className = "modal-backdrop modal-backdrop--blur";
    document.body.classList.add("overlay-blur");
    const close = () => {
        backdrop.remove();
        document.body.classList.remove("overlay-blur");
    };
    const levels = Object.entries(SkillLevelLabels);
    const options = levels
        .map(([value, label]) => `<button class="level-picker__option${value === "0" ? " is-selected" : ""}" type="button" data-value="${value}" role="option">${label}</button>`)
        .join("");
    backdrop.innerHTML = `
    <div class="card modal">
      <h2 style="margin:0 0 1rem;font-size:1.15rem;">Добавить навык</h2>
      <div style="display:flex;flex-direction:column;gap:0.85rem;">
        <div class="field">
          <label for="skill-name">Название</label>
          <input id="skill-name" maxlength="50" placeholder="например, C#" />
        </div>
        <div class="field">
          <label id="skill-level-label">Уровень</label>
          <div class="level-picker" id="skill-level" data-value="0">
            <button class="level-picker__value" type="button" aria-haspopup="listbox" aria-expanded="false" aria-labelledby="skill-level-label">${levels[0][1]}</button>
            <div class="level-picker__menu" role="listbox" hidden>
              ${options}
            </div>
          </div>
        </div>
      </div>
      <div style="display:flex;justify-content:flex-end;gap:0.6rem;margin-top:1rem;">
        <button class="btn btn-ghost" type="button" data-cancel>Отмена</button>
        <button class="btn btn-primary" type="button" data-save>Добавить</button>
      </div>
    </div>
  `;
    document.body.appendChild(backdrop);
    const picker = backdrop.querySelector("#skill-level");
    const pickerValue = picker.querySelector(".level-picker__value");
    const pickerMenu = picker.querySelector(".level-picker__menu");
    pickerValue.addEventListener("click", () => {
        const open = pickerMenu.hasAttribute("hidden");
        pickerMenu.toggleAttribute("hidden", !open);
        pickerValue.setAttribute("aria-expanded", open ? "true" : "false");
    });
    pickerMenu.querySelectorAll(".level-picker__option").forEach((option) => {
        option.addEventListener("click", () => {
            picker.dataset.value = option.dataset.value;
            pickerValue.textContent = option.textContent;
            pickerMenu.querySelectorAll(".level-picker__option").forEach((item) => {
                item.classList.toggle("is-selected", item === option);
            });
            pickerMenu.hidden = true;
            pickerValue.setAttribute("aria-expanded", "false");
        });
    });
    backdrop.querySelector("[data-cancel]")?.addEventListener("click", close);
    backdrop.addEventListener("click", (e) => {
        if (e.target === backdrop)
            close();
        else if (!picker.contains(e.target)) {
            pickerMenu.hidden = true;
            pickerValue.setAttribute("aria-expanded", "false");
        }
    });
    backdrop.querySelector("[data-save]")?.addEventListener("click", async () => {
        const name = backdrop.querySelector("#skill-name").value.trim();
        const level = Number(picker.dataset.value);
        if (!name) {
            showToast("Укажите название навыка", "error");
            return;
        }
        try {
            await api.addSkill({ name, level });
            state.me = await api.getMyProfile();
            close();
            showToast("Навык добавлен");
            await renderProfileView();
        }
        catch (error) {
            handleError(error);
        }
    });
}
function openAddInterestModal() {
    const me = state.me;
    if (!me)
        return;
    const interests = me.bio?.interests ?? [];
    if (interests.length >= 10) {
        showToast("Нельзя добавить больше 10 интересов", "error");
        return;
    }
    const backdrop = document.createElement("div");
    backdrop.className = "modal-backdrop modal-backdrop--blur";
    document.body.classList.add("overlay-blur");
    backdrop.innerHTML = `
    <div class="card modal">
      <h2 style="margin:0 0 1rem;font-size:1.15rem;">Добавить интерес</h2>
      <div class="field">
        <label for="interest-name">Интерес</label>
        <input id="interest-name" maxlength="50" placeholder="например, дизайн" />
      </div>
      <div style="display:flex;justify-content:flex-end;gap:0.6rem;margin-top:1rem;">
        <button class="btn btn-ghost" type="button" data-cancel>Отмена</button>
        <button class="btn btn-primary" type="button" data-save>Добавить</button>
      </div>
    </div>
  `;
    document.body.appendChild(backdrop);
    const close = () => {
        backdrop.remove();
        document.body.classList.remove("overlay-blur");
    };
    backdrop.querySelector("[data-cancel]")?.addEventListener("click", close);
    backdrop.addEventListener("click", (e) => {
        if (e.target === backdrop)
            close();
    });
    backdrop.querySelector("#interest-name")?.focus();
    backdrop.querySelector("[data-save]")?.addEventListener("click", async () => {
        const value = backdrop.querySelector("#interest-name").value.trim();
        if (!value) {
            showToast("Укажите интерес", "error");
            return;
        }
        const current = me.bio?.interests ?? [];
        if (current.some((interest) => interest.toLowerCase() === value.toLowerCase())) {
            showToast("Такой интерес уже есть", "error");
            return;
        }
        try {
            await api.updateProfile({
                displayName: me.displayName,
                about: me.bio.about,
                focusedOn: me.bio.focusedOn,
                comment: me.bio.comment ?? "",
                interests: [...current, value],
            });
            state.me = await api.getMyProfile();
            close();
            showToast("Интерес добавлен");
            await renderRoute();
        }
        catch (error) {
            handleError(error);
        }
    });
}
async function renderEditProfileView() {
    const main = document.getElementById("main-content");
    const me = state.me;
    await ensureMedia(me.id);
    const media = mediaOf(me.id);
    main.innerHTML = `
    <section class="card" style="padding:1.5rem;">
      <h1 style="margin:0 0 1.25rem;font-size:1.35rem;">Редактировать профиль</h1>

      <div class="media-edit-block">
        <div class="profile-banner ${media?.bannerUrl ? "profile-banner--image" : ""}" ${bannerStyle(media)} style="border-radius:0.85rem;${media?.bannerUrl ? "" : ""}">
          <label class="banner-upload-btn" title="Изменить шапку">
            ${icons.photo}
            <span>Изменить шапку</span>
            <input id="banner-file" type="file" accept="image/jpeg,image/png,image/webp,image/gif" hidden />
          </label>
        </div>
        <div class="avatar-wrap profile-avatar-row__avatar">
          ${avatarMarkup(me.id, me.displayName, "avatar--xl", { media })}
          <label class="avatar-upload-btn" title="Изменить аватар">
            ${icons.photo}
            <input id="avatar-file" type="file" accept="image/jpeg,image/png,image/webp,image/gif" hidden />
          </label>
        </div>
        <p style="color:var(--text-muted);font-size:0.85rem;margin:0 0 1rem;">
          JPEG, PNG, WebP или GIF. Аватар до 5 МБ, шапка до 8 МБ.
        </p>
      </div>

      <form id="edit-form" style="display:flex;flex-direction:column;gap:1rem;">
        <div class="field">
          <label for="displayName">Отображаемое имя</label>
          <input id="displayName" required minlength="2" maxlength="50" value="${escapeHtml(me.displayName)}" />
        </div>
        <div class="field">
          <label for="about">О себе (3–100 символов)</label>
          <input id="about" required minlength="3" maxlength="100" value="${escapeHtml(me.bio.about)}" />
        </div>
        <div class="field">
          <label for="comment">Комментарий</label>
          <textarea id="comment" maxlength="500">${escapeHtml(me.bio.comment ?? "")}</textarea>
        </div>
        <div style="display:flex;gap:0.65rem;justify-content:flex-end;margin-top:0.5rem;">
          <button class="btn btn-ghost" type="button" id="cancel-edit">Отмена</button>
          <button class="btn btn-gradient" type="submit">Сохранить</button>
        </div>
      </form>
    </section>
  `;
    renderAside(me);
    bindProfileMediaUploads(main, me.id, () => renderEditProfileView());
    main.querySelector("#cancel-edit")?.addEventListener("click", () => navigate("profile"));
    main.querySelector("#edit-form")?.addEventListener("submit", async (e) => {
        e.preventDefault();
        const displayName = main.querySelector("#displayName").value.trim();
        const about = main.querySelector("#about").value.trim();
        const focusedOn = me.bio.focusedOn;
        const comment = main.querySelector("#comment").value.trim();
        const interests = me.bio.interests ?? [];
        try {
            await api.updateProfile({ displayName, about, focusedOn, comment, interests });
            state.me = await api.getMyProfile();
            showToast("Профиль обновлён");
            navigate("profile");
        }
        catch (error) {
            handleError(error);
        }
    });
}
async function renderAppearanceView() {
    const main = document.getElementById("main-content");
    const options = await loadBackgroundOptions();
    const selectedId = getSelectedBackgroundId();
    const panelStyle = getPanelStyle();
    main.innerHTML = `
    <div style="display:flex;flex-direction:column;gap:1rem;">
      <section class="card" style="padding:1.35rem 1.4rem;">
        <h1 style="margin:0 0 0.35rem;font-size:1.35rem;">Оформление</h1>
        <p style="margin:0 0 1.25rem;color:var(--text-muted);font-size:0.92rem;">
          Выберите задний фон платформы
        </p>
        <div style="display:grid;grid-template-columns:repeat(auto-fill,minmax(200px,1fr));gap:1rem;">
          ${options
        .map((option) => {
        const selected = option.id === selectedId;
        const previewStyle = option.type === "image"
            ? `style="background-image:url('${escapeHtml(option.url)}')"`
            : "";
        const previewClass = option.type === "image"
            ? "bg-option__preview bg-option__preview--image"
            : "bg-option__preview bg-option__preview--default";
        return `
                <button class="bg-option ${selected ? "is-selected" : ""}" type="button" data-bg-id="${escapeHtml(option.id)}">
                  <div class="${previewClass}" ${previewStyle}></div>
                  <div class="bg-option__meta">
                    <span style="font-weight:600;font-size:0.92rem;">${escapeHtml(option.name)}</span>
                    ${selected ? `<span class="bg-option__check">${icons.check}</span>` : ""}
                  </div>
                </button>
              `;
    })
        .join("")}
        </div>
      </section>

      <section class="card" style="padding:1.35rem 1.4rem;">
        <h2 style="margin:0 0 0.35rem;font-size:1.15rem;">Плашки</h2>
        <p style="margin:0 0 1.1rem;color:var(--text-muted);font-size:0.92rem;">
          Как выглядят карточки и панели интерфейса
        </p>
        <div style="display:grid;grid-template-columns:repeat(auto-fill,minmax(220px,1fr));gap:1rem;">
          <button class="panel-style-option ${panelStyle === "solid" ? "is-selected" : ""}" type="button" data-panel-style="solid">
            <div class="panel-style-option__preview panel-style-option__preview--solid">
              <div class="panel-style-option__preview-inner"></div>
            </div>
            <div>
              <div class="panel-style-option__title">Плотные</div>
              <div class="panel-style-option__desc">Тёмный непрозрачный фон</div>
            </div>
          </button>
          <button class="panel-style-option ${panelStyle === "outline" ? "is-selected" : ""}" type="button" data-panel-style="outline">
            <div class="panel-style-option__preview panel-style-option__preview--outline">
              <div class="panel-style-option__preview-inner"></div>
            </div>
            <div>
              <div class="panel-style-option__title">Контур</div>
              <div class="panel-style-option__desc">Прозрачный фон, остаётся только обводка</div>
            </div>
          </button>
          <button class="panel-style-option ${panelStyle === "glass" ? "is-selected" : ""}" type="button" data-panel-style="glass">
            <div class="panel-style-option__preview panel-style-option__preview--glass">
              <div class="panel-style-option__preview-inner"></div>
            </div>
            <div>
              <div class="panel-style-option__title">Стеклянные</div>
              <div class="panel-style-option__desc">Полупрозрачный фон с размытием</div>
            </div>
          </button>
        </div>
      </section>

      <div class="appearance-reset">
        <button class="btn btn-ghost" id="reset-appearance" type="button">Сбросить настройки оформления</button>
      </div>
    </div>
  `;
    renderAside(state.me);
    main.querySelector("#reset-appearance")?.addEventListener("click", async () => {
        try {
            await resetAppearance();
            showToast("Настройки оформления сброшены");
            renderShell();
            await renderAppearanceView();
        }
        catch (error) {
            handleError(error);
        }
    });
    main.querySelectorAll("[data-bg-id]").forEach((el) => {
        el.addEventListener("click", async () => {
            const id = el.dataset.bgId;
            try {
                await selectBackground(id);
                showToast("Фон обновлён");
                await renderAppearanceView();
            }
            catch (error) {
                handleError(error);
            }
        });
    });
    main.querySelectorAll("[data-panel-style]").forEach((el) => {
        el.addEventListener("click", () => {
            const style = el.dataset.panelStyle;
            selectPanelStyle(style);
            const panelStyleLabels = {
                solid: "Плашки: плотные",
                outline: "Плашки: контур",
                glass: "Плашки: стеклянные",
            };
            showToast(panelStyleLabels[style] ?? "Плашки обновлены");
            void renderAppearanceView();
        });
    });
}
async function renderRoute() {
    renderShell();
    const route = parseHash();
    const main = document.getElementById("main-content");
    main.innerHTML = `<div class="card empty-state">Загрузка…</div>`;
    try {
        if (!state.me) {
            state.me = await api.getMyProfile();
        }
        if (route.view === "edit") {
            await renderEditProfileView();
            return;
        }
        if (route.view === "appearance") {
            await renderAppearanceView();
            return;
        }
        if (route.view === "profile") {
            state.profileTab = "posts";
            await renderProfileView();
            return;
        }
        if (route.view === "u" && route.param) {
            state.profileTab = "posts";
            await renderProfileView(decodeURIComponent(route.param));
            return;
        }
        await renderFeedView();
    }
    catch (error) {
        handleError(error);
        main.innerHTML = `<div class="card empty-state">Не удалось загрузить данные</div>`;
    }
}
window.addEventListener("hashchange", () => {
    void renderRoute();
});
void renderRoute();
