// ============================================================
// БЛОК 1: МОДЕЛІ / СУТНОСТІ
// Принцип SRP: кожен клас — одна відповідальність.
// Принцип OCP: щоб додати новий тариф — створюємо новий клас,
//              не змінюємо існуючі.
// ============================================================

namespace TelecomSystem.Models;

/// <summary>
/// Абонент телекомунікаційної компанії.
/// </summary>
public class Subscriber
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public int TariffId { get; set; }
    public Tariff Tariff { get; set; } = null!;
    public ICollection<CallRecord> CallRecords { get; set; } = new List<CallRecord>();
    public ICollection<DataUsageRecord> DataUsageRecords { get; set; } = new List<DataUsageRecord>();
}

/// <summary>
/// Базовий клас тарифу.
/// TPH (Table-Per-Hierarchy) — всі тарифи в одній таблиці через EF Core.
/// </summary>
public abstract class Tariff
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal MonthlyFee { get; set; }
    public decimal PricePerMinute { get; set; }

    // Включений пакет хвилин (0 = немає пакету)
    public int IncludedMinutes { get; set; } = 0;

    // Включений інтернет у МБ (0 = немає пакету)
    public int IncludedDataMB { get; set; } = 0;

    // Ціна за МБ понад пакет
    public decimal PricePerMB { get; set; } = 0m;

    public ICollection<Subscriber> Subscribers { get; set; } = new List<Subscriber>();

    /// <summary>
    /// Розраховує вартість дзвінка.
    /// Кожен тариф може перевизначити логіку (OCP + поліморфізм).
    /// </summary>
    public abstract decimal CalculateCallCost(int durationSeconds);

    /// <summary>
    /// Розраховує вартість використання інтернету.
    /// </summary>
    public abstract decimal CalculateDataCost(int dataMB);

    /// <summary>
    /// Людиночитаний опис тарифу для консолі.
    /// </summary>
    public abstract string GetDescription();
}

// ─────────────────────────────────────────────────────────────
// ТАРИФ 1: "Старт" — найдешевший, без пакетів
// ─────────────────────────────────────────────────────────────
public class StartTariff : Tariff
{
    public StartTariff()
    {
        Name = "Старт";
        MonthlyFee = 30m;
        PricePerMinute = 0.8m;
        IncludedMinutes = 0;
        IncludedDataMB = 0;
        PricePerMB = 0.05m;
    }

    public override decimal CalculateCallCost(int durationSeconds)
    {
        decimal minutes = Math.Ceiling(durationSeconds / 60m);
        return minutes * PricePerMinute;
    }

    public override decimal CalculateDataCost(int dataMB) =>
        dataMB * PricePerMB;

    public override string GetDescription() =>
        $"[{Name}] {MonthlyFee} грн/міс | {PricePerMinute} грн/хв | " +
        $"{PricePerMB} грн/МБ | Без пакетів";
}

// ─────────────────────────────────────────────────────────────
// ТАРИФ 2: "Базовий" — невеликий пакет хвилин та інтернету
// MonthlyFee = 50 грн, PricePerMinute = 0.5 грн
// ─────────────────────────────────────────────────────────────
public class BasicTariff : Tariff
{
    public BasicTariff()
    {
        Name = "Базовий";
        MonthlyFee = 50m;
        PricePerMinute = 0.5m;
        IncludedMinutes = 100;
        IncludedDataMB = 2048;   // 2 ГБ
        PricePerMB = 0.03m;
    }

    public override decimal CalculateCallCost(int durationSeconds)
    {
        decimal minutes = Math.Ceiling(durationSeconds / 60m);
        return minutes * PricePerMinute;
    }

    public override decimal CalculateDataCost(int dataMB)
    {
        if (dataMB <= IncludedDataMB) return 0m;
        return (dataMB - IncludedDataMB) * PricePerMB;
    }

    public override string GetDescription() =>
        $"[{Name}] {MonthlyFee} грн/міс | {PricePerMinute} грн/хв " +
        $"(+{IncludedMinutes} хв в пакеті) | {IncludedDataMB / 1024} ГБ інтернету";
}

// ─────────────────────────────────────────────────────────────
// ТАРИФ 3: "Преміум" — 0.3 грн/хв
// ─────────────────────────────────────────────────────────────
public class PremiumTariff : Tariff
{
    public PremiumTariff()
    {
        Name = "Преміум";
        MonthlyFee = 120m;
        PricePerMinute = 0.3m;
        IncludedMinutes = 300;
        IncludedDataMB = 10240;  // 10 ГБ
        PricePerMB = 0.02m;
    }

