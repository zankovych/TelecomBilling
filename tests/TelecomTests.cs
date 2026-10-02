// ============================================================
// БЛОК 10: UNIT ТЕСТИ (Лаба 4 та 5)
// ============================================================
// Використовуємо xUnit + Moq.
//
// Moq — бібліотека для створення "підробок" (mock-об'єктів).
// Замість реального репозиторію (який звертається до SQLite)
// ми підставляємо mock, який повертає заздалегідь задані дані.
// Це і є одна з головних переваг DI (Лаба 4): тестованість!
//
// Тести покривають:
//   1. Тарифи (розрахунок вартості)
//   2. Фабричний Метод (патерн)
//   3. Декоратор (патерн)
//   4. Спостерігач (патерн)
//   5. Сервіс абонентів (позитивні та негативні сценарії)
//   6. Сервіс білінгу
// ============================================================

using Moq;
using TelecomSystem.Interfaces;
using TelecomSystem.Models;
using TelecomSystem.Patterns;
using TelecomSystem.Services;
using Xunit;

namespace TelecomSystem.Tests;

// ════════════════════════════════════════════════════════════
// ТЕСТИ ТАРИФІВ
// ════════════════════════════════════════════════════════════

public class TariffTests
{
    // ---- ПОЗИТИВНІ СЦЕНАРІЇ ----

    [Theory]
    [InlineData(60, 0.5)]   // 1 хвилина = 0.5 грн
    [InlineData(90, 1.0)]   // 1.5 хв → округлення до 2 хв = 1.0 грн
    [InlineData(120, 1.0)]  // 2 хвилини = 1.0 грн
    [InlineData(1, 0.5)]    // 1 секунда → округлення до 1 хв = 0.5 грн
    public void BasicTariff_CalculateCallCost_ReturnsCorrectAmount(
        int durationSeconds, decimal expectedCost)
    {
        // Arrange (підготовка)
        var tariff = new BasicTariff();

        // Act (дія)
        var cost = tariff.CalculateCallCost(durationSeconds);

        // Assert (перевірка)
        Assert.Equal(expectedCost, cost);
    }

    [Fact]
    public void PremiumTariff_CalculateCallCost_AppliesCorrectRate()
    {
        // Arrange
        var tariff = new PremiumTariff(); // 0.3 грн/хв

        // Act: 3 хвилини = 0.9 грн
        var cost = tariff.CalculateCallCost(180);

        // Assert
        Assert.Equal(0.9m, cost);
    }

    [Fact]
    public void CorporateTariff_CalculateCallCost_AppliesDiscount()
    {
        // Arrange
        var tariff = new CorporateTariff(); // 0.4 грн/хв, знижка 20%

        // Act: 1 хвилина = 0.4 * 0.8 = 0.32 грн
        var cost = tariff.CalculateCallCost(60);

        // Assert
        Assert.Equal(0.32m, cost);
    }

    [Fact]
    public void BasicTariff_GetDescription_ContainsTariffName()
    {
        var tariff = new BasicTariff();
        Assert.Contains("Базовий", tariff.GetDescription());
    }

    // ---- НЕГАТИВНІ СЦЕНАРІЇ ----

    [Fact]
    public void BasicTariff_CalculateCallCost_ZeroDuration_ReturnsZero()
    {
        // Arrange: 0 секунд — Math.Ceiling(0/60) = 0, тому вартість = 0
        var tariff = new BasicTariff();

        // Act
        var cost = tariff.CalculateCallCost(0);

        // Assert
        Assert.Equal(0m, cost);
    }
}

// ════════════════════════════════════════════════════════════
// ТЕСТИ ПАТЕРНУ: ФАБРИЧНИЙ МЕТОД
// ════════════════════════════════════════════════════════════

public class TariffFactoryTests
{
    private readonly TariffFactory _factory = new();

    // ---- ПОЗИТИВНІ СЦЕНАРІЇ ----

