# Venture

Социальная сеть: профиль, посты, подписки, лайки и достижения. Браузер говорит только со шлюзом `http://localhost:5227`. Шлюз отдаёт страницы и пересылает API в нужный сервис.

Рабочий код лежит в `services/`. У каждого сервиса своё решение `services/<Имя>/<Имя>.slnx`. Корневой `Venture.slnx` — старый монолит, его не открывать.

## Как запустить

Нужны .NET 10 SDK, PostgreSQL (`localhost:5432`, пользователь `postgres`, пароль `postgres`), Redis (`localhost:6379`) и Kafka (`localhost:9092`).

Kafka из корня репозитория:

```bash
docker compose up -d
```

Пароль другой — поменяйте `ConnectionStrings:Database` в `appsettings.Development.json` каждого API. При старте сервис сам создаёт свою базу и накатывает миграции. Базы: `venture_users`, `venture_posts`, `venture_social`, `venture_achievements`, `venture_notifications`.

Шесть терминалов, в каждом из корня репозитория:

```bash
dotnet run --project services/Users/Users.Api --launch-profile http
dotnet run --project services/Posts/Posts.Api --launch-profile http
dotnet run --project services/Social/Social.Api --launch-profile http
dotnet run --project services/Achievements/Achievements.Api --launch-profile http
dotnet run --project services/Notifications/Notifications.Api --launch-profile http
dotnet run --project services/Gateway/Venture.Gateway --launch-profile http
```

Откройте `http://localhost:5227`. Сначала регистрация, потом лента.

Тесты одного сервиса: `dotnet test services/Users/Users.slnx` (так же для Posts, Social, Achievements, Notifications).

Границы сервисов и разделение баз: `dotnet test services/ArchitectureTests/ArchitectureTests.slnx`.

## Что можно сделать в интерфейсе

Страницы: `index.html` (лента и свой профиль), `login.html`, `register.html`. Все вызовы к API собраны в `services/Gateway/Venture.Gateway/wwwroot/js/api.js`. Токен лежит в браузере и уходит заголовком `Authorization`.

- Зарегистрироваться и войти.
- Смотреть и править свой профиль: текст, навыки, аватар, шапка.
- Смотреть чужой профиль по имени пользователя.
- Писать, менять и удалять свои посты.
- Лайкать и снимать лайк.
- Подписываться и отписываться.
- Читать ленту: свои посты и посты тех, на кого вы подписаны.

В интерфейсе нет списка уведомлений. Они только записываются в базу `venture_notifications`.

Достижения «первый пост» и «первый подписчик» выдаются сами, отдельной кнопки нет.

## Где что лежит

| Сервис | Порт | Своя база | За что отвечает |
|--------|------|-----------|-----------------|
| Gateway | 5227 | нет | Страницы и маршруты. Код маршрутов: `services/Gateway/Venture.Gateway/appsettings.json` |
| Users | 5231, gRPC 6231 | `venture_users` | Аккаунт, профиль, навыки, JWT. Достижения в профиле читает из Achievements |
| Posts | 5232 | `venture_posts` | Посты, лайки, лента |
| Social | 5233, gRPC 6233 | `venture_social` | Подписки |
| Achievements | 5234, gRPC 6234 | `venture_achievements` | Каталог, прогресс и выдача достижений. Наружу не торчит |
| Notifications | 5235 | `venture_notifications` | Читает топик `venture.notifications` и записывает уведомление. Наружу не торчит |

Внутри Users, Posts, Social, Achievements и Notifications папки одинаковые:

```
services/Users/
├── Users.Api              # HTTP: эндпоинт принимает запрос и вызывает команду
├── Users.Application      # хендлер, проверки, события
├── Users.Domain           # правила агрегата
├── Users.Infrastructure   # база, Redis, gRPC и HTTP к другим сервисам
├── Users.Domain.Tests
├── Users.Application.Tests
└── Users.IntegrationTests
```

Куда смотреть, если открыли конкретное действие:

1. Эндпоинт — `*.Api/Endpoints/...`.
2. Хендлер — рядом с командой, например `Users.Application/Users/Commands/RegisterUser/`.
3. Правило «можно или нельзя» — агрегат в `*.Domain`.
4. Вызов другого сервиса — `*.Infrastructure/Integration/GrpcClients.cs`. Social по-прежнему проверяет пользователя в Users через `HttpClients.cs`.

Сервисы не ссылаются друг на друга проектами. Чужие данные, которые нужны сразу, они берут по gRPC на портах 6231, 6233 и 6234. JSON для шлюза остаётся на 5231, 5233 и 5234. Social всё ещё спрашивает карточку пользователя у Users по HTTP. События после коммита уходят в Kafka.

Шлюз направляет так:

- `/users/{id}/follow` → Social
- `/users` и `/assets/profiles` → Users
- `/posts` и `/feed` → Posts

## Как идёт один запрос

Один и тот же путь во всех пяти API.

