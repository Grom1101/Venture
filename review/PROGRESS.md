# Журнал переноса достижений

## Текущий этап

Код этапов 1–4 подготовлен. Таблица `venture_users.UserAchievements` не удалялась и миграция удаления не входит в сборку.

## Что сделано

### Этап 1. Новые факты рядом со старыми командами

Posts после коммита публикует `venture.posts.post-published`. Social после коммита публикует `venture.social.user-followed`. Оба вызова стоят в существующем `PostCommitActions`, время факта ставится в момент отправки. `eventId` поста — `postId`. `eventId` подписки — `{followingId}:{followerId}`: в тексте документа пара названа без порядка, пример JSON фиксирует `followingId` первым. Ключи Kafka: `authorId` и `followingId`.

Старые топики на этом этапе ещё публиковались. На этапе 4 они удалены вместе с читателем в Users.

### Этап 2. Achievements владеет выдачей

- Свои record-типы `PostPublishedMessage` и `UserFollowedMessage` в Infrastructure.
- Consumer `AchievementFactsConsumer`, группа `venture-achievements`, `AutoOffsetReset.Earliest`, автокоммит выключен.
- Нечитаемый JSON и пустое сообщение пропускаются со сдвигом смещения. `schemaVersion`, отличный от 1, пишется в лог и смещение не двигает. Ошибка базы смещение не двигает: внешний цикл читает то же сообщение снова.
- Таблицы `UserAchievementStates`, `EarnedAchievements` (первичный ключ `UserId` + `AchievementId`), `ProcessedFacts` (`EventId`).
- Миграция `20261012153234_AddUserAchievementProgress` создана и в базу не применялась.
- Повтор `EventId` не увеличивает счётчик. Первый принятый факт выдаёт значок, следующий другой факт того же вида не выдаёт его снова. Факт подписки не выдаёт значок поста.
- На PostgreSQL строка прогресса блокируется `SELECT ... FOR UPDATE`, чтобы параллельные факты одного пользователя не теряли приращение. Уникальный ключ выдачи остаётся защитой от второй строки.
- Перенос старых строк из `UserAchievements` не делается: среда разработки, исторические выдачи не нужны. Классы копирования удалены.

### Этап 3. Профиль читает контракт показа

`GET internal/users/{userId}/achievements` возвращает `id`, `type`, `name`, `description`, `earnedOnUtc`, сортировка по дате выдачи по убыванию, пустой массив если выдач нет.

`UserProfileReader` и оба query handler больше не собирают достижения из локальной таблицы и каталога по id. Остальной профиль по-прежнему кэшируется на 5 минут. После чтения из кэша список достижений запрашивается снова. Таймаут клиента `AchievementsDisplay` — 2 секунды. Таймаут и ошибка HTTP дают пустой массив и запись в лог, профиль при этом успешен. `app.js` не менялся.

### Этап 4. Старый путь снят, таблица оставлена

Поиск по `services/Users`, `services/Posts` и `services/Social` больше не находит `enum AchievementType`, `GrantFirstPostAchievement`, `GrantFirstFollowerAchievement`, `venture.achievements.first-post`, `venture.achievements.first-follower`, `RequestFirstPostAchievement` и `RequestFirstFollowerAchievement`.

Удалены обработчики выдачи Users, коллекция и методы `GrantAchievement` / `HasAchievement`, consumer Users, старые сообщения и топики, команды Posts и Social, порты `IAuthorPostCount`, `IFollowerCount`, `IAchievementCatalog` и их HTTP-клиенты. `IFollowStats` сохранён. Внутренние `GET by-type` и `GET by ids` удалены: после снятия клиента Users вызовов не осталось. Внутренний счётчик постов Posts и счётчик подписчиков Social не удалялись.

Mapping `UserAchievements` снят с текущей модели EF. Старая миграция Users по-прежнему создаёт эту таблицу, а отдельной миграции удаления в сборке нет, поэтому старт Users её не трогает. Пустая таблица в уже созданной базе разработки просто не используется.

## Проверки

Пройдены, код выхода 0:

- `dotnet test services/Users/Users.slnx` — Domain 16, Application 10, Integration 7
- `dotnet test services/Posts/Posts.slnx` — Domain 11, Application 4. В IntegrationTests нет тестов, так было и раньше
- `dotnet test services/Social/Social.slnx` — Domain 3, Application 9. В IntegrationTests нет тестов
- `dotnet test services/Achievements/Achievements.slnx` — Domain 9, Application 3, Integration 21
- `dotnet test services/ArchitectureTests/ArchitectureTests.slnx` — 6

Интеграционные проверки Achievements и профиля Users шли на SQLite в памяти, не на PostgreSQL и не на Kafka. Миграция к PostgreSQL не применялась, брокер не поднимался. Отдельной CI-конфигурации в репозитории нет: команда ArchitectureTests записана в README.

## Откат

- Выложить предыдущие Users, Posts и Social. Таблица `UserAchievements` на месте, старый бинарник снова умеет её читать и писать.
- Consumer Achievements можно остановить. Новые таблицы можно не удалять.

## Оставшиеся ограничения

- Между коммитом поста или подписки и публикацией в Kafka окно потери факта остаётся. Outbox в этот переход не входил.
- Счётчик — это число фактов, принятых после появления топика. Это не архив всех старых постов.
- Если Achievements недоступен, профиль жив, а список достижений пуст.
- Параллельная обработка одного пользователя на PostgreSQL закрыта блокировкой строки. Этот путь не прогонялся на живом PostgreSQL: тесты шли на SQLite, где `FOR UPDATE` нет, а уникальность выдачи и повтор `EventId` проверялись последовательно.
- `schemaVersion`, который Achievements не понимает, останавливает раздел Kafka до появления читателя этой версии. Отдельной dead-letter очереди нет.