    [Theory]
    [InlineData("basic", typeof(BasicTariff))]
    [InlineData("Базовий", typeof(BasicTariff))]
    [InlineData("premium", typeof(PremiumTariff))]
    [InlineData("Преміум", typeof(PremiumTariff))]
    [InlineData("corporate", typeof(CorporateTariff))]
    [InlineData("Корпоративний", typeof(CorporateTariff))]
    public void CreateTariff_ValidType_ReturnsCorrectTariffType(
        string tariffType, Type expectedType)
    {
        // Act: фабрика вирішує що створити на основі рядка
        var tariff = _factory.CreateTariff(tariffType);

        // Assert: перевіряємо тип створеного об'єкту
        Assert.IsType(expectedType, tariff);
    }

    [Fact]
    public void CreateTariff_BasicType_HasCorrectMonthlyFee()
    {
        // Фабрика повертає вже налаштований об'єкт
        var tariff = _factory.CreateTariff("basic");
        Assert.Equal(50m, tariff.MonthlyFee);
    }

    // ---- НЕГАТИВНІ СЦЕНАРІЇ ----

    [Theory]
    [InlineData("unknown")]
    [InlineData("")]
    [InlineData("GOLD")]
    public void CreateTariff_InvalidType_ThrowsArgumentException(string invalidType)
    {
        // Act & Assert: фабрика має кинути виняток для невідомого типу
        Assert.Throws<ArgumentException>(() => _factory.CreateTariff(invalidType));
    }
}

// ════════════════════════════════════════════════════════════
// ТЕСТИ ПАТЕРНУ: ДЕКОРАТОР
// ════════════════════════════════════════════════════════════

public class DecoratorTests
{
    // ---- ПОЗИТИВНІ СЦЕНАРІЇ ----

    [Fact]
    public void RoamingDecorator_IncreasesCallCost()
    {
        // Arrange
        var basicTariff = new BasicTariff(); // 0.5 грн/хв
        var withRoaming = new RoamingDecorator(basicTariff, multiplier: 3.0m);

        // Act
        decimal baseCost = basicTariff.CalculateCallCost(60);    // 0.5 грн
        decimal roamingCost = withRoaming.CalculateCallCost(60); // 1.5 грн

        // Assert: роумінг збільшує вартість
        Assert.True(roamingCost > baseCost);
        Assert.Equal(baseCost * 3m, roamingCost);
    }

    [Fact]
    public void RoamingDecorator_IncreasesMonthlyFee()
    {
        // Arrange
        var basic = new BasicTariff();         // 50 грн/міс
        var withRoaming = new RoamingDecorator(basic, monthlyFee: 50m); // +50 грн

        // Assert: абонплата зросла
        Assert.Equal(basic.MonthlyFee + 50m, withRoaming.MonthlyFee);
    }

    [Fact]
    public void SmsPackageDecorator_DoesNotChangeCallCost()
    {
        // SMS-пакет НЕ впливає на вартість дзвінків — тільки абонплата
        var basic = new BasicTariff();
        var withSms = new SmsPackageDecorator(basic);

        Assert.Equal(
            basic.CalculateCallCost(180),
            withSms.CalculateCallCost(180));
    }

    [Fact]
    public void CombinedDecorators_WorkCorrectly()
    {
        // Демо: SMS + Роумінг — комбінація декораторів
        var basic = new BasicTariff();
        var combined = new RoamingDecorator(new SmsPackageDecorator(basic));

        // Опис містить обидва декоратори
        Assert.Contains("Роумінг", combined.GetDescription());
        Assert.Contains("SMS", combined.GetDescription());
    }

    [Fact]
    public void RoamingDecorator_GetDescription_ContainsOriginalAndExtension()
    {
        var basic = new BasicTariff();
        var withRoaming = new RoamingDecorator(basic);
        var description = withRoaming.GetDescription();

        Assert.Contains("Базовий", description);
        Assert.Contains("Роумінг", description);
    }
}

// ════════════════════════════════════════════════════════════
// ТЕСТИ ПАТЕРНУ: СПОСТЕРІГАЧ
// ════════════════════════════════════════════════════════════

public class ObserverTests
{
    // ---- ПОЗИТИВНІ СЦЕНАРІЇ ----

