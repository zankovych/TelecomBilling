// ============================================================
// БЛОК 3: ІНТЕРФЕЙСИ
// Принцип DIP: залежимо від абстракцій, не від конкретних класів.
// Принцип ISP: маленькі сфокусовані інтерфейси.
// ============================================================

using TelecomSystem.Models;

namespace TelecomSystem.Interfaces;

// ── РЕПОЗИТОРІЇ ──────────────────────────────────────────────

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(int id);
}

public interface ISubscriberRepository : IRepository<Subscriber>
{
    Task<Subscriber?> GetByPhoneNumberAsync(string phoneNumber);
    Task<IEnumerable<Subscriber>> GetWithLowBalanceAsync(decimal threshold);
}

public interface ITariffRepository : IRepository<Tariff>
{
    Task<IEnumerable<Tariff>> GetAllWithTypeAsync();
}

public interface ICallRecordRepository : IRepository<CallRecord>
{
    Task<IEnumerable<CallRecord>> GetBySubscriberIdAsync(int subscriberId);
    Task<IEnumerable<CallRecord>> GetBySubscriberAndPeriodAsync(
        int subscriberId, int month, int year);
}

public interface IDataUsageRepository : IRepository<DataUsageRecord>
{
    Task<IEnumerable<DataUsageRecord>> GetBySubscriberIdAsync(int subscriberId);
    Task<IEnumerable<DataUsageRecord>> GetBySubscriberAndPeriodAsync(
        int subscriberId, int month, int year);
}

public interface IInvoiceRepository : IRepository<Invoice>
{
    Task<IEnumerable<Invoice>> GetBySubscriberIdAsync(int subscriberId);
    Task<Invoice?> GetBySubscriberAndPeriodAsync(int subscriberId, int month, int year);
}

// ── СЕРВІСИ ──────────────────────────────────────────────────

public interface ISubscriberService
{
    Task<Subscriber?> GetSubscriberAsync(int id);
    Task<IEnumerable<Subscriber>> GetAllSubscribersAsync();
    Task<Subscriber> CreateSubscriberAsync(string fullName, string phoneNumber, int tariffId);
    Task ChangeTariffAsync(int subscriberId, int newTariffId);
    Task TopUpBalanceAsync(int subscriberId, decimal amount);
    Task<bool> DeductBalanceAsync(int subscriberId, decimal amount);
}

public interface IBillingService
{
    Task<CallRecord> RegisterCallAsync(int subscriberId, string destinationNumber, int durationSeconds);
    Task<DataUsageRecord> RegisterDataUsageAsync(int subscriberId, int dataMB);
    Task<Invoice> GenerateMonthlyInvoiceAsync(int subscriberId, int month, int year);
    Task<bool> PayInvoiceAsync(int invoiceId);
    Task<decimal> GetTotalSpentBySubscriberAsync(int subscriberId);
}

public interface ITariffService
{
    Task<IEnumerable<Tariff>> GetAllTariffsAsync();
    Task<Tariff?> GetTariffAsync(int id);
}

// ── ПАТЕРН "СПОСТЕРІГАЧ" (Поведінковий, Лаба 5) ─────────────

public interface IBalanceSubject
{
    void AttachObserver(IBalanceObserver observer);
    void DetachObserver(IBalanceObserver observer);
    void NotifyObservers(Subscriber subscriber, decimal newBalance);
}

public interface IBalanceObserver
{
    void OnBalanceChanged(Subscriber subscriber, decimal newBalance);
}

// ── ПАТЕРН "ФАБРИЧНИЙ МЕТОД" (Породжуючий, Лаба 5) ──────────

/// <summary>
/// Фабрика тарифів — єдина точка створення об'єктів тарифу.
/// Аналог з книги Будая: LoggerProviderFactory вирішує який логер створити.
/// </summary>
public interface ITariffFactory
{
    Tariff CreateTariff(string tariffType);
}