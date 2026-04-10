# ChatGPT App — ASP.NET Core 8 + Onion Architecture

A production-ready REST API integrating OpenAI's ChatGPT, built with **ASP.NET Core 8**, **SQL Server**, **Entity Framework Core**, and **Onion (Clean) Architecture**.

---

## 📁 Folder Structure

```
ChatGPTApp/
├── ChatGPTApp.sln
├── setup.ps1                      ← First-run setup script
├── add-migration.ps1              ← Helper to add EF migrations
├── update-database.ps1            ← Helper to apply EF migrations
│
└── src/
    ├── Core/                      ← Inner layers (no external deps)
    │   ├── ChatGPTApp.Domain/
    │   │   ├── Common/
    │   │   │   └── BaseEntity.cs
    │   │   ├── Entities/
    │   │   │   ├── User.cs
    │   │   │   ├── Conversation.cs
    │   │   │   └── Message.cs
    │   │   └── Enums/
    │   │       └── Enums.cs
    │   │
    │   └── ChatGPTApp.Application/
    │       ├── DTOs/
    │       │   ├── ApiResponse.cs
    │       │   ├── Auth/AuthDtos.cs
    │       │   └── Chat/ChatDtos.cs
    │       ├── Interfaces/
    │       │   ├── Repositories/
    │       │   │   ├── IGenericRepository.cs   ← Generic CRUD
    │       │   │   └── IRepositories.cs        ← Specific + IUnitOfWork
    │       │   └── Services/
    │       │       └── IServices.cs
    │       ├── Services/
    │       │   ├── AuthService.cs
    │       │   ├── UserService.cs
    │       │   └── ChatService.cs
    │       └── Validators/
    │           └── Validators.cs              ← FluentValidation
    │
    ├── Infrastructure/            ← Outer layer (DB, external APIs)
    │   └── ChatGPTApp.Infrastructure/
    │       ├── Persistence/
    │       │   ├── Context/
    │       │   │   ├── AppDbContext.cs
    │       │   │   └── DbSeeder.cs            ← Seeds admin users
    │       │   └── Repositories/
    │       │       ├── GenericRepository.cs
    │       │       ├── Repositories.cs
    │       │       └── UnitOfWork.cs
    │       ├── Services/
    │       │   ├── JwtService.cs
    │       │   ├── OpenAIService.cs
    │       │   ├── EmailService.cs
    │       │   └── CurrentUserService.cs
    │       └── Extensions/
    │           └── ServiceCollectionExtensions.cs
    │
    └── Presentation/              ← Entry point
        └── ChatGPTApp.API/
            ├── Controllers/
            │   ├── AuthController.cs          ← Register/Login/ForgotPwd/ResetPwd
            │   ├── ChatController.cs          ← Send message, conversations
            │   ├── UsersController.cs         ← Admin panel (Admin/SuperAdmin)
            │   └── ProfileController.cs       ← User's own profile
            ├── Filters/
            │   └── ValidationFilter.cs
            ├── Middlewares/
            │   └── ExceptionMiddleware.cs
            ├── Extensions/
            │   └── ServiceCollectionExtensions.cs
            ├── Program.cs
            ├── appsettings.json
            └── appsettings.Development.json
```

---

## 🏗️ Architecture — Onion (Clean)

```
┌──────────────────────────────────────┐
│           Presentation (API)         │  ← Controllers, Middleware, Filters
├──────────────────────────────────────┤
│         Infrastructure               │  ← EF Core, JWT, OpenAI, Email, SMTP
├──────────────────────────────────────┤
│           Application                │  ← Services, DTOs, Validators, Interfaces
├──────────────────────────────────────┤
│             Domain                   │  ← Entities, Enums, BaseEntity
└──────────────────────────────────────┘
        Dependency Rule: outer → inner only
```

**Key patterns:**
- **Generic Repository** `IGenericRepository<T>` — type-safe CRUD for any entity
- **Unit of Work** — single `SaveChangesAsync` across multiple repos
- **Service layer** — all business logic lives here, never in controllers
- **ApiResponse<T>** — consistent success/error envelope on every endpoint

---

## 📦 NuGet Packages

### ChatGPTApp.Application
| Package | Purpose |
|---|---|
| `BCrypt.Net-Next 4.0.3` | Password hashing |
| `FluentValidation 11.9.0` | DTO validation |
| `FluentValidation.DependencyInjectionExtensions 11.9.0` | Auto-register validators |
| `Microsoft.Extensions.Logging.Abstractions 8.0.0` | ILogger interface |

### ChatGPTApp.Infrastructure
| Package | Purpose |
|---|---|
| `Microsoft.EntityFrameworkCore 8.0.0` | ORM core |
| `Microsoft.EntityFrameworkCore.SqlServer 8.0.0` | SQL Server provider |
| `Microsoft.EntityFrameworkCore.Tools 8.0.0` | EF CLI tools |
| `Microsoft.EntityFrameworkCore.Design 8.0.0` | Design-time EF |
| `Microsoft.AspNetCore.Authentication.JwtBearer 8.0.0` | JWT middleware |
| `System.IdentityModel.Tokens.Jwt 7.3.1` | JWT creation |
| `Microsoft.IdentityModel.Tokens 7.3.1` | Token validation |
| `BCrypt.Net-Next 4.0.3` | Password hashing |
| `Microsoft.Extensions.Http 8.0.0` | HttpClient factory |
| `Microsoft.AspNetCore.Http.Abstractions 2.2.0` | IHttpContextAccessor |
| `FluentValidation.AspNetCore 11.3.0` | Auto-validation integration |

