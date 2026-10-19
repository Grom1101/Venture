# 02 — Каталог сервисов и проектов

## Инвентаризация

Всего обнаружено **38 проектов (.csproj)** в 8 директориях. Из них:
- 5 полноценных сервисов (Users, Posts, Social, Achievements, Notifications)
- 1 API-шлюз (Gateway)
- 1 заглушка сервиса (Activities)
- 1 проект архитектурных тестов
- 25 проектов Domain / Application / Infrastructure / Tests

## Сервисы

### Gateway (Venture.Gateway)

| Параметр | Значение |
|----------|----------|
| Путь | `services/Gateway/Venture.Gateway/` |
| Тип | Web API (YARP reverse proxy) |
| Ответственность | Статические файлы (SPA), маршрутизация API |
| Точки входа | HTTP :5227 |
| База данных | Нет |
| Зависимости NuGet | `Yarp.ReverseProxy 2.3.0` |
| Межсервисные зависимости | Proxy → Users, Posts, Social, Achievements |
| Тесты | Нет |
| Статус | ✅ Исследован полностью |

### Users

| Параметр | Значение |
|----------|----------|
| Путь | `services/Users/` |
| Проекты | Users.Api, Users.Application, Users.Domain, Users.Infrastructure + 3 теста |
| Ответственность | Аккаунты, профили, навыки, JWT, загрузка аватара/шапки |
| Точки входа | HTTP :5231, gRPC :6231 |
| База данных | `venture_users` (PostgreSQL) |
| Ключевые NuGet | EF Core 10.0.12, Confluent.Kafka 2.16.0, Grpc.AspNetCore, StackExchange.Redis 3.3.1, MediatR 12.5.0, FluentValidation 12.1.1 |
| gRPC сервер | `UserLookupService` — проверка существования пользователя, карточки |
| gRPC клиенты | Social (подписки), Achievements (значки для профиля) |
| Kafka producer | `venture.notifications` (Welcome) |
| Redis | Кэш профилей (5 мин) |
| Тесты | Domain.Tests, Application.Tests, IntegrationTests |
| Статус | ✅ Исследован полностью |

### Posts

| Параметр | Значение |
|----------|----------|
| Путь | `services/Posts/` |
| Проекты | Posts.Api, Posts.Application, Posts.Domain, Posts.Infrastructure + 3 теста |
| Ответственность | Посты, лайки, лента |
| Точки входа | HTTP :5232 |
| База данных | `venture_posts` (PostgreSQL) |
| gRPC клиенты | Users (проверка автора, карточки), Social (подписки для ленты) |
| Kafka producer | `venture.notifications` (PostLiked), `venture.posts.post-published` |
| Redis | Кэш ленты |
| Тесты | Domain.Tests, Application.Tests, IntegrationTests |
| Статус | ✅ Исследован полностью |

### Social

| Параметр | Значение |
|----------|----------|
| Путь | `services/Social/` |
| Проекты | Social.Api, Social.Application, Social.Domain, Social.Infrastructure + 3 теста |
| Ответственность | Подписки (follow/unfollow), счётчики подписчиков |
| Точки входа | HTTP :5233, gRPC :6233 |
| База данных | `venture_social` (PostgreSQL) |
| HTTP клиент | Users (проверка существования пользователя) — **не gRPC!** |
| gRPC сервер | `SocialLookupService` — счётчики подписок, список подписчиков |
| Kafka producer | `venture.notifications` (UserFollowed), `venture.social.user-followed` |
| Redis | Кэш |
| Тесты | Domain.Tests, Application.Tests, IntegrationTests |
| Статус | ✅ Исследован полностью |

### Achievements

| Параметр | Значение |
|----------|----------|
| Путь | `services/Achievements/` |
| Проекты | Achievements.Api, Achievements.Application, Achievements.Domain, Achievements.Infrastructure + 3 теста |
| Ответственность | Каталог достижений, прогресс пользователя, выдача значков |
| Точки входа | gRPC :6234 (HTTP :5234 для internal endpoints) |
| База данных | `venture_achievements` (PostgreSQL) |
| Kafka consumer | `venture.posts.post-published`, `venture.social.user-followed` |
| gRPC сервер | `AchievementLookupService` — достижения пользователя |
| Без Auth | Нет JWT-проверки — внутренний сервис |
| Тесты | Domain.Tests, Application.Tests, IntegrationTests |
| Статус | ✅ Исследован полностью |

### Notifications

| Параметр | Значение |
|----------|----------|
| Путь | `services/Notifications/` |
| Проекты | Notifications.Api, Notifications.Application, Notifications.Domain, Notifications.Infrastructure + 3 теста |
| Ответственность | Запись уведомлений из Kafka |
| Точки входа | HTTP :5235 (но нет public endpoints!) |
| База данных | `venture_notifications` (PostgreSQL) |
| Kafka consumer | `venture.notifications` |
| Без Auth | Нет JWT — внутренний сервис |
| Тесты | Domain.Tests, Application.Tests, IntegrationTests |
| Статус | ✅ Исследован полностью |

### Activities (заглушка)

| Параметр | Значение |
|----------|----------|
| Путь | `services/Activities/` |
| Проекты | Activities.Domain (только DDD-абстракции) |
| Ответственность | Планировался как сервис активностей |
| Точки входа | Нет |
| Статус | ⚠️ Заглушка, не интегрирован |

### ArchitectureTests

| Параметр | Значение |
|----------|----------|
| Путь | `services/ArchitectureTests/` |
| Тесты | ProjectReferenceTests, DatabaseOwnershipTests, FactContractTests, LegacyAchievementPathTests |
| Ответственность | Проверка архитектурных инвариантов |
| Статус | ✅ Исследован полностью |

## Направления зависимостей между слоями (каждый сервис)

```
Api → Application → Domain
Api → Infrastructure → Application → Domain
```

Domain не ссылается ни на один проект — **это подтверждено кодом и архитектурными тестами.**

## Потенциальные проблемы

### Дублирование базового кода

Следующие типы продублированы во **всех 5 сервисах** (+ Activities):
- `AggregateRoot<T>`, `BaseEntity<T>`, `ValueObject`
- `DomainEvent`, `IDomainEvent`, `IHasDomainEvents`
- `StronglyTypedId<T>`
- `Result`, `Result<T>`, `Error`, `ErrorType`
- `ICommand`, `ICommandHandler`, `IQuery`, `IQueryHandler`
- `LoggingBehavior`, `PerformanceBehavior`, `ValidationBehavior`, `TransactionBehavior`
- `DomainEventDispatcher`, `EfTransactionManager`, `PostCommitActions`
- `ExceptionHandlingMiddleware`, `ResultExtensions`
- `VentureDbContext` (с одинаковым pattern dispatch events в SaveChanges)

**Факт**: это сознательный компромисс ради полной независимости сервисов (нет shared NuGet пакетов). Однако при 5 копиях багфикс нужно вносить в 5 мест.

### Неиспользуемые компоненты

| Компонент | Причина |
|-----------|---------|
| Activities (весь сервис) | Только Domain-абстракции, нет бизнес-логики |
| `NotificationType.AchievementEarned` | Объявлен, но никто не отправляет |
| `Notification.MarkAsRead()` (если существует) | Нет endpoint для чтения/отметки уведомлений |
| `UploadProfileMediaEndpoint` | Обходит MediatR pipeline — потенциальный design smell |
