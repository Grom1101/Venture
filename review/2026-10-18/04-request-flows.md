# 04 — Трассировка запросов

## Каталог сценариев

| # | Сценарий | Тип | Сервисы | Документ |
|---|----------|-----|---------|----------|
| 1 | Регистрация пользователя | Команда, cross-service (Kafka) | Users → Notifications | [01-register-user.md](flows/01-register-user.md) |
| 2 | Создание поста | Команда, cross-service (gRPC + Kafka) | Posts → Users → Achievements | [02-create-post.md](flows/02-create-post.md) |
| 3 | Подписка на пользователя | Команда, cross-service (HTTP + Kafka) | Social → Users → Achievements + Notifications | [03-follow-user.md](flows/03-follow-user.md) |
| 4 | Чтение ленты | Запрос, multi-service (gRPC + Redis) | Posts → Social + Users | [04-get-feed.md](flows/04-get-feed.md) |
| 5 | Чтение профиля | Запрос, multi-service (gRPC) | Users → Social + Achievements | Описан ниже |

## Сценарий 1: Регистрация пользователя

```mermaid
sequenceDiagram
    participant Browser
    participant Gateway
    participant UsersAPI as Users.Api
    participant Middleware as ExceptionHandling
    participant MediatR
    participant Validation as ValidationBehavior
    participant Transaction as TransactionBehavior
    participant Handler as RegisterUserCommandHandler
    participant Domain as User.Register()
    participant DB as PostgreSQL
    participant SaveChanges as VentureDbContext.SaveChanges
    participant DomainEvents as DomainEventDispatcher
    participant EventHandler as UserRegisteredHandler
    participant PostCommit as PostCommitActions
    participant Kafka

    Browser->>Gateway: POST /users {email, username, displayName, password}
    Gateway->>UsersAPI: YARP proxy
    UsersAPI->>Middleware: ExceptionHandlingMiddleware.InvokeAsync
    Middleware->>MediatR: ISender.Send(RegisterUserCommand)
    MediatR->>Validation: LoggingBehavior → PerformanceBehavior → ValidationBehavior
    Validation->>Transaction: TransactionBehavior.Handle
    Transaction->>DB: BEGIN TRANSACTION
    Transaction->>Handler: next()
    Handler->>Handler: Email.Create, Username.Create, DisplayName.Create
    Handler->>DB: IsUsernameTakenAsync, IsEmailTakenAsync
    Handler->>Handler: PasswordHash.FromHash(hash(password))
    Handler->>Domain: User.Register(...)
    Domain->>Domain: AddDomainEvent(UserRegisteredDomainEvent)
    Handler->>Handler: userRepository.Add(user)
    Handler-->>Transaction: Result.Success(userId)
    Transaction->>SaveChanges: SaveChangesAsync
    SaveChanges->>DB: INSERT INTO users
    SaveChanges->>DomainEvents: DispatchAsync([UserRegisteredDomainEvent])
    DomainEvents->>EventHandler: Publish(DomainEventNotification<UserRegistered>)
    EventHandler->>PostCommit: Enqueue(KafkaPublish notification)
    Transaction->>DB: COMMIT
    Transaction->>PostCommit: RunAsync
    PostCommit->>Kafka: PublishAsync("venture.notifications", Welcome)
    UsersAPI-->>Gateway: 201 Created {userId}
    Gateway-->>Browser: 201 Created
```

### Путь выполнения

**A. Точка входа**
- `POST /users` — `RegisterUserEndpoint` (`Users.Api/Endpoints/Users/RegisterUserEndpoint.cs:11-25`)
- AllowAnonymous — нет JWT-проверки
- Request: `{email, username, displayName, password}`

**B. MediatR Pipeline**
1. `LoggingBehavior` — логирует начало/конец (`Users.Application/Behaviors/LoggingBehavior.cs`)
2. `PerformanceBehavior` — замеряет время (`Users.Application/Behaviors/PerformanceBehavior.cs`)
3. `ValidationBehavior` — запускает `RegisterUserCommandValidator` (FluentValidation)
4. `TransactionBehavior` — открывает транзакцию, вызывает handler, SaveChanges, Commit, PostCommit

