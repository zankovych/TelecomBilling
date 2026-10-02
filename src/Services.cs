// ============================================================
// БЛОК 6: СЕРВІСИ (бізнес-логіка)
// Принцип SRP: кожен сервіс — своя ділянка відповідальності.
// Принцип DIP: залежать від інтерфейсів, не від конкретних класів.
// DI через конструктор (Лаба 4 — IoC-контейнер).
// ============================================================

using TelecomSystem.Interfaces;
using TelecomSystem.Models;
using TelecomSystem.Patterns;

namespace TelecomSystem.Services;

public class SubscriberService : ISubscriberService
{
    private readonly ISubscriberRepository _subscriberRepo;
    private readonly ITariffRepository _tariffRepo;
    private readonly IBalanceSubject _balanceSubject; // Observer Pattern (Лаба 5)

    public SubscriberService(
        ISubscriberRepository subscriberRepo,
        ITariffRepository tariffRepo,
        IBalanceSubject balanceSubject)
    {
        _subscriberRepo = subscriberRepo;
        _tariffRepo = tariffRepo;
        _balanceSubject = balanceSubject;
    }

    public async Task<Subscriber?> GetSubscriberAsync(int id) =>
        await _subscriberRepo.GetByIdAsync(id);

    public async Task<IEnumerable<Subscriber>> GetAllSubscribersAsync() =>
        await _subscriberRepo.GetAllAsync();

    public async Task<Subscriber> CreateSubscriberAsync(
        string fullName, string phoneNumber, int tariffId)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("ПІБ не може бути порожнім");
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Номер телефону не може бути порожнім");

        var existing = await _subscriberRepo.GetByPhoneNumberAsync(phoneNumber);
        if (existing != null)
            throw new InvalidOperationException($"Номер '{phoneNumber}' вже зареєстрований");

        var tariff = await _tariffRepo.GetByIdAsync(tariffId)
            ?? throw new ArgumentException($"Тариф #{tariffId} не знайдений");

        var subscriber = new Subscriber
        {
            FullName = fullName,
            PhoneNumber = phoneNumber,
            TariffId = tariffId,
            Balance = 0m
        };

        await _subscriberRepo.AddAsync(subscriber);
        return subscriber;
    }

    public async Task ChangeTariffAsync(int subscriberId, int newTariffId)
    {
        var subscriber = await _subscriberRepo.GetByIdAsync(subscriberId)
            ?? throw new ArgumentException($"Абонент #{subscriberId} не знайдений");

        _ = await _tariffRepo.GetByIdAsync(newTariffId)
            ?? throw new ArgumentException($"Тариф #{newTariffId} не знайдений");

        subscriber.TariffId = newTariffId;
        await _subscriberRepo.UpdateAsync(subscriber);
    }

    public async Task TopUpBalanceAsync(int subscriberId, decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Сума поповнення має бути більше нуля");

        var subscriber = await _subscriberRepo.GetByIdAsync(subscriberId)
            ?? throw new ArgumentException($"Абонент #{subscriberId} не знайдений");

        subscriber.Balance += amount;
        await _subscriberRepo.UpdateAsync(subscriber);

        // Observer: сповіщаємо всіх підписаних спостерігачів
        _balanceSubject.NotifyObservers(subscriber, subscriber.Balance);
    }

    public async Task<bool> DeductBalanceAsync(int subscriberId, decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Сума списання має бути більше нуля");

        var subscriber = await _subscriberRepo.GetByIdAsync(subscriberId)
            ?? throw new ArgumentException($"Абонент #{subscriberId} не знайдений");

        subscriber.Balance -= amount;
        await _subscriberRepo.UpdateAsync(subscriber);

        // Observer: може спрацювати LowBalanceObserver або NegativeBalanceBlocker
        _balanceSubject.NotifyObservers(subscriber, subscriber.Balance);

        return subscriber.Balance >= 0;
    }
}

public class BillingService : IBillingService
{
    private readonly ICallRecordRepository _callRepo;
    private readonly IDataUsageRepository _dataRepo;
    private readonly IInvoiceRepository _invoiceRepo;
    private readonly ISubscriberRepository _subscriberRepo;
    private readonly ISubscriberService _subscriberService;

    public BillingService(
        ICallRecordRepository callRepo,
        IDataUsageRepository dataRepo,
        IInvoiceRepository invoiceRepo,
        ISubscriberRepository subscriberRepo,
        ISubscriberService subscriberService)
    {
        _callRepo = callRepo;
        _dataRepo = dataRepo;
        _invoiceRepo = invoiceRepo;
        _subscriberRepo = subscriberRepo;
        _subscriberService = subscriberService;
    }