    public override decimal CalculateCallCost(int durationSeconds)
    {
        decimal minutes = Math.Ceiling(durationSeconds / 60m);
        return minutes * PricePerMinute;
    }

    public override decimal CalculateDataCost(int dataMB)
    {
        if (dataMB <= IncludedDataMB) return 0m;
        return (dataMB - IncludedDataMB) * PricePerMB;
    }

    public override string GetDescription() =>
        $"[{Name}] {MonthlyFee} грн/міс | {PricePerMinute} грн/хв " +
        $"(+{IncludedMinutes} хв) | {IncludedDataMB / 1024} ГБ інтернету";
}

// ─────────────────────────────────────────────────────────────
// ТАРИФ 4: "Безліміт" — великий пакет, знижена ціна за хвилину
// ─────────────────────────────────────────────────────────────
public class UnlimitedTariff : Tariff
{
    public UnlimitedTariff()
    {
        Name = "Безліміт";
        MonthlyFee = 250m;
        PricePerMinute = 0.2m;
        IncludedMinutes = 1000;
        IncludedDataMB = 30720;  // 30 ГБ
        PricePerMB = 0.01m;
    }

    public override decimal CalculateCallCost(int durationSeconds)
    {
        decimal minutes = Math.Ceiling(durationSeconds / 60m);
        return minutes * PricePerMinute;
    }

    public override decimal CalculateDataCost(int dataMB)
    {
        if (dataMB <= IncludedDataMB) return 0m;
        return (dataMB - IncludedDataMB) * PricePerMB;
    }

    public override string GetDescription() =>
        $"[{Name}] {MonthlyFee} грн/міс | {PricePerMinute} грн/хв " +
        $"(+{IncludedMinutes} хв) | {IncludedDataMB / 1024} ГБ інтернету";
}

// ─────────────────────────────────────────────────────────────
// ТАРИФ 5: "Корпоративний" — для бізнесу, знижка 20% на дзвінки і інтернет
// ─────────────────────────────────────────────────────────────
public class CorporateTariff : Tariff
{
    public decimal DiscountPercent { get; set; } = 20m;

    public CorporateTariff()
    {
        Name = "Корпоративний";
        MonthlyFee = 400m;
        PricePerMinute = 0.4m;
        IncludedMinutes = 500;
        IncludedDataMB = 20480;  // 20 ГБ
        PricePerMB = 0.015m;
    }

    public override decimal CalculateCallCost(int durationSeconds)
    {
        decimal minutes = Math.Ceiling(durationSeconds / 60m);
        decimal baseCost = minutes * PricePerMinute;
        return baseCost * (1 - DiscountPercent / 100m);
    }

    public override decimal CalculateDataCost(int dataMB)
    {
        if (dataMB <= IncludedDataMB) return 0m;
        decimal baseCost = (dataMB - IncludedDataMB) * PricePerMB;
        return baseCost * (1 - DiscountPercent / 100m);
    }

    public override string GetDescription() =>
        $"[{Name}] {MonthlyFee} грн/міс | {PricePerMinute} грн/хв " +
        $"(знижка {DiscountPercent}%) | +{IncludedMinutes} хв | " +
        $"{IncludedDataMB / 1024} ГБ інтернету";
}

/// <summary>
/// Запис про дзвінок абонента.
/// </summary>
public class CallRecord
{
    public int Id { get; set; }
    public int SubscriberId { get; set; }
    public Subscriber Subscriber { get; set; } = null!;
    public string DestinationNumber { get; set; } = string.Empty;
    public int DurationSeconds { get; set; }
    public decimal Cost { get; set; }
    public DateTime CallDateTime { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Запис про використання мобільного інтернету.
/// </summary>
public class DataUsageRecord
{
    public int Id { get; set; }
    public int SubscriberId { get; set; }
    public Subscriber Subscriber { get; set; } = null!;
    public int DataUsedMB { get; set; }
    public decimal Cost { get; set; }
    public DateTime UsageDateTime { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Місячний рахунок для абонента.
/// </summary>
public class Invoice
{
    public int Id { get; set; }
    public int SubscriberId { get; set; }
    public Subscriber Subscriber { get; set; } = null!;
    public int BillingMonth { get; set; }
    public int BillingYear { get; set; }
    public decimal TotalCallsCost { get; set; }
    public decimal TotalDataCost { get; set; }
    public decimal MonthlyFee { get; set; }
    public decimal TotalAmount => TotalCallsCost + TotalDataCost + MonthlyFee;
    public bool IsPaid { get; set; } = false;
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
}