### ChatGPTApp.API
| Package | Purpose |
|---|---|
| `Swashbuckle.AspNetCore 6.5.0` | Swagger / OpenAPI UI |
| `Microsoft.AspNetCore.Authentication.JwtBearer 8.0.0` | JWT auth |

---

## ⚡ Quick Start

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (local or Docker)
- OpenAI API Key

### 1. Configure settings
Edit `src/Presentation/ChatGPTApp.API/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ChatGPTAppDb;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Secret": "YOUR_SECRET_KEY_MIN_32_CHARACTERS_LONG",
    "Issuer": "ChatGPTApp",
    "Audience": "ChatGPTApp",
    "ExpiryMinutes": "60"
  },
  "OpenAI": {
    "ApiKey": "sk-YOUR_OPENAI_KEY"
  },
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": "587",
    "Username": "your@gmail.com",
    "Password": "your-app-password",
    "From": "your@gmail.com",
    "FromName": "ChatGPT App"
  }
}
```

### 2. Run the setup script
```powershell
# From solution root
.\setup.ps1
```

This will:
1. Restore all NuGet packages
2. Build the solution
3. Create the initial EF Core migration
4. Apply the migration (creates the database)
5. Seed default admin users

### 3. Run the API
```powershell
dotnet run --project src\Presentation\ChatGPTApp.API\ChatGPTApp.API.csproj
```

**Swagger UI:** http://localhost:5000 (root URL in Development)

---

## 🔑 Default Seeded Accounts

| Role | Email | Password |
|---|---|---|
| SuperAdmin | superadmin@chatgptapp.com | Admin@123456 |
| Admin | admin@chatgptapp.com | Admin@123456 |

---

## 🗺️ API Endpoints

### Auth — `api/auth`
| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/register` | ❌ | Register new user |
| POST | `/login` | ❌ | Login, receive JWT |
| POST | `/refresh-token` | ❌ | Refresh access token |
| POST | `/forgot-password` | ❌ | Send reset email |
| POST | `/reset-password` | ❌ | Reset with token |
| POST | `/change-password` | ✅ | Change own password |
| POST | `/logout` | ✅ | Invalidate refresh token |
| GET | `/me` | ✅ | Get current user info |

### Chat — `api/chat`
| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/send` | ✅ | Send message to ChatGPT |
| POST | `/conversations` | ✅ | Create conversation |
| GET | `/conversations` | ✅ | List conversations (paged) |
| GET | `/conversations/{id}` | ✅ | Get conversation + messages |
| DELETE | `/conversations/{id}` | ✅ | Delete conversation |

### Profile — `api/profile`
| Method | Endpoint | Auth | Description |
|---|---|---|---|
| GET | `/` | ✅ | Get my profile |
| PUT | `/` | ✅ | Update my profile |

### Admin Users — `api/admin/users`
| Method | Endpoint | Auth | Description |
|---|---|---|---|
| GET | `/` | 🔐 Admin | List all users (paged) |
| GET | `/{id}` | 🔐 Admin | Get user by ID |
| PUT | `/{id}` | 🔐 Admin | Update user |
| DELETE | `/{id}` | 🔐 SuperAdmin | Soft delete user |
| PATCH | `/{id}/toggle-active` | 🔐 Admin | Enable/disable user |
| PATCH | `/{id}/role` | 🔐 SuperAdmin | Change user role |

---

## 🔐 Auth Flow

```
1. POST /api/auth/login
   → { accessToken, refreshToken, user }

2. Use accessToken in header:
   Authorization: Bearer <accessToken>

3. When expired → POST /api/auth/refresh-token
   → { new accessToken, new refreshToken }

4. POST /api/auth/logout
   → Clears refresh token from DB
```

---

## 💬 Chat Flow

```
1. POST /api/chat/send
   Body: { "message": "Hello!", "model": "gpt-3.5-turbo" }
   → Creates new conversation automatically

2. Continue same conversation:
   Body: { "message": "Tell me more", "conversationId": "<guid>" }
   → Sends full history to OpenAI for context

3. GET /api/chat/conversations
   → Paginated list of your conversations

4. GET /api/chat/conversations/{id}
   → Full conversation with all messages
```

---

## 🛠️ EF Core Migration Commands

```powershell
# Add a new migration
.\add-migration.ps1 -Name "AddNewTable"

# Apply pending migrations
.\update-database.ps1

# Manual EF commands (from solution root)
dotnet ef migrations add <MigrationName> `
    --project src\Infrastructure\ChatGPTApp.Infrastructure\ChatGPTApp.Infrastructure.csproj `
    --startup-project src\Presentation\ChatGPTApp.API\ChatGPTApp.API.csproj `
    --output-dir Migrations

dotnet ef database update `
    --project src\Infrastructure\ChatGPTApp.Infrastructure\ChatGPTApp.Infrastructure.csproj `
    --startup-project src\Presentation\ChatGPTApp.API\ChatGPTApp.API.csproj
```

---

## 🐳 SQL Server with Docker (optional)

```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourPassword123!" \
  -p 1433:1433 --name sqlserver -d \
  mcr.microsoft.com/mssql/server:2022-latest
```

Connection string:
```
Server=localhost,1433;Database=ChatGPTAppDb;User Id=sa;Password=YourPassword123!;TrustServerCertificate=True;
```

---

## 📋 Response Format

All endpoints return a consistent `ApiResponse<T>`:

```json
{
  "success": true,
  "message": "Login successful.",
  "data": { ... },
  "errors": []
}
```

On failure:
```json
{
  "success": false,
  "message": null,
  "data": null,
  "errors": ["Invalid email or password."]
}
```