**C. Обработчик**
- `RegisterUserCommandHandler` (`Users.Application/Users/Commands/RegisterUser/RegisterUserCommandHandler.cs:11-55`)
- Создаёт value objects: `Email`, `Username`, `DisplayName`
- Проверяет уникальность username и email через `IUserRepository`
- Хеширует пароль через `IPasswordHasher` (PBKDF2, 100k итераций, SHA-256)
- Вызывает `User.Register()` — доменный агрегат

**D. Домен**
- `User.Register()` (`Users.Domain/Users/User.cs:60-96`)
- Проверяет null-параметры
- Генерирует `UserId.New()` (Guid)
- Регистрирует `UserRegisteredDomainEvent`

**E. Сохранение и события**
- `TransactionBehavior` вызывает `SaveChangesAsync` → `VentureDbContext` переопределяет его
- `VentureDbContext.SaveChangesAsync` (`Users.Infrastructure/Persistence/VentureDbContext.cs:29-48`):
  1. Собирает domain events
  2. Выполняет `base.SaveChangesAsync()` — INSERT в БД
  3. Публикует events через `DomainEventDispatcher`
  4. Если появились новые изменения — повторный SaveChanges
- Event handler для `UserRegisteredDomainEvent` ставит в `PostCommitActions` задачу записи в Kafka
- После коммита транзакции `PostCommitActions.RunAsync()` публикует `Welcome` в `venture.notifications`

**F. Риски**
- Если Kafka недоступен: пользователь создан, но Welcome-уведомление потеряно
- Race condition на уникальность username/email: проверка и INSERT не атомарны (но unique-индекс в БД защищает от дублей через exception)

---

## Сценарий 5: Чтение профиля (краткое описание)

**Путь**: `GET /users/me` или `GET /users/{username}` → Users.Api

```mermaid
sequenceDiagram
    participant Browser
    participant Gateway
    participant UsersAPI as Users.Api
    participant Handler as GetMyProfileQueryHandler
    participant Reader as UserProfileReader
    participant DB as PostgreSQL
    participant Social as Social gRPC
    participant Achievements as Achievements gRPC
    participant Cache as Redis

    Browser->>Gateway: GET /users/me
    Gateway->>UsersAPI: YARP proxy
    UsersAPI->>Handler: ISender.Send(GetMyProfileQuery)
    Handler->>Cache: TryGet profile
    alt Cache hit
        Cache-->>Handler: cached profile
    else Cache miss
        Handler->>Reader: ReadAsync(source, followStats)
        Reader->>DB: SELECT Id, Username, DisplayName, Bio, Skills
        Reader->>Social: gRPC GetFollowStatsAsync
        Social-->>Reader: {followers, following}
        Reader-->>Handler: UserProfileDto
        Handler->>Cache: Set (5 min TTL)
    end
    Handler->>Achievements: gRPC GetUserAchievementsAsync (2s timeout)
    alt Achievements ответил
        Achievements-->>Handler: [achievements]
    else Timeout или ошибка
        Handler->>Handler: return empty[]
    end
    Handler-->>UsersAPI: Result.Success(profile + achievements)
    UsersAPI-->>Browser: 200 OK
```

**Ключевой момент**: достижения **не кэшируются** — они запрашиваются каждый раз. Профиль (без достижений) кэшируется на 5 минут.

## Непроверенные сценарии

- Удаление поста (DeletePostCommandHandler)
- Редактирование поста (EditPostCommandHandler)
- Unlike поста (UnlikePostCommandHandler)
- Отписка (UnfollowUserCommandHandler)
- Загрузка аватара/шапки (обходит MediatR)
- Вход пользователя (Login — проверен на уровне handler, без диаграммы)