1. Шлюз пересылает HTTP.
2. `ExceptionHandlingMiddleware`, затем проверка JWT (её нет у Achievements и Notifications).
3. Эндпоинт вызывает `ISender.Send`.
4. MediatR по порядку: `LoggingBehavior` → `PerformanceBehavior` → `ValidationBehavior` → `TransactionBehavior`.
5. Хендлер делает проверки и меняет агрегат в памяти.
6. `TransactionBehavior` сохраняет базу и коммитит.
7. Ответ собирается из `Result`: успех или 400 / 401 / 403 / 404 / 409.

`TransactionBehavior` есть только у команд. Запросы вроде ленты и профиля его пропускают: они ничего не сохраняют.

Доменное событие срабатывает внутри `SaveChanges`, ещё до коммита. Обработчик события может сбросить Redis сразу. Сообщение в другой сервис он почти всегда только кладёт в очередь `PostCommitActions`. После коммита отправитель публикует его в Kafka. Получатель читает топик и снова проходит весь путь с пункта 2.

Запросы и команды лежат в `*.Application`. Команда меняет данные (`CreatePostCommand`). Запрос только читает (`GetFeedQuery`).

## Что происходит в основных сценариях

**Регистрация.** `POST /users` → `RegisterUserCommandHandler` создаёт `User`. Событие `UserRegistered` после коммита публикует в `venture.notifications` сообщение `Welcome`. Notifications читает топик и пишет запись.

**Вход.** `POST /users/login` → `LoginUserCommandHandler` проверяет пароль и возвращает JWT. Пишет в базу ничего.

**Новый пост.** `POST /posts` → `CreatePostCommandHandler` спрашивает Users, есть ли автор, и публикует пост. После коммита Posts пишет факт в `venture.posts.post-published`. Achievements сам решает, выдавать ли «Первый пост».

**Лайк.** `LikePostCommandHandler` пишет лайк в базу постов. Если лайк чужой, после коммита в `venture.notifications` уходит `PostLiked`.

**Подписка.** Шлюз ведёт `POST /users/{id}/follow` в Social. `FollowUserCommandHandler` проверяет в Users, что цель существует, и создаёт `Follow`. После коммита Social пишет `UserFollowed` в `venture.notifications` и факт в `venture.social.user-followed`. Achievements сам решает, выдавать ли «Первый подписчик».

**Лента.** `GET /feed` → `GetFeedQueryHandler`. Сначала Redis. Если пусто: список подписок из Social, посты из своей базы, имена авторов из Users.

**Профиль.** `GET /users/me` и `GET /users/{username}` оба идут в `UserProfileReader`: свои колонки из Users, счётчики подписок из Social по gRPC. Достижения читаются отдельно вызовом `GetUserAchievements` и в Redis не пишутся. Остальной профиль кэшируется на 5 минут. Если Achievements не ответил за 2 секунды, профиль возвращается с пустым массивом достижений.

Аватар и шапка идут мимо MediatR: `UploadProfileMediaEndpoint` сразу пишет файл, шлюз отдаёт его с Users по пути `/assets/profiles`.

## Сообщения

Вопрос, на который нужен ответ сразу, идёт по gRPC (порты 6231, 6233, 6234): есть ли пользователь, счётчики, карточки, список подписок, значки профиля. Карточку пользователя для Social Users по-прежнему отдаёт по HTTP на 5231. Kafka забирает то, что можно сделать после коммита.

| Топик | Кто пишет | Кто читает | Что внутри |
|--------|-----------|------------|------------|
| `venture.notifications` | Users, Posts, Social | Notifications | `recipientId`, `type` (`Welcome`, `UserFollowed`, `PostLiked`), `actorId`, `referenceId`, `createdAtUtc` |
| `venture.posts.post-published` | Posts | Achievements | `schemaVersion`, `eventId` (это `postId`), `occurredAtUtc`, `postId`, `authorId`. Ключ — `authorId` |
| `venture.social.user-followed` | Social | Achievements | `schemaVersion`, `eventId` (`followingId:followerId`), `occurredAtUtc`, `followerId`, `followingId`. Ключ — `followingId` |

Ключ сообщения — id пользователя, которого касается событие. Читатель коммитит смещение только после успешной обработки. Публикация идёт уже после коммита своей базы: если брокер в этот момент недоступен, запрос завершится ошибкой, а сообщение в топик не попадёт. Адрес брокера: `Kafka:BootstrapServers`. Группы: `venture-users` и `venture-notifications`.

## Чего в коде ещё нет

- Экрана и API «мои уведомления». Тип `AchievementEarned` объявлен, но никто его не пишет. Метод `MarkAsRead` тоже никуда не подключён.
- Redis нужен Users, Posts и Social. Без него эти три сервиса не работают. Префикс ключей: `Venture:`.
- Секрет JWT в `appsettings.json` общий и только для локального запуска. Токен живёт 60 минут.
