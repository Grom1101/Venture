
using Microsoft.EntityFrameworkCore;
using Venture.Domain.Achievements;
using Venture.Domain.Achievements.Enums;

namespace Venture.Infrastructure.Persistence;

/// <summary>
/// Каталог достижений — справочные данные, а не пользовательский контент,
/// поэтому он наполняется при старте приложения.
/// </summary>
public static class AchievementCatalogSeeder
{
    private static readonly (AchievementType Type, string Name, string Description)[] Catalog =
    [
        // 1. Первые шаги
        (
            AchievementType.FirstSteps,
            "Первопроходец",
            "Зарегистрироваться на сайте"
        ),
        (
            AchievementType.FirstSteps,
            "Личность",
            "Добавить аватар и имя профиля"
        ),
        (
            AchievementType.FirstSteps,
            "Открытая книга",
            "Заполнить раздел «О себе»"
        ),
        (
            AchievementType.FirstSteps,
            "Маршрут построен",
            "Начать путь развития"
        ),
        (
            AchievementType.FirstSteps,
            "Есть цель!",
            "Создать первую цель"
        ),
        (
            AchievementType.FirstSteps,
            "Первое действие",
            "Записать первую активность"
        ),
        (
            AchievementType.FirstSteps,
            "Не держу в себе",
            "Опубликовать первый пост"
        ),

        // 2. Регулярность
        (
            AchievementType.Regularity,
            "Ритм найден",
            "Записать активности в течение 10 разных дней"
        ),
        (
            AchievementType.Regularity,
            "Не пропадаю",
            "Быть активным 20 разных дней"
        ),
        (
            AchievementType.Regularity,
            "На дистанции",
            "Выполнить 50 активностей за всё время"
        ),
        (
            AchievementType.Regularity,
            "Полный ход",
            "Выполнить 100 активностей"
        ),
        (
            AchievementType.Regularity,
            "Фундамент",
            "Выполнить 250 активностей"
        ),
        (
            AchievementType.Regularity,
            "Ранний старт",
            "Зафиксировать активность до 8:00 в 5 разные дни"
        ),
        (
            AchievementType.Regularity,
            "Вечерний фокус",
            "Зафиксировать активность после 20:00 в 5 разные дни"
        ),

        // 3. Цели и прогресс
        (
            AchievementType.GoalsAndProgress,
            "Свой курс",
            "Создать 3 пути развития"
        ),
        (
            AchievementType.GoalsAndProgress,
            "В движении",
            "Выполнить первую активность в рамках пути"
        ),
        (
            AchievementType.GoalsAndProgress,
            "Цель достигнута",
            "Завершить первую цель"
        ),
        (
            AchievementType.GoalsAndProgress,
            "Коллекционер результатов",
            "Завершить 5 целей"
        ),
        (
            AchievementType.GoalsAndProgress,
            "Не зря старался",
            "Завершить 10 целей"
        ),
        (
            AchievementType.GoalsAndProgress,
            "Многогранная личность",
            "Развивать 3 разных направления жизни"
        ),

        // 4. Сообщество
        (
            AchievementType.Community,
            "Первый контакт",
            "Подписаться на первого пользователя"
        ),
        (
            AchievementType.Community,
            "Не один на пути",
            "Подписаться на 10 пользователей"
        ),
        (
            AchievementType.Community,
            "Начало диалога",
            "Оставить первый комментарий"
        ),
        (
            AchievementType.Community,
            "Заметный участник",
            "Получить 10 реакций на свои записи"
        ),
        (
            AchievementType.Community,
            "Находят полезным",
            "Получить 50 реакций на свои записи"
        ),
        (
            AchievementType.Community,
            "Ищем похожих",
            "Найти пользователя с похожими целями"
        ),

        // 5. Особые
        (
            AchievementType.Special,
            "Первые из первых",
            "Зарегистрироваться в период закрытого тестирования"
        ),
        (
            AchievementType.Special,
            "🛠Соавтор",
            "Предложить улучшение, которое вошло в продукт"
        ),
        (
            AchievementType.Special,
            "Попал в рекомендации",
            "Сделал пост, который попал в глобальную ленту рекомендаций"
        ),
    ];

    public static async Task SeedAsync(
        VentureDbContext dbContext,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var existingNames = await dbContext.Achievements
            .Select(achievement => achievement.Name)
            .ToListAsync(cancellationToken);

        var existingNamesSet = existingNames.ToHashSet(
            StringComparer.Ordinal
        );

        var missing = Catalog
            .Where(item => !existingNamesSet.Contains(item.Name))
            .Select(item => Achievement.Create(
                item.Type,
                item.Name,
                item.Description,
                utcNow
            ).Value)
            .ToArray();

        if (missing.Length == 0)
            return;

        dbContext.Achievements.AddRange(missing);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
