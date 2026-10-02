// ============================================================
// БЛОК 4: ПАТЕРНИ ПРОЕКТУВАННЯ (Лаба 5)
//
// 1. ПОРОДЖУЮЧИЙ  → Фабричний Метод (Factory Method)
// 2. СТРУКТУРНИЙ  → Декоратор (Decorator)
// 3. ПОВЕДІНКОВИЙ → Спостерігач (Observer)
// ============================================================

using TelecomSystem.Interfaces;
using TelecomSystem.Models;

namespace TelecomSystem.Patterns;

// ============================================================
// ПАТЕРН 1: ФАБРИЧНИЙ МЕТОД (Factory Method) — Породжуючий
// ============================================================

public class TariffFactory : ITariffFactory
{
    public Tariff CreateTariff(string tariffType)
    {
        return tariffType.ToLower() switch
        {
            "start"      or "старт"          => new StartTariff(),
            "basic"      or "базовий"        => new BasicTariff(),
            "premium"    or "преміум"        => new PremiumTariff(),
            "unlimited"  or "безліміт"       => new UnlimitedTariff(),
            "corporate"  or "корпоративний"  => new CorporateTariff(),
            _ => throw new ArgumentException(
                $"Невідомий тип тарифу: '{tariffType}'. " +
                "Доступні: start, basic, premium, unlimited, corporate")
        };
    }
}


// ============================================================
// ПАТЕРН 2: ДЕКОРАТОР (Decorator) — Структурний
// ============================================================

public abstract class TariffDecorator : Tariff
{
    protected readonly Tariff _decoratedTariff;

    protected TariffDecorator(Tariff tariff)
    {
        _decoratedTariff = tariff;
        Name = tariff.Name;
        MonthlyFee = tariff.MonthlyFee;
        PricePerMinute = tariff.PricePerMinute;
        IncludedMinutes = tariff.IncludedMinutes;
        IncludedDataMB = tariff.IncludedDataMB;
        PricePerMB = tariff.PricePerMB;
    }

    public override decimal CalculateCallCost(int durationSeconds) =>
        _decoratedTariff.CalculateCallCost(durationSeconds);

    public override decimal CalculateDataCost(int dataMB) =>
        _decoratedTariff.CalculateDataCost(dataMB);

    public override string GetDescription() =>
        _decoratedTariff.GetDescription();
}

/// <summary>
/// Декоратор "Міжнародний роумінг": множить вартість дзвінка.
/// </summary>
public class RoamingDecorator : TariffDecorator
{
    private readonly decimal _roamingMultiplier;
    private readonly decimal _roamingMonthlyFee;

    public RoamingDecorator(Tariff tariff, decimal multiplier = 3.0m, decimal monthlyFee = 50m)
        : base(tariff)
    {
        _roamingMultiplier = multiplier;
        _roamingMonthlyFee = monthlyFee;
        Name = tariff.Name + " + Роумінг";
        MonthlyFee = tariff.MonthlyFee + _roamingMonthlyFee;
    }

    public override decimal CalculateCallCost(int durationSeconds) =>
        _decoratedTariff.CalculateCallCost(durationSeconds) * _roamingMultiplier;

    public override string GetDescription() =>
        _decoratedTariff.GetDescription() +
        $" | +Роумінг (×{_roamingMultiplier}, +{_roamingMonthlyFee} грн/міс)";
}

/// <summary>
/// Декоратор "Додатковий інтернет": збільшує пакет даних.
/// </summary>
public class ExtraDataDecorator : TariffDecorator
{
    private readonly int _extraDataMB;
    private readonly decimal _extraDataFee;

    public ExtraDataDecorator(Tariff tariff, int extraDataMB = 5120, decimal monthlyFee = 40m)
        : base(tariff)
    {
        _extraDataMB = extraDataMB;
        _extraDataFee = monthlyFee;
        Name = tariff.Name + $" + Інтернет +{extraDataMB / 1024}ГБ";
        MonthlyFee = tariff.MonthlyFee + _extraDataFee;
        IncludedDataMB = tariff.IncludedDataMB + _extraDataMB;
    }

