# TelecomBilling

Консольна білінгова система мобільного оператора на **C# / .NET 8**.
Дозволяє вести абонентів, тарифи, дзвінки та інтернет-трафік, формувати й оплачувати рахунки.

## Можливості

- Список абонентів, додавання нового абонента
- Поповнення балансу
- Реєстрація дзвінків та використання мобільного інтернету
- Формування та оплата рахунків
- Зміна тарифу (Старт, Базовий, Преміум, Безліміт, Корпоративний)
- Демо додаткових опцій тарифу (роумінг, додатковий трафік, SMS-пакет)

## Технології та патерни

- **.NET 8**, консольний застосунок
- **Entity Framework Core + SQLite** — зберігання даних
- **Microsoft.Extensions.DependencyInjection** — IoC-контейнер
- **xUnit + Moq** — unit-тести
- Патерни: **Factory Method** (створення тарифів), **Decorator** (опції тарифу),
  **Observer** (сповіщення про баланс), **Repository**

## Структура

```
TelecomSystem.sln
├── src/                        # основний застосунок
│   ├── Program.cs              # точка входу
│   ├── ConsoleMenuService.cs   # консольне меню
│   ├── Models.cs               # сутності та тарифи
│   ├── Patterns.cs             # Factory Method, Decorator, Observer
│   ├── Services.cs             # бізнес-логіка
│   ├── Repositories.cs         # доступ до даних
│   ├── AppDbContext.cs         # EF Core контекст
│   └── DependencyInjectionSetup.cs
└── tests/                      # unit-тести (xUnit + Moq)
    └── TelecomTests.cs
```

## Запуск

Потрібен [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```bash
git clone https://github.com/zankovych/TelecomBilling.git
cd TelecomBilling
dotnet run --project src
```

> База даних `telecom.db` створюється автоматично при запуску (з початковими тарифами)
> і перестворюється при кожному старті програми.

## Тести

```bash
dotnet test
```