    [Fact]
    public void LowBalanceObserver_Triggers_WhenBalanceBelowThreshold()
    {
        // Arrange
        var observer = new NegativeBalanceBlocker(); // Використовуємо блокер для перевірки стану
        var manager = new BalanceNotificationManager();
        manager.AttachObserver(observer);

        var subscriber = new Subscriber { Id = 1, FullName = "Тест", PhoneNumber = "0670000000" };

        // Act: сповіщаємо про від'ємний баланс
        manager.NotifyObservers(subscriber, -5m);

        // Assert: блокер спрацював
        Assert.True(observer.WasBlocked);
        Assert.Equal("0670000000", observer.BlockedSubscriberPhone);
    }

    [Fact]
    public void Observer_NotTriggered_WhenBalancePositive()
    {
        // Arrange
        var observer = new NegativeBalanceBlocker();
        var manager = new BalanceNotificationManager();
        manager.AttachObserver(observer);

        var subscriber = new Subscriber { Id = 1, FullName = "Тест", PhoneNumber = "0670000001" };

        // Act: позитивний баланс
        manager.NotifyObservers(subscriber, 100m);

        // Assert: блокер НЕ спрацював
        Assert.False(observer.WasBlocked);
    }

    [Fact]
    public void MultipleObservers_AllNotified()
    {
        // Arrange: два незалежні спостерігачі
        var blocker1 = new NegativeBalanceBlocker();
        var blocker2 = new NegativeBalanceBlocker();
        var manager = new BalanceNotificationManager();
        manager.AttachObserver(blocker1);
        manager.AttachObserver(blocker2);

        var subscriber = new Subscriber { Id = 1, FullName = "Тест", PhoneNumber = "0670000002" };

        // Act
        manager.NotifyObservers(subscriber, -1m);

        // Assert: обидва сповіщені
        Assert.True(blocker1.WasBlocked);
        Assert.True(blocker2.WasBlocked);
    }

    [Fact]
    public void DetachObserver_StopsNotifications()
    {
        // Arrange
        var blocker = new NegativeBalanceBlocker();
        var manager = new BalanceNotificationManager();
        manager.AttachObserver(blocker);

        var subscriber = new Subscriber { Id = 1, FullName = "Тест", PhoneNumber = "0670000003" };

        // Act: відписуємося і потім сповіщаємо
        manager.DetachObserver(blocker);
        manager.NotifyObservers(subscriber, -100m);

        // Assert: спостерігач НЕ отримав сповіщення
        Assert.False(blocker.WasBlocked);
    }
}

// ════════════════════════════════════════════════════════════
// ТЕСТИ СЕРВІСУ АБОНЕНТІВ (з Moq mock-об'єктами)
// ════════════════════════════════════════════════════════════

public class SubscriberServiceTests
{
    // Фабричний метод для підготовки сервісу з mock-залежностями
    private static (SubscriberService service,
                    Mock<ISubscriberRepository> subscriberRepo,
                    Mock<ITariffRepository> tariffRepo,
                    BalanceNotificationManager manager)
        CreateService()
    {
        var subscriberRepo = new Mock<ISubscriberRepository>();
        var tariffRepo = new Mock<ITariffRepository>();
        var manager = new BalanceNotificationManager();

        var service = new SubscriberService(
            subscriberRepo.Object,
            tariffRepo.Object,
            manager);

        return (service, subscriberRepo, tariffRepo, manager);
    }

    // ---- ПОЗИТИВНІ СЦЕНАРІЇ ----

    [Fact]
    public async Task CreateSubscriber_ValidData_CreatesSuccessfully()
    {
        // Arrange
        var (service, subscriberRepo, tariffRepo, _) = CreateService();

        // Mock: тариф існує
        tariffRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new BasicTariff { Id = 1 });

        // Mock: номер унікальний
        subscriberRepo.Setup(r => r.GetByPhoneNumberAsync("0671234567"))
            .ReturnsAsync((Subscriber?)null);