    public async Task<CallRecord> RegisterCallAsync(
        int subscriberId, string destinationNumber, int durationSeconds)
    {
        if (durationSeconds <= 0)
            throw new ArgumentException("Тривалість дзвінка має бути більше нуля");
        if (string.IsNullOrWhiteSpace(destinationNumber))
            throw new ArgumentException("Номер призначення не може бути порожнім");

        var subscriber = await _subscriberRepo.GetByIdAsync(subscriberId)
            ?? throw new ArgumentException($"Абонент #{subscriberId} не знайдений");

        // Поліморфізм: кожен тариф рахує по-своєму
        decimal cost = subscriber.Tariff.CalculateCallCost(durationSeconds);

        var record = new CallRecord
        {
            SubscriberId = subscriberId,
            DestinationNumber = destinationNumber,
            DurationSeconds = durationSeconds,
            Cost = cost,
            CallDateTime = DateTime.UtcNow
        };

        await _callRepo.AddAsync(record);
        await _subscriberService.DeductBalanceAsync(subscriberId, cost);

        return record;
    }

    public async Task<DataUsageRecord> RegisterDataUsageAsync(int subscriberId, int dataMB)
    {
        if (dataMB <= 0)
            throw new ArgumentException("Обсяг даних має бути більше нуля");

        var subscriber = await _subscriberRepo.GetByIdAsync(subscriberId)
            ?? throw new ArgumentException($"Абонент #{subscriberId} не знайдений");

        // Поліморфізм: кожен тариф рахує інтернет по-своєму
        // (BasicTariff — в пакеті безкоштовно, StartTariff — завжди платно)
        decimal cost = subscriber.Tariff.CalculateDataCost(dataMB);

        var record = new DataUsageRecord
        {
            SubscriberId = subscriberId,
            DataUsedMB = dataMB,
            Cost = cost,
            UsageDateTime = DateTime.UtcNow
        };

        await _dataRepo.AddAsync(record);

        if (cost > 0)
            await _subscriberService.DeductBalanceAsync(subscriberId, cost);

        return record;
    }

    public async Task<Invoice> GenerateMonthlyInvoiceAsync(
        int subscriberId, int month, int year)
    {
        var existing = await _invoiceRepo.GetBySubscriberAndPeriodAsync(subscriberId, month, year);
        if (existing != null)
            throw new InvalidOperationException(
                $"Рахунок за {month:D2}/{year} вже існує (ID={existing.Id})");

        var subscriber = await _subscriberRepo.GetByIdAsync(subscriberId)
            ?? throw new ArgumentException($"Абонент #{subscriberId} не знайдений");

        var calls = await _callRepo.GetBySubscriberAndPeriodAsync(subscriberId, month, year);
        var dataUsages = await _dataRepo.GetBySubscriberAndPeriodAsync(subscriberId, month, year);

        var invoice = new Invoice
        {
            SubscriberId = subscriberId,
            BillingMonth = month,
            BillingYear = year,
            TotalCallsCost = calls.Sum(c => c.Cost),
            TotalDataCost = dataUsages.Sum(d => d.Cost),
            MonthlyFee = subscriber.Tariff.MonthlyFee,
            IsPaid = false,
            IssuedAt = DateTime.UtcNow
        };

        await _invoiceRepo.AddAsync(invoice);
        return invoice;
    }

    public async Task<bool> PayInvoiceAsync(int invoiceId)
    {
        var invoice = await _invoiceRepo.GetByIdAsync(invoiceId)
            ?? throw new ArgumentException($"Рахунок #{invoiceId} не знайдений");

        if (invoice.IsPaid)
            throw new InvalidOperationException($"Рахунок #{invoiceId} вже оплачений");

        bool success = await _subscriberService
            .DeductBalanceAsync(invoice.SubscriberId, invoice.TotalAmount);

        invoice.IsPaid = true;
        await _invoiceRepo.UpdateAsync(invoice);
        return success;
    }

    public async Task<decimal> GetTotalSpentBySubscriberAsync(int subscriberId)
    {
        var calls = await _callRepo.GetBySubscriberIdAsync(subscriberId);
        var data = await _dataRepo.GetBySubscriberIdAsync(subscriberId);
        return calls.Sum(c => c.Cost) + data.Sum(d => d.Cost);
    }
}

public class TariffService : ITariffService
{
    private readonly ITariffRepository _tariffRepo;

    public TariffService(ITariffRepository tariffRepo) => _tariffRepo = tariffRepo;

    public async Task<IEnumerable<Tariff>> GetAllTariffsAsync() =>
        await _tariffRepo.GetAllWithTypeAsync();

    public async Task<Tariff?> GetTariffAsync(int id) =>
        await _tariffRepo.GetByIdAsync(id);
}

