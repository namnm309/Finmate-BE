# Finmate Backend - Personal Finance Management API

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-14+-336791)](https://www.postgresql.org/)
[![Clerk](https://img.shields.io/badge/Auth-Clerk-6C47FF)](https://clerk.com/)

Backend API cho ứng dụng quản lý tài chính cá nhân Finmate, được xây dựng với .NET 8, sử dụng Clerk cho authentication và PostgreSQL (Supabase) làm database.

---

## 📋 Mục lục

- [Tổng quan kiến trúc](#-tổng-quan-kiến-trúc)
- [Cấu trúc dự án](#-cấu-trúc-dự-án)
- [Database Schema](#-database-schema)
- [Authentication với Clerk](#-authentication-với-clerk)
- [Cài đặt và Chạy dự án](#-cài-đặt-và-chạy-dự-án)
- [API Endpoints](#-api-endpoints)
- [Webhook Events](#-webhook-events)

---

## 🏗 Tổng quan kiến trúc

Dự án sử dụng kiến trúc **3-layer** (Three-tier Architecture):

```
┌─────────────────────────────────────────────────────┐
│          Presentation Layer (FinmateController)     │
│  - API Controllers                                  │
│  - JWT Authentication Middleware                    │
│  - Swagger UI                                       │
└──────────────────┬──────────────────────────────────┘
                   │
┌──────────────────▼──────────────────────────────────┐
│          Business Logic Layer (BLL)                 │
│  - Services (UserService, TransactionService, ...)  │
│  - DTOs (Request/Response)                          │
│  - Business Rules & Validation                      │
└──────────────────┬──────────────────────────────────┘
                   │
┌──────────────────▼──────────────────────────────────┐
│          Data Access Layer (DAL)                    │
│  - DbContext (Entity Framework Core)                │
│  - Repositories (UserRepository, ...)               │
│  - Models (Entity Classes)                          │
│  - Migrations                                       │
└─────────────────────────────────────────────────────┘
                   │
                   ▼
            PostgreSQL Database
            (Supabase Hosted)
```

### Công nghệ sử dụng

- **.NET 8**: Framework chính
- **ASP.NET Core Web API**: RESTful API
- **Entity Framework Core 9.0**: ORM
- **PostgreSQL**: Database (Supabase)
- **Clerk**: Authentication & User Management
- **Swagger/OpenAPI**: API Documentation

---

## 📁 Cấu trúc dự án

```
Finmate-BE/
├── FinmateController/          # Presentation Layer
│   ├── Controllers/
│   │   ├── AuthController.cs           # Authentication endpoints
│   │   ├── UserController.cs           # User management
│   │   ├── ClerkWebhookController.cs   # Clerk webhooks
│   │   ├── TransactionController.cs    # Transaction CRUD
│   │   ├── CategoryController.cs       # Category management
│   │   ├── MoneySourceController.cs    # Money source (accounts)
│   │   ├── ContactController.cs        # Contact management
│   │   └── ReportController.cs         # Financial reports
│   ├── Program.cs              # Application entry point & config
│   └── appsettings.json        # Configuration (DB, Clerk keys)
│
├── BLL/                        # Business Logic Layer
│   ├── Services/
│   │   ├── UserService.cs              # User business logic
│   │   ├── ClerkService.cs             # Clerk API integration
│   │   ├── TransactionService.cs       # Transaction logic
│   │   ├── CategoryService.cs          # Category logic
│   │   ├── MoneySourceService.cs       # Money source logic
│   │   ├── ContactService.cs           # Contact logic
│   │   └── ReportService.cs            # Report generation
│   └── DTOs/
│       ├── Request/            # API request models
│       └── Response/           # API response models
│
└── DAL/                        # Data Access Layer
    ├── Data/
    │   ├── FinmateContext.cs           # EF Core DbContext
    │   └── FinmateFactory.cs           # Design-time factory
    ├── Models/
    │   ├── Users.cs                    # User entity
    │   ├── Transaction.cs              # Transaction entity
    │   ├── Category.cs                 # Category entity
    │   ├── MoneySource.cs              # Money source entity
    │   ├── Contact.cs                  # Contact entity
    │   ├── AccountType.cs              # Account type (seed data)
    │   ├── TransactionType.cs          # Transaction type (seed data)
    │   └── Currency.cs                 # Currency (seed data)
    ├── Repositories/
    │   ├── IUserRepository.cs          # User repository interface
    │   ├── UserRepository.cs           # User repository implementation
    │   └── ...                         # Other repositories
    └── Migrations/                     # EF Core migrations
```

---

## 🗄 Database Schema

### Core Tables

#### `tbl_users` - Người dùng
```sql
- Id (UUID, PK)
- ClerkUserId (VARCHAR, UNIQUE) -- Clerk User ID
- Email (VARCHAR, REQUIRED)
- FullName (VARCHAR, REQUIRED)
- PhoneNumber (VARCHAR)
- AvatarUrl (VARCHAR)
- Address (VARCHAR)
- Occupation (VARCHAR)
- DateOfBirth (TIMESTAMP)
- CurrencyPreference (VARCHAR, DEFAULT 'VND')
- LanguagePreference (VARCHAR, DEFAULT 'vi')
- IsActive (BOOLEAN, DEFAULT true)
- IsPremium (BOOLEAN, DEFAULT false)
- PasswordHash (VARCHAR) -- Empty for Clerk users
- CreatedAt, UpdatedAt, LastLoginAt
```

#### `tbl_transactions` - Giao dịch
```sql
- Id (UUID, PK)
- UserId (UUID, FK -> tbl_users)
- TransactionTypeId (UUID, FK -> tbl_transaction_types)
- CategoryId (UUID, FK -> tbl_categories)
- MoneySourceId (UUID, FK -> tbl_money_sources)
- ContactId (UUID, FK -> tbl_contacts, NULLABLE)
- Amount (DECIMAL(18,2))
- Description (VARCHAR)
- TransactionDate (TIMESTAMP)
- IsFee (BOOLEAN)
- IsBorrowingForThis (BOOLEAN)
- ExcludeFromReport (BOOLEAN)
- CreatedAt, UpdatedAt
```

#### `tbl_money_sources` - Nguồn tiền (Tài khoản)
```sql
- Id (UUID, PK)
- UserId (UUID, FK -> tbl_users)
- AccountTypeId (UUID, FK -> tbl_account_types)
- Name (VARCHAR)
- Balance (DECIMAL(18,2))
- Currency (VARCHAR)
- Icon (VARCHAR)
- Color (VARCHAR)
- IsActive (BOOLEAN)
- CreatedAt, UpdatedAt
```

#### `tbl_categories` - Danh mục giao dịch
```sql
- Id (UUID, PK)
- UserId (UUID, FK -> tbl_users)
- TransactionTypeId (UUID, FK -> tbl_transaction_types)
- Name (VARCHAR)
- Icon (VARCHAR)
- DisplayOrder (INT)
- IsActive (BOOLEAN)
- CreatedAt, UpdatedAt
```

### Reference Tables (Seed Data)

- **`tbl_account_types`**: Loại tài khoản (Tiền mặt, Ngân hàng, Thẻ tín dụng, ...)
- **`tbl_transaction_types`**: Loại giao dịch (Chi tiêu, Thu tiền, Cho vay, Đi vay)
- **`tbl_currencies`**: Tiền tệ (VND, USD, EUR, ...)

### Entity Relationships

```
Users (1) ──< (N) Transactions
Users (1) ──< (N) MoneySources
Users (1) ──< (N) Categories
Users (1) ──< (N) Contacts

AccountType (1) ──< (N) MoneySources
TransactionType (1) ──< (N) Categories
TransactionType (1) ──< (N) Transactions

MoneySource (1) ──< (N) Transactions
Category (1) ──< (N) Transactions
Contact (1) ──< (N) Transactions [NULLABLE]
```

---

## 🔐 Authentication với Clerk

### Tổng quan

Ứng dụng sử dụng **Clerk** (https://clerk.com) để xử lý authentication. Clerk cung cấp:
- ✅ JWT-based authentication
- ✅ Social login (Google, Facebook, ...)
- ✅ Email/Password authentication
- ✅ Phone authentication
- ✅ User management dashboard
- ✅ Webhook events

### Luồng Authentication

```
┌──────────────┐
│ Mobile App   │  1. User logs in via Clerk UI
│   (Client)   │     (Email, Google, Facebook, etc.)
└──────┬───────┘
       │
       ▼
┌──────────────┐
│    Clerk     │  2. Clerk validates credentials
│   Service    │     and issues JWT token
└──────┬───────┘
       │ 3. Return JWT token
       ▼
┌──────────────┐
│ Mobile App   │  4. Store JWT token
└──────┬───────┘
       │ 5. API request with Bearer token
       │    Authorization: Bearer <jwt_token>
       ▼
┌─────────────────────────────────────┐
│      Finmate Backend API             │
│  (.NET 8 - FinmateController)        │
├─────────────────────────────────────┤
│  UseAuthentication() Middleware      │
│  6. Verify JWT signature with Clerk │
│  7. Extract claims (sub = userId)    │
│  8. Populate HttpContext.User        │
└──────┬──────────────────────────────┘
       │ 9. If valid, allow access to
       │    [Authorize] endpoints
       ▼
┌──────────────┐
│  Controller  │  10. Extract ClerkUserId from claims
│  [Authorize] │      and process request
└──────────────┘
```

### Cấu hình JWT trong Program.cs

```csharp
// JWT Authentication với Clerk
var clerkInstanceUrl = builder.Configuration["Clerk:InstanceUrl"];
var metadataAddress = $"{clerkInstanceUrl}/.well-known/openid-configuration";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Clerk's OpenID Connect discovery endpoint
        options.MetadataAddress = metadataAddress;
        
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,           // ✅ Verify token from Clerk
            ValidIssuer = clerkInstanceUrl,
            ValidateAudience = false,        // ❌ Clerk tokens don't use audience
            ValidateLifetime = true,         // ✅ Check token expiration
            ValidateIssuerSigningKey = true, // ✅ Verify signature with public key
            NameClaimType = "sub"            // Map "sub" claim to User.Identity.Name
        };
    });

builder.Services.AddAuthorization();

// Apply middleware
app.UseAuthentication();  // ← JWT verification happens here
app.UseAuthorization();
```

### User Synchronization

#### Cách 1: Webhook (Tự động - Khuyến nghị)

Clerk gửi webhook events khi có thay đổi user:

```csharp
// ClerkWebhookController.cs
[HttpPost("webhook")]
[AllowAnonymous]
public async Task<IActionResult> HandleWebhook()
{
    // 1. Verify webhook signature (Svix)
    // 2. Parse event type: user.created, user.updated, user.deleted, session.created
    // 3. Sync user to database
}
```

**Events được xử lý:**
- `user.created` → Tạo user mới trong database
- `user.updated` → Cập nhật thông tin user
- `user.deleted` → Soft delete (set IsActive = false)
- `session.created` → Tạo user nếu chưa có khi đăng nhập

#### Cách 2: Manual Sync (Khi cần)

```csharp
// AuthController.cs hoặc UserController.cs
[HttpPost("sync")]
[Authorize]
public async Task<IActionResult> SyncUser()
{
    var clerkUserId = User.FindFirst("sub")?.Value;
    var user = await _userService.GetOrCreateUserFromClerkAsync(clerkUserId);
    return Ok(user);
}
```

### Protected Endpoints

Sử dụng attribute `[Authorize]` để bảo vệ endpoints:

```csharp
[ApiController]
[Route("api/users")]
[Authorize]  // ← Yêu cầu JWT token hợp lệ
public class UserController : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        // Extract Clerk User ID from JWT claims
        var clerkUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        
        // Get user from database
        var user = await _userService.GetOrCreateUserFromClerkAsync(clerkUserId);
        return Ok(user);
    }
}
```

---

## 🛠 Cài đặt và Chạy dự án

### 1. Yêu cầu hệ thống

- **.NET 8 SDK**: https://dotnet.microsoft.com/download/dotnet/8.0
- **PostgreSQL** (hoặc Supabase account)
- **Clerk account**: https://clerk.com (Free tier available)
- **IDE**: Visual Studio 2022 / VS Code / Rider

### 2. Clone repository

```bash
git clone https://github.com/namnm309/Finmate-BE.git
cd Finmate-BE
```

### 3. Setup Clerk

#### Bước 1: Tạo Clerk Application

1. Đăng ký/Đăng nhập tại https://clerk.com
2. Tạo Application mới (chọn loại "Personal use" hoặc "Business")
3. Chọn authentication methods:
   - Email + Password
   - Google OAuth
   - Facebook OAuth (tùy chọn)
   - Phone number (tùy chọn)

#### Bước 2: Lấy API Keys

Vào **Dashboard → API Keys**, copy các keys sau:

```
Publishable Key: pk_test_xxxxxxxxxx
Secret Key: sk_test_xxxxxxxxxx
Instance URL: https://your-instance.clerk.accounts.dev
```

#### Bước 3: Cấu hình Webhook

1. Vào **Dashboard → Webhooks → Add Endpoint**
2. URL: `https://your-api-domain.com/api/clerk/webhook`
   - Nếu local testing: Dùng [ngrok](https://ngrok.com/) hoặc [localtunnel](https://localtunnel.github.io/www/)
   - Ví dụ: `https://abc123.ngrok.io/api/clerk/webhook`

3. Chọn events:
   - ✅ `user.created`
   - ✅ `user.updated`
   - ✅ `user.deleted`
   - ✅ `session.created`

4. Copy **Signing Secret** (whsec_xxxxxxxxxx)

#### Bước 4: Cấu hình CORS & Allowed Origins

Vào **Dashboard → API Keys → Advanced → Allowed Origins**, thêm:
```
http://localhost:5000
https://your-production-api.com
```

### 4. Cấu hình Database

#### Option A: Sử dụng Supabase (Khuyến nghị)

1. Tạo project tại https://supabase.com
2. Vào **Settings → Database**, copy connection string:
   ```
   postgresql://postgres.[project-ref]:[password]@aws-1-ap-south-1.pooler.supabase.com:5432/postgres
   ```

#### Option B: Local PostgreSQL

```bash
# Install PostgreSQL
# https://www.postgresql.org/download/

# Create database
psql -U postgres
CREATE DATABASE finmate;
```

### 5. Cập nhật appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "YOUR_POSTGRES_CONNECTION_STRING"
  },
  "Clerk": {
    "SecretKey": "sk_test_YOUR_SECRET_KEY",
    "PublishableKey": "pk_test_YOUR_PUBLISHABLE_KEY",
    "WebhookSecret": "whsec_YOUR_WEBHOOK_SECRET",
    "InstanceUrl": "https://your-instance.clerk.accounts.dev"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

⚠️ **Lưu ý:** Không commit file này với keys thật lên Git!

### 6. Chạy Migrations

```bash
# Di chuyển đến thư mục DAL
cd DAL

# Tạo migration (nếu cần)
dotnet ef migrations add InitialCreate --startup-project ../FinmateController

# Apply migration vào database
dotnet ef database update --startup-project ../FinmateController
```

Hoặc migrations sẽ tự động apply khi chạy ứng dụng (xem `Program.cs`):
```csharp
ApplyPendingMigrations(app);
```

### 7. Chạy ứng dụng

```bash
# Di chuyển đến thư mục FinmateController
cd FinmateController

# Chạy ứng dụng
dotnet run

# Hoặc với hot reload
dotnet watch run
```

API sẽ chạy tại:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`
- Swagger UI: `http://localhost:5000/swagger`

### 8. Test với Swagger

1. Mở browser: `http://localhost:5000/swagger`
2. Test endpoint `/api/auth/test` (không cần auth)
3. Lấy JWT token từ Clerk (qua mobile app hoặc Clerk Dashboard)
4. Click **Authorize** button, nhập: `Bearer YOUR_JWT_TOKEN`
5. Test các protected endpoints

---

## 📡 API Endpoints

### Authentication & User

| Endpoint | Method | Auth | Mô tả |
|----------|--------|------|-------|
| `/api/auth/verify` | POST | ✗ | Verify JWT token với Clerk |
| `/api/auth/me` | GET | ✓ | Lấy user hiện tại |
| `/api/auth/sync` | POST | ✓ | Sync user từ Clerk vào DB |
| `/api/auth/test` | GET | ✓ | Test authentication & claims |
| `/api/users/me` | GET | ✓ | Lấy thông tin user |
| `/api/users/me` | PUT | ✓ | Cập nhật profile |
| `/api/users/me/data` | DELETE | ✓ | Xóa tất cả dữ liệu |
| `/api/users/me` | DELETE | ✓ | Xóa tài khoản (soft) |

### Transactions

| Endpoint | Method | Auth | Mô tả |
|----------|--------|------|-------|
| `/api/transactions` | GET | ✓ | Lấy danh sách giao dịch |
| `/api/transactions/{id}` | GET | ✓ | Lấy chi tiết giao dịch |
| `/api/transactions` | POST | ✓ | Tạo giao dịch mới |
| `/api/transactions/{id}` | PUT | ✓ | Cập nhật giao dịch |
| `/api/transactions/{id}` | DELETE | ✓ | Xóa giao dịch |
| `/api/transactions/by-date-range` | GET | ✓ | Lọc theo khoảng thời gian |

### Money Sources (Accounts)

| Endpoint | Method | Auth | Mô tả |
|----------|--------|------|-------|
| `/api/money-sources` | GET | ✓ | Lấy danh sách tài khoản |
| `/api/money-sources/{id}` | GET | ✓ | Lấy chi tiết tài khoản |
| `/api/money-sources` | POST | ✓ | Tạo tài khoản mới |
| `/api/money-sources/{id}` | PUT | ✓ | Cập nhật tài khoản |
| `/api/money-sources/{id}` | DELETE | ✓ | Xóa tài khoản |

### Categories

| Endpoint | Method | Auth | Mô tả |
|----------|--------|------|-------|
| `/api/categories` | GET | ✓ | Lấy danh sách danh mục |
| `/api/categories/{id}` | GET | ✓ | Lấy chi tiết danh mục |
| `/api/categories` | POST | ✓ | Tạo danh mục mới |
| `/api/categories/{id}` | PUT | ✓ | Cập nhật danh mục |
| `/api/categories/{id}` | DELETE | ✓ | Xóa danh mục |

### Reports

| Endpoint | Method | Auth | Mô tả |
|----------|--------|------|-------|
| `/api/reports/summary` | GET | ✓ | Tổng quan thu chi |
| `/api/reports/by-category` | GET | ✓ | Báo cáo theo danh mục |
| `/api/reports/by-time-range` | GET | ✓ | Báo cáo theo khoảng thời gian |

### Webhook

| Endpoint | Method | Auth | Mô tả |
|----------|--------|------|-------|
| `/api/clerk/webhook` | POST | ✗* | Nhận events từ Clerk |

*\*Xác thực qua Svix signature, không cần JWT token*

---

## 🔔 Webhook Events

### Event Types

#### `user.created`
```json
{
  "type": "user.created",
  "data": {
    "id": "user_2abc123xyz",
    "first_name": "John",
    "last_name": "Doe",
    "email_addresses": [
      {
        "id": "idn_123",
        "email_address": "john@example.com",
        "verification": { "status": "verified" }
      }
    ],
    "phone_numbers": [],
    "created_at": 1703001234567,
    "updated_at": 1703001234567
  }
}
```
**Action:** Tạo user mới trong database

#### `user.updated`
```json
{
  "type": "user.updated",
  "data": {
    "id": "user_2abc123xyz",
    "first_name": "John Updated",
    // ... other fields
  }
}
```
**Action:** Cập nhật thông tin user trong database

#### `user.deleted`
```json
{
  "type": "user.deleted",
  "data": {
    "id": "user_2abc123xyz",
    "deleted": true
  }
}
```
**Action:** Soft delete user (set `IsActive = false`)

#### `session.created`
```json
{
  "type": "session.created",
  "data": {
    "user_id": "user_2abc123xyz",
    "user": {
      "id": "user_2abc123xyz",
      "email_addresses": [...]
      // ... full user object
    }
  }
}
```
**Action:** Tạo user nếu chưa có khi đăng nhập lần đầu

### Webhook Security

```csharp
// Verify Svix signature
var signature = Request.Headers["svix-signature"].FirstOrDefault();
if (!_clerkService.VerifyWebhookSignature(body, signature))
{
    return Unauthorized("Invalid signature");
}
```

---

## 🔒 Security Best Practices

### 1. Environment Variables

Không lưu secrets trong `appsettings.json`, dùng:

```bash
# Development
dotnet user-secrets init
dotnet user-secrets set "Clerk:SecretKey" "sk_test_xxx"

# Production (Azure, Docker, etc.)
# Set environment variables:
export Clerk__SecretKey="sk_test_xxx"
export ConnectionStrings__DefaultConnection="postgres://..."
```

### 2. CORS Configuration

```csharp
// Program.cs
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMobileApp", policy =>
    {
        policy.WithOrigins("https://yourmobileapp.com")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

app.UseCors("AllowMobileApp");
```

### 3. Rate Limiting

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));
});

app.UseRateLimiter();
```

---

## 🧪 Testing

### Unit Tests

```bash
# Di chuyển đến thư mục test (tạo nếu chưa có)
dotnet new xunit -n Finmate.Tests
cd Finmate.Tests

# Add references
dotnet add reference ../BLL/BLL.csproj
dotnet add reference ../DAL/DAL.csproj

# Chạy tests
dotnet test
```

### Integration Tests với Postman

Import Postman collection: (tạo file riêng nếu cần)

```bash
# Export từ Swagger
curl http://localhost:5000/swagger/v1/swagger.json > finmate-api.json
```

---

## 📝 Todo List

- [ ] Implement refresh token
- [ ] Add role-based authorization (Admin, Premium User)
- [ ] Implement caching (Redis)
- [ ] Add background jobs (Hangfire)
- [ ] Implement file upload (Avatar, receipts)
- [ ] Add logging service (Serilog, Application Insights)
- [ ] Write comprehensive unit tests
- [ ] Add CI/CD pipeline (GitHub Actions)
- [ ] Deploy to Azure/AWS

---

## 📞 Liên hệ & Đóng góp

- **Repository:** https://github.com/namnm309/Finmate-BE
- **Issues:** https://github.com/namnm309/Finmate-BE/issues
- **Author:** namnm309

Contributions are welcome! Please open an issue or submit a PR.

---

## 📄 License

This project is licensed under the MIT License.

---

## 🎯 Quick Start Checklist

- [ ] Clone repository
- [ ] Create Clerk application
- [ ] Setup Supabase/PostgreSQL database
- [ ] Update `appsettings.json` with keys
- [ ] Run migrations: `dotnet ef database update`
- [ ] Start API: `dotnet run`
- [ ] Open Swagger: `http://localhost:5000/swagger`
- [ ] Configure Clerk webhook endpoint
- [ ] Test authentication flow
- [ ] Integrate with mobile app

---

**Happy Coding! 🚀**