        // Mock: збереження проходить успішно
        subscriberRepo.Setup(r => r.AddAsync(It.IsAny<Subscriber>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await service.CreateSubscriberAsync(
            "Іваненко Іван", "0671234567", 1);

        // Assert
        Assert.Equal("Іваненко Іван", result.FullName);
        Assert.Equal("0671234567", result.PhoneNumber);
        Assert.Equal(1, result.TariffId);
        // Перевіряємо що AddAsync викликався один раз
        subscriberRepo.Verify(r => r.AddAsync(It.IsAny<Subscriber>()), Times.Once);
    }

    [Fact]
    public async Task TopUpBalance_ValidAmount_IncreasesBalance()
    {
        // Arrange
        var (service, subscriberRepo, _, _) = CreateService();

        var subscriber = new Subscriber
        {
            Id = 1,
            FullName = "Тест",
            PhoneNumber = "0670000000",
            Balance = 50m,
            Tariff = new BasicTariff()
        };

        subscriberRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(subscriber);
        subscriberRepo.Setup(r => r.UpdateAsync(It.IsAny<Subscriber>()))
            .Returns(Task.CompletedTask);

        // Act
        await service.TopUpBalanceAsync(1, 100m);

        // Assert: баланс збільшився
        Assert.Equal(150m, subscriber.Balance);
    }

    // ---- НЕГАТИВНІ СЦЕНАРІЇ ----

    [Fact]
    public async Task CreateSubscriber_EmptyName_ThrowsArgumentException()
    {
        // Arrange
        var (service, _, _, _) = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateSubscriberAsync("", "0671234567", 1));
    }

    [Fact]
    public async Task CreateSubscriber_DuplicatePhone_ThrowsInvalidOperationException()
    {
        // Arrange
        var (service, subscriberRepo, tariffRepo, _) = CreateService();

        // Mock: тариф існує
        tariffRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new BasicTariff { Id = 1 });

        // Mock: номер вже зайнятий
        subscriberRepo.Setup(r => r.GetByPhoneNumberAsync("0671234567"))
            .ReturnsAsync(new Subscriber { PhoneNumber = "0671234567" });

        // Act & Assert: має кинути виняток
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateSubscriberAsync("Нова Людина", "0671234567", 1));
    }

    [Fact]
    public async Task TopUpBalance_NegativeAmount_ThrowsArgumentException()
    {
        // Arrange
        var (service, _, _, _) = CreateService();

        // Act & Assert: від'ємна сума — виняток
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.TopUpBalanceAsync(1, -50m));
    }

    [Fact]
    public async Task TopUpBalance_ZeroAmount_ThrowsArgumentException()
    {
        var (service, _, _, _) = CreateService();
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.TopUpBalanceAsync(1, 0m));
    }

    [Fact]
    public async Task GetSubscriber_NonExistentId_ReturnsNull()
    {
        // Arrange
        var (service, subscriberRepo, _, _) = CreateService();
        subscriberRepo.Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((Subscriber?)null);

        // Act
        var result = await service.GetSubscriberAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateSubscriber_NonExistentTariff_ThrowsArgumentException()
    {
        // Arrange
        var (service, subscriberRepo, tariffRepo, _) = CreateService();

        tariffRepo.Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((Tariff?)null); // Тариф не знайдено

        subscriberRepo.Setup(r => r.GetByPhoneNumberAsync(It.IsAny<string>()))
            .ReturnsAsync((Subscriber?)null);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateSubscriberAsync("Тест Тестович", "0670000099", 999));
    }
}

// ════════════════════════════════════════════════════════════
// ТЕСТИ СЕРВІСУ БІЛІНГУ
// ════════════════════════════════════════════════════════════

public class BillingServiceTests
{
    private static (BillingService service,
                    Mock<ICallRecordRepository> callRepo,
                    Mock<IInvoiceRepository> invoiceRepo,
                    Mock<ISubscriberRepository> subscriberRepo,
                    Mock<ISubscriberService> subscriberService)
        CreateBillingService()
    {
        var callRepo = new Mock<ICallRecordRepository>();
        var invoiceRepo = new Mock<IInvoiceRepository>();
        var subscriberRepo = new Mock<ISubscriberRepository>();
        var subscriberService = new Mock<ISubscriberService>();

        var service = new BillingService(
            callRepo.Object,
            invoiceRepo.Object,
            subscriberRepo.Object,
            subscriberService.Object);

        return (service, callRepo, invoiceRepo, subscriberRepo, subscriberService);
    }