    public override decimal CalculateDataCost(int dataMB)
    {
        if (dataMB <= IncludedDataMB) return 0m;
        return (dataMB - IncludedDataMB) * PricePerMB;
    }

    public override string GetDescription() =>
        _decoratedTariff.GetDescription() +
        $" | +{_extraDataMB / 1024}ГБ інтернету (+{_extraDataFee} грн/міс)";
}

/// <summary>
/// Декоратор "SMS-пакет": додає безлімітні SMS.
/// </summary>
public class SmsPackageDecorator : TariffDecorator
{
    private readonly decimal _packageMonthlyFee;

    public SmsPackageDecorator(Tariff tariff, decimal monthlyFee = 20m)
        : base(tariff)
    {
        _packageMonthlyFee = monthlyFee;
        Name = tariff.Name + " + SMS∞";
        MonthlyFee = tariff.MonthlyFee + _packageMonthlyFee;
    }

    public override string GetDescription() =>
        _decoratedTariff.GetDescription() +
        $" | +Безліміт SMS (+{_packageMonthlyFee} грн/міс)";
}


// ============================================================
// ПАТЕРН 3: СПОСТЕРІГАЧ (Observer) — Поведінковий
// ============================================================

public class BalanceNotificationManager : IBalanceSubject
{
    private readonly List<IBalanceObserver> _observers = new();

    public void AttachObserver(IBalanceObserver observer)
    {
        if (!_observers.Contains(observer))
            _observers.Add(observer);
    }

    public void DetachObserver(IBalanceObserver observer) =>
        _observers.Remove(observer);

    public void NotifyObservers(Subscriber subscriber, decimal newBalance)
    {
        foreach (var observer in _observers)
            observer.OnBalanceChanged(subscriber, newBalance);
    }
}

/// <summary>
/// Спостерігач: попередження про низький баланс у консолі.
/// </summary>
public class LowBalanceConsoleObserver : IBalanceObserver
{
    private readonly decimal _threshold;

    public LowBalanceConsoleObserver(decimal threshold = 30m)
    {
        _threshold = threshold;
    }

    public void OnBalanceChanged(Subscriber subscriber, decimal newBalance)
    {
        if (newBalance < _threshold && newBalance >= 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(
                $"\n⚠️  Увага! Низький баланс у {subscriber.FullName} " +
                $"({subscriber.PhoneNumber}): {newBalance:F2} грн");
            Console.ResetColor();
        }
    }
}

/// <summary>
/// Спостерігач: логування у файл для аудиту.
/// </summary>
public class BalanceAuditLogger : IBalanceObserver
{
    private readonly string _logFilePath;

    public BalanceAuditLogger(string logFilePath = "balance_audit.log")
    {
        _logFilePath = logFilePath;
    }

    public void OnBalanceChanged(Subscriber subscriber, decimal newBalance)
    {
        var entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] " +
                    $"Абонент #{subscriber.Id} ({subscriber.PhoneNumber}) | " +
                    $"Баланс: {newBalance:F2} грн\n";
        File.AppendAllText(_logFilePath, entry);
    }
}

/// <summary>
/// Спостерігач: блокує абонента при від'ємному балансі.
/// </summary>
public class NegativeBalanceBlocker : IBalanceObserver
{
    public bool WasBlocked { get; private set; } = false;
    public string? BlockedSubscriberPhone { get; private set; }

    public void OnBalanceChanged(Subscriber subscriber, decimal newBalance)
    {
        if (newBalance < 0)
        {
            WasBlocked = true;
            BlockedSubscriberPhone = subscriber.PhoneNumber;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(
                $"\n🚫 Абонент '{subscriber.FullName}' заблокований. " +
                $"Баланс: {newBalance:F2} грн");
            Console.ResetColor();
        }
    }
}