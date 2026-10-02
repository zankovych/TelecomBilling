// ============================================================
// БЛОК 2: DATABASE CONTEXT (EF Core + SQLite)
// AppDbContext — єдина точка доступу до бази даних.
// Отримує налаштування через DI (Лаба 4 — IoC).
// ============================================================

using Microsoft.EntityFrameworkCore;
using TelecomSystem.Models;

namespace TelecomSystem.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Subscriber> Subscribers => Set<Subscriber>();
    public DbSet<Tariff> Tariffs => Set<Tariff>();
    public DbSet<CallRecord> CallRecords => Set<CallRecord>();
    public DbSet<DataUsageRecord> DataUsageRecords => Set<DataUsageRecord>();
    public DbSet<Invoice> Invoices => Set<Invoice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // TPH — всі тарифи в одній таблиці "Tariffs" з колонкою "TariffType"
        modelBuilder.Entity<Tariff>()
            .HasDiscriminator<string>("TariffType")
            .HasValue<StartTariff>("Start")
            .HasValue<BasicTariff>("Basic")
            .HasValue<PremiumTariff>("Premium")
            .HasValue<UnlimitedTariff>("Unlimited")
            .HasValue<CorporateTariff>("Corporate");

        // Точність decimal для SQLite
        modelBuilder.Entity<Subscriber>()
            .Property(s => s.Balance).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Tariff>()
            .Property(t => t.MonthlyFee).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Tariff>()
            .Property(t => t.PricePerMinute).HasColumnType("decimal(18,4)");
        modelBuilder.Entity<Tariff>()
            .Property(t => t.PricePerMB).HasColumnType("decimal(18,4)");
        modelBuilder.Entity<CorporateTariff>()
            .Property(t => t.DiscountPercent).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<CallRecord>()
            .Property(c => c.Cost).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<DataUsageRecord>()
            .Property(d => d.Cost).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Invoice>()
            .Property(i => i.TotalCallsCost).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Invoice>()
            .Property(i => i.TotalDataCost).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Invoice>()
            .Property(i => i.MonthlyFee).HasColumnType("decimal(18,2)");

        // Seed-дані: всі 5 тарифів одразу при першому запуску
        modelBuilder.Entity<StartTariff>().HasData(
            new StartTariff { Id = 1, Name = "Старт", MonthlyFee = 30m,
                PricePerMinute = 0.8m, IncludedMinutes = 0,
                IncludedDataMB = 0, PricePerMB = 0.05m });

        modelBuilder.Entity<BasicTariff>().HasData(
            new BasicTariff { Id = 2, Name = "Базовий", MonthlyFee = 50m,
                PricePerMinute = 0.5m, IncludedMinutes = 100,
                IncludedDataMB = 2048, PricePerMB = 0.03m });

        modelBuilder.Entity<PremiumTariff>().HasData(
            new PremiumTariff { Id = 3, Name = "Преміум", MonthlyFee = 120m,
                PricePerMinute = 0.3m, IncludedMinutes = 300,
                IncludedDataMB = 10240, PricePerMB = 0.02m });

        modelBuilder.Entity<UnlimitedTariff>().HasData(
            new UnlimitedTariff { Id = 4, Name = "Безліміт", MonthlyFee = 250m,
                PricePerMinute = 0.2m, IncludedMinutes = 1000,
                IncludedDataMB = 30720, PricePerMB = 0.01m });

        modelBuilder.Entity<CorporateTariff>().HasData(
            new CorporateTariff { Id = 5, Name = "Корпоративний", MonthlyFee = 400m,
                PricePerMinute = 0.4m, IncludedMinutes = 500,
                IncludedDataMB = 20480, PricePerMB = 0.015m, DiscountPercent = 20m });
    }
}