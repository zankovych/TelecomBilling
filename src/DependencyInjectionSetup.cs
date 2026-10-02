// ============================================================
// БЛОК 7: DI / IoC КОНТЕЙНЕР (Лаба 4 — головна вимога!)
// ============================================================

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TelecomSystem.Data;
using TelecomSystem.Interfaces;
using TelecomSystem.Patterns;
using TelecomSystem.Repositories;
using TelecomSystem.Services;

namespace TelecomSystem.Infrastructure;

public static class DependencyInjectionSetup
{
    public static IServiceProvider BuildServiceProvider(string dbPath = "telecom.db")
    {
        var services = new ServiceCollection();

        // ── База даних ───────────────────────────────────────────
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        // ── Репозиторії (Transient) ──────────────────────────────
        services.AddTransient<ISubscriberRepository, SubscriberRepository>();
        services.AddTransient<ITariffRepository, TariffRepository>();
        services.AddTransient<ICallRecordRepository, CallRecordRepository>();
        services.AddTransient<IDataUsageRepository, DataUsageRepository>();
        services.AddTransient<IInvoiceRepository, InvoiceRepository>();

        // ── Сервіси ──────────────────────────────────────────────
        services.AddTransient<ISubscriberService, SubscriberService>();
        services.AddTransient<IBillingService, BillingService>();
        services.AddTransient<ITariffService, TariffService>();

        // ── Патерн Фабричний Метод (Singleton) ──────────────────
        services.AddSingleton<ITariffFactory, TariffFactory>();

        // ── Патерн Спостерігач (Singleton) ──────────────────────
        services.AddSingleton<BalanceNotificationManager>();
        services.AddSingleton<IBalanceSubject>(sp =>
            sp.GetRequiredService<BalanceNotificationManager>());

        services.AddSingleton<LowBalanceConsoleObserver>();
        services.AddSingleton<BalanceAuditLogger>();
        services.AddSingleton<NegativeBalanceBlocker>();

        // ── UI ───────────────────────────────────────────────────
        services.AddTransient<ConsoleMenuService>();

        return services.BuildServiceProvider();
    }

    public static void ConfigureObservers(IServiceProvider sp)
    {
        var manager = sp.GetRequiredService<BalanceNotificationManager>();
        manager.AttachObserver(sp.GetRequiredService<LowBalanceConsoleObserver>());
        manager.AttachObserver(sp.GetRequiredService<BalanceAuditLogger>());
        manager.AttachObserver(sp.GetRequiredService<NegativeBalanceBlocker>());
    }

    /// <summary>
    /// Ініціалізує БД.
    /// ВАЖЛИВО: якщо схема застаріла — видаляємо і створюємо заново.
    /// При продакшн-використанні замінити на міграції EF Core.
    /// </summary>
    public static async Task InitializeDatabaseAsync(IServiceProvider sp, bool forceRecreate = false)
    {
        using var scope = sp.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (forceRecreate)
        {
            // Повне перестворення — видаляємо стару схему
            await context.Database.EnsureDeletedAsync();
            Console.WriteLine("⚠️  Стару базу видалено (оновлення схеми).");
        }

        await context.Database.EnsureCreatedAsync();
    }
}