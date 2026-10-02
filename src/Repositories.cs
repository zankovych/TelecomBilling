// ============================================================
// БЛОК 5: РЕПОЗИТОРІЇ — шар доступу до даних
// Принцип SRP: тільки CRUD-операції, жодної бізнес-логіки.
// Принцип DIP: сервіси залежать від інтерфейсів, не від EF Core напряму.
// ============================================================

using Microsoft.EntityFrameworkCore;
using TelecomSystem.Data;
using TelecomSystem.Interfaces;
using TelecomSystem.Models;

namespace TelecomSystem.Repositories;

public class SubscriberRepository : ISubscriberRepository
{
    private readonly AppDbContext _context;

    public SubscriberRepository(AppDbContext context) => _context = context;

    public async Task<Subscriber?> GetByIdAsync(int id) =>
        await _context.Subscribers.Include(s => s.Tariff).FirstOrDefaultAsync(s => s.Id == id);

    public async Task<IEnumerable<Subscriber>> GetAllAsync() =>
        await _context.Subscribers.Include(s => s.Tariff).ToListAsync();

    public async Task AddAsync(Subscriber entity)
    {
        await _context.Subscribers.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Subscriber entity)
    {
        _context.Subscribers.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var s = await GetByIdAsync(id);
        if (s != null) { _context.Subscribers.Remove(s); await _context.SaveChangesAsync(); }
    }

    public async Task<Subscriber?> GetByPhoneNumberAsync(string phoneNumber) =>
        await _context.Subscribers.Include(s => s.Tariff)
            .FirstOrDefaultAsync(s => s.PhoneNumber == phoneNumber);

    public async Task<IEnumerable<Subscriber>> GetWithLowBalanceAsync(decimal threshold) =>
        await _context.Subscribers.Include(s => s.Tariff)
            .Where(s => s.Balance < threshold).ToListAsync();
}

public class TariffRepository : ITariffRepository
{
    private readonly AppDbContext _context;

    public TariffRepository(AppDbContext context) => _context = context;

    public async Task<Tariff?> GetByIdAsync(int id) => await _context.Tariffs.FindAsync(id);
    public async Task<IEnumerable<Tariff>> GetAllAsync() => await _context.Tariffs.ToListAsync();
    public async Task<IEnumerable<Tariff>> GetAllWithTypeAsync() => await _context.Tariffs.ToListAsync();

    public async Task AddAsync(Tariff entity)
    {
        await _context.Tariffs.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Tariff entity)
    {
        _context.Tariffs.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var t = await GetByIdAsync(id);
        if (t != null) { _context.Tariffs.Remove(t); await _context.SaveChangesAsync(); }
    }
}

public class CallRecordRepository : ICallRecordRepository
{
    private readonly AppDbContext _context;

    public CallRecordRepository(AppDbContext context) => _context = context;

    public async Task<CallRecord?> GetByIdAsync(int id) => await _context.CallRecords.FindAsync(id);
    public async Task<IEnumerable<CallRecord>> GetAllAsync() => await _context.CallRecords.ToListAsync();

    public async Task AddAsync(CallRecord entity)
    {
        await _context.CallRecords.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(CallRecord entity)
    {
        _context.CallRecords.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var c = await GetByIdAsync(id);
        if (c != null) { _context.CallRecords.Remove(c); await _context.SaveChangesAsync(); }
    }

    public async Task<IEnumerable<CallRecord>> GetBySubscriberIdAsync(int subscriberId) =>
        await _context.CallRecords
            .Where(c => c.SubscriberId == subscriberId)
            .OrderByDescending(c => c.CallDateTime)
            .ToListAsync();

    public async Task<IEnumerable<CallRecord>> GetBySubscriberAndPeriodAsync(
        int subscriberId, int month, int year) =>
        await _context.CallRecords
            .Where(c => c.SubscriberId == subscriberId
                     && c.CallDateTime.Month == month
                     && c.CallDateTime.Year == year)
            .ToListAsync();
}

public class DataUsageRepository : IDataUsageRepository
{
    private readonly AppDbContext _context;

    public DataUsageRepository(AppDbContext context) => _context = context;

    public async Task<DataUsageRecord?> GetByIdAsync(int id) =>
        await _context.DataUsageRecords.FindAsync(id);

    public async Task<IEnumerable<DataUsageRecord>> GetAllAsync() =>
        await _context.DataUsageRecords.ToListAsync();

    public async Task AddAsync(DataUsageRecord entity)
    {
        await _context.DataUsageRecords.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(DataUsageRecord entity)
    {
        _context.DataUsageRecords.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var d = await GetByIdAsync(id);
        if (d != null) { _context.DataUsageRecords.Remove(d); await _context.SaveChangesAsync(); }
    }

    public async Task<IEnumerable<DataUsageRecord>> GetBySubscriberIdAsync(int subscriberId) =>
        await _context.DataUsageRecords
            .Where(d => d.SubscriberId == subscriberId)
            .OrderByDescending(d => d.UsageDateTime)
            .ToListAsync();

    public async Task<IEnumerable<DataUsageRecord>> GetBySubscriberAndPeriodAsync(
        int subscriberId, int month, int year) =>
        await _context.DataUsageRecords
            .Where(d => d.SubscriberId == subscriberId
                     && d.UsageDateTime.Month == month
                     && d.UsageDateTime.Year == year)
            .ToListAsync();
}

public class InvoiceRepository : IInvoiceRepository
{
    private readonly AppDbContext _context;

    public InvoiceRepository(AppDbContext context) => _context = context;

    public async Task<Invoice?> GetByIdAsync(int id) =>
        await _context.Invoices.Include(i => i.Subscriber).FirstOrDefaultAsync(i => i.Id == id);

    public async Task<IEnumerable<Invoice>> GetAllAsync() =>
        await _context.Invoices.Include(i => i.Subscriber).ToListAsync();

    public async Task AddAsync(Invoice entity)
    {
        await _context.Invoices.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Invoice entity)
    {
        _context.Invoices.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var inv = await GetByIdAsync(id);
        if (inv != null) { _context.Invoices.Remove(inv); await _context.SaveChangesAsync(); }
    }

    public async Task<IEnumerable<Invoice>> GetBySubscriberIdAsync(int subscriberId) =>
        await _context.Invoices.Include(i => i.Subscriber)
            .Where(i => i.SubscriberId == subscriberId)
            .OrderByDescending(i => i.BillingYear).ThenByDescending(i => i.BillingMonth)
            .ToListAsync();

    public async Task<Invoice?> GetBySubscriberAndPeriodAsync(int subscriberId, int month, int year) =>
        await _context.Invoices
            .FirstOrDefaultAsync(i =>
                i.SubscriberId == subscriberId &&
                i.BillingMonth == month &&
                i.BillingYear == year);
}