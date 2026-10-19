# 01 — Обзор системы

Дата аудита: 2026-10-18

## Краткое описание

**Venture** — социальная сеть для отслеживания профессионального и личностного развития. Функциональность: регистрация/авторизация, профиль с навыками, посты, подписки, лайки, лента, автоматические достижения.

Технологический стек: **.NET 10**, **PostgreSQL**, **Redis**, **Apache Kafka**, **gRPC**, **YARP**.

Система представляет собой **6 отдельных микросервисов** с разделением данных (database-per-service) и асинхронным взаимодействием через Kafka.

## System Context Diagram

```mermaid
graph LR
    Browser["🌐 Браузер"]
    Gateway["Gateway<br/>YARP :5227"]
    Users["Users<br/>:5231 / gRPC :6231"]
    Posts["Posts<br/>:5232"]
    Social["Social<br/>:5233 / gRPC :6233"]
    Achievements["Achievements<br/>:5234 / gRPC :6234"]
    Notifications["Notifications<br/>:5235"]
    Kafka["Apache Kafka<br/>:9092"]
    PG_Users["PostgreSQL<br/>venture_users"]
    PG_Posts["PostgreSQL<br/>venture_posts"]
    PG_Social["PostgreSQL<br/>venture_social"]
    PG_Ach["PostgreSQL<br/>venture_achievements"]
    PG_Notif["PostgreSQL<br/>venture_notifications"]
    Redis["Redis<br/>:6379"]

    Browser -->|HTTP| Gateway
    Gateway -->|HTTP proxy| Users
    Gateway -->|HTTP proxy| Posts
    Gateway -->|HTTP proxy| Social
    Gateway -->|HTTP proxy| Achievements

    Users -->|gRPC| Social
    Users -->|gRPC| Achievements
    Posts -->|gRPC| Users
    Posts -->|gRPC| Social
    Social -->|HTTP| Users

    Users -->|publish| Kafka
    Posts -->|publish| Kafka
    Social -->|publish| Kafka
    Kafka -->|consume| Achievements
    Kafka -->|consume| Notifications

    Users --> PG_Users
    Posts --> PG_Posts
    Social --> PG_Social
    Achievements --> PG_Ach
    Notifications --> PG_Notif

    Users --> Redis
    Posts --> Redis
    Social --> Redis
```

## Container Diagram

```mermaid
graph TB
    subgraph "Внешний уровень"
        Browser["Браузер (SPA)"]
    end

    subgraph "Gateway (YARP)"
        GW["Venture.Gateway<br/>Статические файлы + reverse proxy"]
    end

    subgraph "Микросервисы"
        subgraph "Users Service"
            UAPI["Users.Api<br/>Minimal API + gRPC server"]
            UAPP["Users.Application<br/>MediatR handlers"]
            UDOM["Users.Domain<br/>User aggregate"]
            UINF["Users.Infrastructure<br/>EF Core + Redis + Kafka + gRPC clients"]
        end
        subgraph "Posts Service"
            PAPI["Posts.Api<br/>Minimal API"]
            PAPP["Posts.Application<br/>MediatR handlers"]
            PDOM["Posts.Domain<br/>Post aggregate"]
            PINF["Posts.Infrastructure<br/>EF Core + Redis + Kafka + gRPC clients"]
        end
        subgraph "Social Service"
            SAPI["Social.Api<br/>Minimal API + gRPC server"]
            SAPP["Social.Application<br/>MediatR handlers"]
            SDOM["Social.Domain<br/>Follow aggregate"]
            SINF["Social.Infrastructure<br/>EF Core + Redis + Kafka + HTTP client"]
        end
        subgraph "Achievements Service"
            AAPI["Achievements.Api<br/>gRPC server"]
            AAPP["Achievements.Application<br/>MediatR handlers"]
            ADOM["Achievements.Domain<br/>Achievement + Progress"]
            AINF["Achievements.Infrastructure<br/>EF Core + Kafka consumer"]
        end
        subgraph "Notifications Service"
            NAPI["Notifications.Api<br/>Minimal API (no endpoints)"]
            NAPP["Notifications.Application<br/>MediatR handlers"]
            NDOM["Notifications.Domain<br/>Базовые абстракции"]
            NINF["Notifications.Infrastructure<br/>EF Core + Kafka consumer"]
        end
    end

    Browser --> GW
    GW --> UAPI
    GW --> PAPI
    GW --> SAPI
    GW --> AAPI
```

## Основные технологические решения

| Решение | Обоснование | Оценка |
|---------|------------|--------|
| .NET 10 + Minimal API | Современный стек, легковесные эндпоинты | ✅ Оправдано |
| YARP как Gateway | Минимальная логика в шлюзе, просто прокси | ✅ Оправдано |
| Database-per-service | Строгое разделение данных между сервисами | ✅ Оправдано |
| MediatR (CQRS) | Разделение команд и запросов, pipeline behaviors | ⚠️ Оправдано, но есть дублирование кода |
| gRPC для синхронных вызовов | Типизированные контракты, быстрый протокол | ✅ Оправдано |
| Kafka для async событий | Надёжная доставка фактов, at-least-once | ✅ Оправдано |
| Redis для кэша | Быстрый доступ к лентам и профилям | ✅ Оправдано, но жёсткая зависимость |
| Clean Architecture в каждом сервисе | Разделение слоёв Domain → Application → Infrastructure → Api | ⚠️ Для данного размера избыточно, но консистентно |
| Отсутствие shared library | Каждый сервис дублирует абстракции | ⚠️ Компромисс: независимость vs дублирование |

## Принципы владения данными

Каждый сервис владеет своей базой PostgreSQL:

| Сервис | База | Владение |
|--------|------|----------|
| Users | `venture_users` | Пользователи, профили, навыки, хеши паролей |
| Posts | `venture_posts` | Посты, лайки |
| Social | `venture_social` | Подписки (follows) |
| Achievements | `venture_achievements` | Каталог достижений, прогресс, факты |
| Notifications | `venture_notifications` | Уведомления |
| Gateway | — | Нет собственной базы |

**Shared databases отсутствуют.** Это подтверждено архитектурным тестом `DatabaseOwnershipTests`.

## Границы сервисов

- **Users** — единственный источник правды о пользователях, JWT-токенах
- **Posts** — единственный владелец постов и лайков; при создании поста проверяет существование автора в Users через gRPC
- **Social** — владеет подписками; проверяет существование пользователя в Users через HTTP
- **Achievements** — реактивный сервис: принимает факты из Kafka (пост опубликован, подписка создана) и самостоятельно решает о выдаче достижений
- **Notifications** — чистый sink: принимает уведомления из Kafka и пишет в базу
- **Gateway** — статика + маршрутизация, бизнес-логики нет

## Основные архитектурные компромиссы

1. **Отсутствие Outbox Pattern**: публикация в Kafka происходит после коммита БД в PostCommitActions. Если Kafka недоступен, данные сохранены, но событие потеряно.

2. **Дублирование DDD-абстракций**: `AggregateRoot`, `ValueObject`, `Result`, `DomainEvent` и 4 MediatR-поведения продублированы в каждом из 5 сервисов (+ Activities). Независимость сервисов обеспечена, но при этом ~40% базового кода идентичен.

3. **Notifications без API для чтения**: уведомления записываются, но не отдаются клиенту — нет эндпоинта для чтения.

4. **Activities** — заглушка. Содержит только Domain с базовыми абстракциями, без Application/Infrastructure/Api. Сервис не запускается и не интегрирован.
