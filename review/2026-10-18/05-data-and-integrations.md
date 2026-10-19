# 05 — Данные и интеграции

## Базы данных

### Владение данными

| База | Сервис-владелец | Основные таблицы | Миграции |
|------|----------------|------------------|----------|
| `venture_users` | Users | `users` (+ skills как owned entity) | 2 миграции (InitialCreate, RemoveUserAchievements) |
| `venture_posts` | Posts | `posts`, `post_media`, `post_likes` | Миграции в EF Core |
| `venture_social` | Social | `follows` | Миграции в EF Core |
| `venture_achievements` | Achievements | `achievements`, `user_achievement_progress`, `earned_achievements`, `processed_facts` | 4 миграции (InitialCreate + 3 эволюционных) |
| `venture_notifications` | Notifications | `notifications` | 1 миграция |

### Паттерны работы с данными

**EF Core** — все сервисы используют EF Core 10.0.12 с Npgsql.

**VentureDbContext** — каждый сервис имеет свой `VentureDbContext`, реализующий `IUnitOfWork` и `IReadDbContext`.

**AsNoTracking** — правильно используется для read-side:
```csharp
// Users.Infrastructure/Persistence/VentureDbContext.cs:16
IQueryable<User> IReadDbContext.Users => Users.AsNoTracking();
```

**Транзакции** — управляются через `TransactionBehavior` (только для команд). Запросы проходят без транзакции.

**Миграции** — применяются автоматически при старте сервиса (`InitializeDatabaseAsync`). Каталог достижений seed'ится в `AchievementCatalogSeeder`.

### Idempotency в Achievements

Achievements использует таблицу `processed_facts` для дедупликации:
- `ProcessedFact` хранит `EventId` (string)
- Конфигурация в `ProcessedFactConfiguration` определяет unique index
- Перед обработкой факта проверяется наличие EventId в таблице

**Оценка**: ✅ Корректная реализация idempotency для Kafka consumer.

### Потенциальные проблемы с данными

**Race condition при регистрации**: `IsUsernameTakenAsync` и `IsEmailTakenAsync` — это SELECT до INSERT. Между проверкой и INSERT может прийти параллельный запрос. Защита: unique-индекс в БД → EF Core кинет exception → 500 вместо 409 Conflict. Не критично, но UX плохой.

**N+1 в ленте**: `GetFeedQueryHandler` (строки 68-71) использует подзапрос для подсчёта лайков и проверки "лайкнул ли я":
```csharp
LikesCount = readDbContext.PostLikes.Count(like => like.PostId == post.Id),
LikedByMe = readDbContext.PostLikes.Any(like => like.PostId == post.Id && like.UserId == currentUserId)
```
EF Core 10 транслирует это в correlated subqueries. Это **не N+1**, а один SQL-запрос с подзапросами. Для малого объёма данных приемлемо. При росте нужен materialized view или денормализация.

## Kafka-топики

| Топик | Продюсер | Потребитель | Формат ключа | Формат значения |
|-------|----------|-------------|-------------|-----------------|
| `venture.notifications` | Users, Posts, Social | Notifications | `recipientId` (Guid) | JSON: `{recipientId, type, actorId, referenceId, createdAtUtc}` |
| `venture.posts.post-published` | Posts | Achievements | `authorId` (Guid) | JSON: `{schemaVersion, eventId, occurredAtUtc, postId, authorId}` |
| `venture.social.user-followed` | Social | Achievements | `followingId` (Guid) | JSON: `{schemaVersion, eventId, occurredAtUtc, followerId, followingId}` |

### Контрактная совместимость

Контракты между продюсерами и потребителями проверяются **архитектурным тестом** `FactContractTests`:
- Проверяет, что JSON-поля в тестах продюсера и потребителя совпадают
- Проверяет конкретные значения тестовых GUID

**Оценка**: ✅ Редко встречающийся паттерн — contract testing через cross-service architecture tests. Хорошее решение.

### Schema versioning

Факты в топиках `post-published` и `user-followed` имеют `schemaVersion`. `FactParser` проверяет `schemaVersion == 1`, при несовпадении бросает `UnsupportedFactSchemaException`.

**Проблема**: при получении сообщения с неизвестной версией, offset не коммитится, consumer перезапускается → бесконечный retry на сообщении с новой версией, пока не обновят код.

## Redis

| Сервис | Использование | Prefix | TTL |
|--------|--------------|--------|-----|
| Users | Кэш профиля | `Venture:` | 5 минут |
| Posts | Кэш ленты + feed version counter | `Venture:` | Настраиваемый |
| Social | Кэш (предполагается) | `Venture:` | — |

### Инвалидация кэша

- При публикации поста: `InvalidateCacheOnPostPublishedHandler` увеличивает `feed version counter` для автора → старый кэш ленты становится невалидным
- При лайке: `ICacheInvalidator.InvalidateUserFeedAsync` инвалидирует кэш ленты лайкнувшего
- При изменении профиля: `UserProfileChangedDomainEvent` → инвалидация кэша профиля

**Стратегия**: версионированный кэш ленты (counter-based). При изменении данных увеличивается версия, и новые запросы идут мимо старого кэша.

**Оценка**: ✅ Грамотная стратегия инвалидации, избегающая thundering herd.

## gRPC контракты

### users.proto
- `UserExists(UserIdRequest) → ExistsReply`
- `GetUserCard(UserIdRequest) → UserCardReply`
- `GetUserCards(UserIdsRequest) → UserCardsReply`

### social.proto
- `GetFollowStats(UserIdRequest) → FollowStatsReply`
- `GetFollowingIds(UserIdRequest) → FollowingIdsReply`

### achievements.proto
- `GetUserAchievements(UserIdRequest) → UserAchievementsReply`

Proto-файлы расположены в `<Service>.Api/Protos/` и подключаются в потребителях через relative path в `.csproj`.
