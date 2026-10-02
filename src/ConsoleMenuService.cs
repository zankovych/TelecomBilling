// ============================================================
// БЛОК 8: КОНСОЛЬНИЙ ІНТЕРФЕЙС
// Принцип SRP: тільки відображення і введення даних.
// Вся логіка — в сервісах. UI нічого не рахує сам.
// ============================================================

using TelecomSystem.Interfaces;
using TelecomSystem.Models;
using TelecomSystem.Patterns;

namespace TelecomSystem.Services;

public class ConsoleMenuService
{
    private readonly ISubscriberService _subscriberService;
    private readonly IBillingService _billingService;
    private readonly ITariffService _tariffService;
    private readonly ITariffFactory _tariffFactory;

    public ConsoleMenuService(
        ISubscriberService subscriberService,
        IBillingService billingService,
        ITariffService tariffService,
        ITariffFactory tariffFactory)
    {
        _subscriberService = subscriberService;
        _billingService = billingService;
        _tariffService = tariffService;
        _tariffFactory = tariffFactory;
    }

    public async Task RunAsync()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        PrintWelcome();

        while (true)
        {
            PrintMainMenu();
            var choice = Console.ReadLine()?.Trim();

            try
            {
                switch (choice)
                {
                    case "1": await ShowAllSubscribersAsync(); break;
                    case "2": await AddSubscriberAsync(); break;
                    case "3": await TopUpBalanceAsync(); break;
                    case "4": await RegisterCallAsync(); break;
                    case "5": await RegisterDataUsageAsync(); break;
                    case "6": await ShowSubscriberDetailsAsync(); break;
                    case "7": await GenerateInvoiceAsync(); break;
                    case "8": await PayInvoiceAsync(); break;
                    case "9": await ChangeTariffAsync(); break;
                    case "10": await ShowTariffsWithDecoratorAsync(); break;
                    case "0":
                        Console.WriteLine("\nДякуємо за використання системи. До побачення!");
                        return;
                    default:
                        Console.WriteLine("❌ Невідомий вибір.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n❌ Помилка: {ex.Message}");
                Console.ResetColor();
            }

            Console.WriteLine("\nНатисніть Enter для продовження...");
            Console.ReadLine();
            Console.Clear();
        }
    }