    // ---- ПОЗИТИВНІ СЦЕНАРІЇ ----

    [Fact]
    public async Task RegisterCall_ValidData_ReturnsCallRecordWithCorrectCost()
    {
        // Arrange
        var (service, callRepo, _, subscriberRepo, subscriberService) =
            CreateBillingService();

        var basicTariff = new BasicTariff(); // 0.5 грн/хв

        subscriberRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Subscriber
            {
                Id = 1,
                PhoneNumber = "0671234567",
                Tariff = basicTariff,
                Balance = 100m
            });

        callRepo.Setup(r => r.AddAsync(It.IsAny<CallRecord>()))
            .Returns(Task.CompletedTask);

        subscriberService.Setup(s => s.DeductBalanceAsync(1, It.IsAny<decimal>()))
            .ReturnsAsync(true);

        // Act: дзвінок 120 секунд = 2 хвилини = 1.0 грн
        var result = await service.RegisterCallAsync(1, "0679876543", 120);

        // Assert
        Assert.Equal(1.0m, result.Cost);
        Assert.Equal("0679876543", result.DestinationNumber);
        Assert.Equal(120, result.DurationSeconds);
    }

    [Fact]
    public async Task GenerateMonthlyInvoice_NewInvoice_CalculatesCorrectTotal()
    {
        // Arrange
        var (service, callRepo, invoiceRepo, subscriberRepo, _) =
            CreateBillingService();

        var tariff = new BasicTariff { MonthlyFee = 50m };
        subscriberRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Subscriber
            {
                Id = 1,
                Tariff = tariff
            });

        // Рахунок ще не існує
        invoiceRepo.Setup(r => r.GetBySubscriberAndPeriodAsync(1, 4, 2026))
            .ReturnsAsync((Invoice?)null);

        // Два дзвінки по 10 грн
        callRepo.Setup(r => r.GetBySubscriberAndPeriodAsync(1, 4, 2026))
            .ReturnsAsync(new List<CallRecord>
            {
                new() { Cost = 10m },
                new() { Cost = 10m }
            });

        invoiceRepo.Setup(r => r.AddAsync(It.IsAny<Invoice>()))
            .Returns(Task.CompletedTask);

        // Act
        var invoice = await service.GenerateMonthlyInvoiceAsync(1, 4, 2026);

        // Assert: 50 (абонплата) + 20 (дзвінки) = 70 грн
        Assert.Equal(20m, invoice.TotalCallsCost);
        Assert.Equal(50m, invoice.MonthlyFee);
        Assert.Equal(70m, invoice.TotalAmount);
        Assert.False(invoice.IsPaid);
    }

    // ---- НЕГАТИВНІ СЦЕНАРІЇ ----

    [Fact]
    public async Task RegisterCall_ZeroDuration_ThrowsArgumentException()
    {
        var (service, _, _, _, _) = CreateBillingService();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.RegisterCallAsync(1, "0679876543", 0));
    }

    [Fact]
    public async Task RegisterCall_EmptyDestination_ThrowsArgumentException()
    {
        var (service, _, _, _, _) = CreateBillingService();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.RegisterCallAsync(1, "", 60));
    }

    [Fact]
    public async Task GenerateInvoice_DuplicateInvoice_ThrowsInvalidOperationException()
    {
        // Arrange
        var (service, _, invoiceRepo, subscriberRepo, _) = CreateBillingService();

        subscriberRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Subscriber { Id = 1, Tariff = new BasicTariff() });

        // Рахунок вже існує
        invoiceRepo.Setup(r => r.GetBySubscriberAndPeriodAsync(1, 4, 2026))
            .ReturnsAsync(new Invoice { Id = 5 });

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GenerateMonthlyInvoiceAsync(1, 4, 2026));
    }

    [Fact]
    public async Task RegisterCall_NonExistentSubscriber_ThrowsArgumentException()
    {
        var (service, _, _, subscriberRepo, _) = CreateBillingService();

        subscriberRepo.Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((Subscriber?)null);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.RegisterCallAsync(999, "0671234567", 60));
    }
}