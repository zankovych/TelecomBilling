// ============================================================
// БЛОК 9: ТОЧКА ВХОДУ ПРОГРАМИ (Program.cs)
// ============================================================

using Microsoft.Extensions.DependencyInjection;
using TelecomSystem.Infrastructure;
using TelecomSystem.Services;

// --- КРОК 1: Збираємо IoC-контейнер ---
var serviceProvider = DependencyInjectionSetup.BuildServiceProvider("telecom.db");

// --- КРОК 2: Підключаємо Observer-спостерігачів ---
DependencyInjectionSetup.ConfigureObservers(serviceProvider);

// --- КРОК 3: Ініціалізуємо SQLite базу даних ---
// forceRecreate: true — видаляє стару БД якщо схема застаріла.
// Після першого успішного запуску можна змінити на false.
Console.WriteLine("⏳ Ініціалізація бази даних...");
await DependencyInjectionSetup.InitializeDatabaseAsync(serviceProvider, forceRecreate: true);
Console.WriteLine("✅ База даних готова!\n");

// --- КРОК 4: Запускаємо головне меню ---
var menuService = serviceProvider.GetRequiredService<ConsoleMenuService>();
await menuService.RunAsync();