    private void PrintWelcome()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("╔══════════════════════════════════════════════╗");
        Console.WriteLine("║   📱 ТЕЛЕКОМУНІКАЦІЙНА СИСТЕМА БІЛІНГУ v2.0  ║");
        Console.WriteLine("╚══════════════════════════════════════════════╝");
        Console.ResetColor();
    }

    private void PrintMainMenu()
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n═══════════ ГОЛОВНЕ МЕНЮ ═══════════");
        Console.ResetColor();
        Console.WriteLine("  1.  📋 Список абонентів");
        Console.WriteLine("  2.  ➕ Додати абонента");
        Console.WriteLine("  3.  💰 Поповнити баланс");
        Console.WriteLine("  4.  📞 Зареєструвати дзвінок");
        Console.WriteLine("  5.  🌐 Зареєструвати інтернет");
        Console.WriteLine("  6.  👤 Деталі абонента");
        Console.WriteLine("  7.  🧾 Сформувати рахунок");
        Console.WriteLine("  8.  💳 Оплатити рахунок");
        Console.WriteLine("  9.  🔄 Змінити тариф");
        Console.WriteLine("  10. 🔧 Демо: тарифи + декоратори");
        Console.WriteLine("  0.  🚪 Вийти");
        Console.Write("\nВаш вибір: ");
    }

    private async Task ShowAllSubscribersAsync()
    {
        Console.WriteLine("\n═══════════ СПИСОК АБОНЕНТІВ ═══════════");
        var subscribers = (await _subscriberService.GetAllSubscribersAsync()).ToList();

        if (!subscribers.Any()) { Console.WriteLine("  (список порожній)"); return; }

        Console.WriteLine($"  {"ID",-5} {"ПІБ",-22} {"Телефон",-15} {"Тариф",-15} {"Баланс",10}");
        Console.WriteLine("  " + new string('─', 72));

        foreach (var s in subscribers)
        {
            Console.Write($"  {s.Id,-5} {s.FullName,-22} {s.PhoneNumber,-15} {s.Tariff?.Name,-15} ");
            Console.ForegroundColor = s.Balance < 30 ? ConsoleColor.Yellow : ConsoleColor.White;
            Console.WriteLine($"{s.Balance,9:F2} грн");
            Console.ResetColor();
        }
    }

    private async Task AddSubscriberAsync()
    {
        Console.WriteLine("\n═══════════ ДОДАТИ АБОНЕНТА ═══════════");
        Console.Write("  ПІБ: ");
        var name = Console.ReadLine() ?? "";

        Console.Write("  Телефон: ");
        var phone = Console.ReadLine() ?? "";

        await ShowTariffsListAsync();
        Console.Write("  ID тарифу: ");
        if (!int.TryParse(Console.ReadLine(), out int tariffId)) return;

        var subscriber = await _subscriberService.CreateSubscriberAsync(name, phone, tariffId);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  ✅ '{subscriber.FullName}' додано (ID={subscriber.Id})");
        Console.ResetColor();
    }

    private async Task TopUpBalanceAsync()
    {
        Console.WriteLine("\n═══════════ ПОПОВНЕННЯ БАЛАНСУ ═══════════");
        Console.Write("  ID абонента: ");
        if (!int.TryParse(Console.ReadLine(), out int id)) return;

        Console.Write("  Сума (грн): ");
        if (!decimal.TryParse(Console.ReadLine(), out decimal amount)) return;

        await _subscriberService.TopUpBalanceAsync(id, amount);
        var s = await _subscriberService.GetSubscriberAsync(id);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  ✅ Баланс поповнено. Поточний: {s?.Balance:F2} грн");
        Console.ResetColor();
    }

    private async Task RegisterCallAsync()
    {
        Console.WriteLine("\n═══════════ РЕЄСТРАЦІЯ ДЗВІНКА ═══════════");
        Console.Write("  ID абонента: ");
        if (!int.TryParse(Console.ReadLine(), out int id)) return;

        Console.Write("  Номер призначення: ");
        var dest = Console.ReadLine() ?? "";

        Console.Write("  Тривалість (секунди): ");
        if (!int.TryParse(Console.ReadLine(), out int duration)) return;

        var call = await _billingService.RegisterCallAsync(id, dest, duration);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  ✅ Дзвінок {duration / 60} хв {duration % 60} с. Вартість: {call.Cost:F2} грн");
        Console.ResetColor();
    }

    private async Task RegisterDataUsageAsync()
    {
        Console.WriteLine("\n═══════════ РЕЄСТРАЦІЯ ІНТЕРНЕТУ ═══════════");
        Console.Write("  ID абонента: ");
        if (!int.TryParse(Console.ReadLine(), out int id)) return;

        Console.Write("  Обсяг (МБ): ");
        if (!int.TryParse(Console.ReadLine(), out int dataMB)) return;

        var record = await _billingService.RegisterDataUsageAsync(id, dataMB);
        Console.ForegroundColor = ConsoleColor.Green;
        if (record.Cost == 0)
            Console.WriteLine($"  ✅ {dataMB} МБ використано (в межах пакету — безкоштовно)");
        else
            Console.WriteLine($"  ✅ {dataMB} МБ використано. Вартість: {record.Cost:F2} грн");
        Console.ResetColor();
    }

    private async Task ShowSubscriberDetailsAsync()
    {
        Console.WriteLine("\n═══════════ ДЕТАЛІ АБОНЕНТА ═══════════");
        Console.Write("  ID абонента: ");
        if (!int.TryParse(Console.ReadLine(), out int id)) return;

        var s = await _subscriberService.GetSubscriberAsync(id);
        if (s == null) { Console.WriteLine("  ❌ Не знайдено"); return; }

        Console.WriteLine($"\n  ПІБ:     {s.FullName}");
        Console.WriteLine($"  Телефон: {s.PhoneNumber}");
        Console.WriteLine($"  Тариф:   {s.Tariff?.GetDescription()}");
        Console.ForegroundColor = s.Balance < 30 ? ConsoleColor.Yellow : ConsoleColor.White;
        Console.WriteLine($"  Баланс:  {s.Balance:F2} грн");
        Console.ResetColor();

        var totalSpent = await _billingService.GetTotalSpentBySubscriberAsync(id);
        Console.WriteLine($"  Витрачено (дзвінки + інтернет): {totalSpent:F2} грн");
    }

    private async Task GenerateInvoiceAsync()
    {
        Console.WriteLine("\n═══════════ ФОРМУВАННЯ РАХУНКУ ═══════════");
        Console.Write("  ID абонента: ");
        if (!int.TryParse(Console.ReadLine(), out int id)) return;

        Console.Write("  Місяць (1-12): ");
        if (!int.TryParse(Console.ReadLine(), out int month)) return;

        Console.Write("  Рік: ");
        if (!int.TryParse(Console.ReadLine(), out int year)) return;

        var inv = await _billingService.GenerateMonthlyInvoiceAsync(id, month, year);

        Console.WriteLine($"\n  ══════ РАХУНОК #{inv.Id} ══════");
        Console.WriteLine($"  Абонент:   #{inv.SubscriberId}");
        Console.WriteLine($"  Період:    {month:D2}/{year}");
        Console.WriteLine($"  Абонплата: {inv.MonthlyFee:F2} грн");
        Console.WriteLine($"  Дзвінки:   {inv.TotalCallsCost:F2} грн");
        Console.WriteLine($"  Інтернет:  {inv.TotalDataCost:F2} грн");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"  РАЗОМ:     {inv.TotalAmount:F2} грн");
        Console.ResetColor();
        Console.WriteLine($"  Статус:    {(inv.IsPaid ? "✅ Оплачено" : "⏳ Не оплачено")}");
    }

    private async Task PayInvoiceAsync()
    {
        Console.WriteLine("\n═══════════ ОПЛАТА РАХУНКУ ═══════════");
        Console.Write("  ID рахунку: ");
        if (!int.TryParse(Console.ReadLine(), out int id)) return;

        bool success = await _billingService.PayInvoiceAsync(id);
        Console.ForegroundColor = success ? ConsoleColor.Green : ConsoleColor.Yellow;
        Console.WriteLine(success
            ? "  ✅ Рахунок оплачено!"
            : "  ⚠️  Оплачено, але баланс від'ємний!");
        Console.ResetColor();
    }

    private async Task ChangeTariffAsync()
    {
        Console.WriteLine("\n═══════════ ЗМІНА ТАРИФУ ═══════════");
        Console.Write("  ID абонента: ");
        if (!int.TryParse(Console.ReadLine(), out int subId)) return;

        await ShowTariffsListAsync();
        Console.Write("  ID нового тарифу: ");
        if (!int.TryParse(Console.ReadLine(), out int tariffId)) return;

        await _subscriberService.ChangeTariffAsync(subId, tariffId);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("  ✅ Тариф змінено!");
        Console.ResetColor();
    }

    /// <summary>
    /// Демонстрація патерну Декоратор (Лаба 5, Структурний).
    /// Показує як тарифи можна доповнювати послугами в runtime.
    /// </summary>
    private async Task ShowTariffsWithDecoratorAsync()
    {
        Console.WriteLine("\n══ ТАРИФИ + ДЕМО ДЕКОРАТОРА (Structural Pattern) ══");
        Console.WriteLine("  Принцип: базовий тариф + будь-яка послуга = новий тариф");
        Console.WriteLine("  Аналог з книги Будая: Mercedes → AmbulanceCar\n");

        var tariffs = (await _tariffService.GetAllTariffsAsync()).ToList();

        Console.WriteLine("  ── Базові тарифи ──");
        foreach (var t in tariffs)
            Console.WriteLine($"  [{t.Id}] {t.GetDescription()}");

        Console.WriteLine("\n  ── Декоровані тарифи (приклади комбінацій) ──");

        // Беремо BasicTariff (ID=2) як базу для демо
        var basic = tariffs.FirstOrDefault(t => t.Name == "Базовий") ?? tariffs.First();

        // Декоратор 1: Базовий + Роумінг
        Tariff withRoaming = new RoamingDecorator(basic);
        Console.WriteLine($"\n  🌍 {withRoaming.GetDescription()}");
        Console.WriteLine($"     Дзвінок 3 хв: {withRoaming.CalculateCallCost(180):F2} грн");

        // Декоратор 2: Базовий + 5ГБ інтернету
        Tariff withExtraData = new ExtraDataDecorator(basic, extraDataMB: 5120, monthlyFee: 40m);
        Console.WriteLine($"\n  📡 {withExtraData.GetDescription()}");
        Console.WriteLine($"     Пакет даних: {withExtraData.IncludedDataMB / 1024} ГБ");

        // Декоратор 3: Базовий + SMS-пакет + Роумінг (ланцюжок!)
        Tariff combo = new RoamingDecorator(new SmsPackageDecorator(basic));
        Console.WriteLine($"\n  🔗 Комбо: {combo.GetDescription()}");
        Console.WriteLine($"     Абонплата: {combo.MonthlyFee:F2} грн/міс");
        Console.WriteLine($"     Дзвінок 3 хв у роумінгу: {combo.CalculateCallCost(180):F2} грн");
    }

    private async Task ShowTariffsListAsync()
    {
        var tariffs = await _tariffService.GetAllTariffsAsync();
        Console.WriteLine("\n  Доступні тарифи:");
        foreach (var t in tariffs)
            Console.WriteLine($"    [{t.Id}] {t.Name} — {t.MonthlyFee} грн/міс");
        Console.WriteLine();
    }
}