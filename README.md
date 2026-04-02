# 🔐 OAuthHub — Единый центр аутентификации

<div align="center">

![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-8.0-512BD4?logo=dotnet&logoColor=white)
![Entity Framework](https://img.shields.io/badge/EF%20Core-8.0-512BD4?logo=dotnet&logoColor=white)
![SQLite](https://img.shields.io/badge/SQLite-003B57?logo=sqlite&logoColor=white)
![Bootstrap](https://img.shields.io/badge/Bootstrap-5.3-7952B3?logo=bootstrap&logoColor=white)
![License](https://img.shields.io/badge/License-MIT-green)

**Веб-приложение для аутентификации пользователей через OAuth-провайдеров и локальную регистрацию с полноценной админ-панелью, управлением сессиями и системой блокировки.**

</div>

---

## 📋 Содержание

- [Возможности](#-возможности)
- [Стек технологий](#-стек-технологий)
- [Архитектура](#-архитектура)
- [Структура проекта](#-структура-проекта)
- [Установка и запуск](#-установка-и-запуск)
- [Настройка OAuth-провайдеров](#-настройка-oauth-провайдеров)
- [Учётная запись администратора](#-учётная-запись-администратора)
- [Система безопасности](#-система-безопасности)

---

## ✨ Возможности

### 👤 Для пользователей
- **Регистрация и вход** по Email/паролю
- **OAuth-аутентификация** через Google, Яндекс, Mail.ru
- **Привязка нескольких провайдеров** к одному аккаунту
- **Синхронизация профиля** (имя, аватар) из соцсетей
- **Двухфакторная аутентификация** (TOTP — Google Authenticator, Яндекс.Ключ)
- **Управление сессиями** — просмотр активных входов, завершение подозрительных
- **Смена/установка пароля**
- **Экспорт данных** в JSON и CSV (GDPR-совместимость)
- **Удаление аккаунта** с подтверждением

### 🛡️ Для администраторов
- **Панель управления** со сводной статистикой
- **Управление пользователями** — просмотр, блокировка/разблокировка
- **Реальная блокировка** — автоматическое завершение всех сессий, запрет входа
- **Просмотр всех сессий** системы с возможностью принудительного завершения
- **Реальное завершение сессий** — пользователь разлогинивается на следующем запросе
- **Журнал аудита** — все действия пользователей и администраторов

---

## 🛠 Стек технологий

| Компонент | Технология | Версия |
|-----------|-----------|--------|
| **Runtime** | .NET | 8.0 |
| **Web Framework** | ASP.NET Core MVC | 8.0 |
| **ORM** | Entity Framework Core (Code First) | 8.0 |
| **База данных** | SQLite | — |
| **Аутентификация** | ASP.NET Core Identity | 8.0 |
| **OAuth — Google** | Microsoft.AspNetCore.Authentication.Google | 8.0.0 |
| **OAuth — Яндекс** | AspNet.Security.OAuth.Yandex | 8.0.0 |
| **OAuth — Mail.ru** | AspNet.Security.OAuth.MailRu | 8.0.0 |
| **CSS-фреймворк** | Bootstrap | 5.3.2 |
| **Иконки** | Bootstrap Icons | 1.11.2 |

---

## 🏗 Архитектура
```
┌─────────────────────────────────────────────────────┐
│ HTTP Request │
├─────────────────────────────────────────────────────┤
│ Authentication Middleware │
├─────────────────────────────────────────────────────┤
│ Authorization Middleware │
├─────────────────────────────────────────────────────┤
│ BlockedUserMiddleware │
│ (проверяет блокировку → разлогинивает → /Blocked) │
├─────────────────────────────────────────────────────┤
│ SessionValidationMiddleware │
│ (проверяет cookie → сессия в БД → разлогинивает) │
├─────────────────────────────────────────────────────┤
│ MVC Pipeline │
│ Controllers → Views → Response │
└─────────────────────────────────────────────────────┘
```

### Потоки аутентификации
Локальный вход: Email/Пароль → Identity → Session Cookie + DB Session
OAuth вход: Provider → Callback → Identity → Session Cookie + DB Session
Блокировка: Admin → LockoutEnd = Max → Terminate Sessions → Middleware → /Blocked
Завершение сессии: Admin/User → IsActive=false → Middleware → SignOut → /Login?terminated=1


## 📁 Структура проекта
```
OAuthHub/
├── Controllers/
│ ├── HomeController.cs # Главная страница
│ ├── AccountController.cs # Вход, регистрация, OAuth, блокировка
│ ├── ManageController.cs # Профиль, пароль, 2FA, внешние аккаунты
│ ├── SessionController.cs # Управление сессиями пользователя
│ ├── ExportImportController.cs # Экспорт данных (JSON/CSV)
│ └── AdminController.cs # Админ-панель
│
├── Middleware/
│ ├── BlockedUserMiddleware.cs # Проверка блокировки на каждом запросе
│ └── SessionValidationMiddleware.cs # Проверка активности сессии в БД
│
├── Data/
│ ├── ApplicationDbContext.cs # Контекст EF Core
│ └── Migrations/ # Миграции базы данных
│
├── Models/
│ ├── Entities/
│ │ ├── ApplicationUser.cs # Расширенный IdentityUser
│ │ ├── UserSession.cs # Сессии пользователей
│ │ └── AuditLog.cs # Журнал аудита
│ └── ViewModels/
│ ├── LoginViewModel.cs
│ ├── LoginWith2faViewModel.cs
│ ├── RegisterViewModel.cs
│ ├── ManageViewModel.cs
│ ├── ChangePasswordViewModel.cs
│ ├── ExternalLoginsViewModel.cs
│ ├── Enable2faViewModel.cs
│ ├── ConfirmActionViewModel.cs
│ ├── ExportDataViewModel.cs
│ ├── SessionViewModel.cs
│ ├── ExternalLoginConfirmationViewModel.cs
│ └── ErrorViewModel.cs
│
├── Services/
│ ├── ISessionService.cs # Интерфейс сервиса сессий
│ ├── SessionService.cs # Реализация (CRUD сессий, проверка активности)
│ ├── IExportService.cs # Интерфейс экспорта
│ └── ExportService.cs # Экспорт в JSON/CSV
│
├── Views/
│ ├── Shared/
│ │ ├── _Layout.cshtml # Основной макет
│ │ ├── _LoginPartial.cshtml # Навигация пользователя
│ │ ├── _ValidationScriptsPartial.cshtml
│ │ └── Error.cshtml
│ ├── Home/
│ │ └── Index.cshtml # Главная страница
│ ├── Account/
│ │ ├── Login.cshtml # Форма входа
│ │ ├── Register.cshtml # Форма регистрации
│ │ ├── LoginWith2fa.cshtml # Ввод 2FA-кода
│ │ ├── ExternalLoginConfirmation.cshtml
│ │ ├── Blocked.cshtml # Страница блокировки
│ │ └── AccessDenied.cshtml
│ ├── Manage/
│ │ ├── Index.cshtml # Профиль пользователя
│ │ ├── ChangePassword.cshtml
│ │ ├── ExternalLogins.cshtml # Привязанные аккаунты
│ │ ├── TwoFactorAuthentication.cshtml
│ │ ├── DeleteAccount.cshtml
│ │ └── ConfirmAction.cshtml
│ ├── Session/
│ │ └── Index.cshtml # Сессии пользователя
│ ├── ExportImport/
│ │ └── Index.cshtml # Экспорт данных
│ └── Admin/
│ ├── Index.cshtml # Dashboard админки
│ ├── Users.cshtml # Управление пользователями
│ ├── Sessions.cshtml # Все сессии системы
│ └── Logs.cshtml # Журнал аудита
│
├── wwwroot/
│ ├── css/site.css # Кастомные стили
│ ├── js/site.js # Кастомные скрипты
│ └── favicon.ico
│
├── Program.cs # Точка входа, DI, middleware, seed
├── appsettings.json # Конфигурация (OAuth ключи, строка подключения)
├── appsettings.Development.json
├── OAuthHub.csproj # Зависимости проекта
└── README.md
```
---

## 🚀 Установка и запуск

### Предварительные требования

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Git

### 1. Клонирование репозитория

```bash
git clone https://github.com/Naviuki/OAuthHub.git
cd OAuthHub
```
2. Восстановление зависимостей
```bash
dotnet restore
```
3. Настройка OAuth-провайдеров
Отредактируйте файл appsettings.json (подробнее в разделе Настройка OAuth-провайдеров):

```JSON
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=oauthhub.db"
  },
  "Authentication": {
    "Google": {
      "ClientId": "ВАШ_GOOGLE_CLIENT_ID",
      "ClientSecret": "ВАШ_GOOGLE_CLIENT_SECRET"
    },
    "Yandex": {
      "ClientId": "ВАШ_YANDEX_CLIENT_ID",
      "ClientSecret": "ВАШ_YANDEX_CLIENT_SECRET"
    },
    "MailRu": {
      "ClientId": "ВАШ_MAILRU_CLIENT_ID",
      "ClientSecret": "ВАШ_MAILRU_CLIENT_SECRET"
    }
  }
}
```
4. Применение миграций и запуск
``` bash
dotnet run
```
⚡ Миграции применяются автоматически при запуске. База данных SQLite (oauthhub.db) создаётся в корне проекта.

5. Открытие в браузере
```
http://localhost:5285
```

🔑 Настройка OAuth-провайдеров
Google
Перейдите в Google Cloud Console
Создайте проект или выберите существующий
Перейдите в APIs & Services → Credentials
Нажмите Create Credentials → OAuth 2.0 Client IDs
Тип приложения: Web application
Добавьте Authorized redirect URIs:

https://localhost:7153/signin-google
http://localhost:5285/signin-google
Скопируйте Client ID и Client Secret в appsettings.json
Яндекс
Перейдите в Яндекс OAuth
Нажмите Зарегистрировать новое приложение
Заполните название, выберите Веб-сервисы
Добавьте Redirect URI:

https://localhost:7153/signin-yandex
http://localhost:5285/signin-yandex
В разделе Доступы отметьте:
✅ Доступ к логину, имени и фамилии, полу
✅ Доступ к адресу электронной почты
✅ Доступ к портрету пользователя
Скопируйте ID и Пароль приложения в appsettings.json
Mail.ru
Перейдите на OAuth Mail.ru
Нажмите Создать приложение
Тип: Веб-сайт
Добавьте Redirect URL:
```
https://localhost:7153/signin-mailru
http://localhost:5285/signin-mailru
```
В разделе Разрешения включите:
✅ Получение данных пользователя
Скопируйте ID приложения и Секретный ключ в appsettings.json
💡 Совет: Если вы не хотите подключать какой-то провайдер — оставьте значения "ClientId" и "ClientSecret" как есть. Провайдер с пустыми ключами не будет зарегистрирован, кнопка не появится.

👑 Учётная запись администратора
При первом запуске автоматически создаётся администратор:

Поле	Значение
Email	admin@oauthhub.com
Пароль	Admin123!
Роль	Admin
⚠️ Обязательно смените пароль администратора после первого входа!

Возможности администратора
Функция	Описание
Dashboard	Сводная статистика: пользователи, сессии, логи
Управление пользователями	Просмотр всех пользователей, блокировка/разблокировка
Управление сессиями	Просмотр всех активных сессий, принудительное завершение
Журнал аудита	Все действия в системе (входы, выходы, блокировки, регистрации)
🔒 Система безопасности
Блокировка пользователей
```
Админ нажимает "Заблокировать"
        │
        ▼
LockoutEnd = DateTimeOffset.MaxValue
        │
        ▼
Все активные сессии → IsActive = false
        │
        ▼
BlockedUserMiddleware (каждый запрос)
        │
        ├── Пользователь залогинен и заблокирован?
        │         │
        │        ДА → SignOut() → удаление cookie → /Account/Blocked
        │
        ▼
Попытка входа заблокированного
        │
        ▼
AccountController проверяет IsLockedOut → /Account/Blocked
Реальное завершение сессий
text

Админ/Пользователь завершает сессию
        │
        ▼
БД: session.IsActive = false
        │
        ▼
SessionValidationMiddleware (каждый запрос)
        │
        ├── Читает cookie "OAuthHub.SessionId"
        ├── Проверяет в БД: IsActive == true?
        │         │
        │        НЕТ → SignOut() → удаление cookie
        │               → /Account/Login?terminated=1
        │               → "Сессия была принудительно завершена"
        │
        ▼
Запрос проходит дальше (сессия активна)
Двухфакторная аутентификация (2FA)
Поддержка TOTP (Time-based One-Time Password)
Совместимость с Google Authenticator, Яндекс.Ключ, Microsoft Authenticator
Ручной ввод секретного ключа в приложение аутентификации
Возможность включения/отключения из профиля
```
