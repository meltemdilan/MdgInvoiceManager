# 📄 MdgInvoiceManager - Fatura Yönetim Sistemi (Backend & API)

## 🏗️ Mimari

```text
┌─────────────────────────────────────────────────────────────┐
│                       Flutter Client                        │
│                 (Mobile App - Dart / REST)                  │
└──────────────────────────────┬──────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────┐
│                 MdgInvoiceManager (Web API)                 │
│              (Controllers, Middleware, JWT, DI)             │
└──────────────────────────────┬──────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────┐
│                 MdgInvoiceManager.Business                  │
│       (AuthManager, InvoiceManager, Business Rules)         │
└──────────────┬───────────────────────────────┬──────────────┘
               │                               │
       (Event Publish)                         ▼
               │                ┌─────────────────────────────┐
               ▼                │ MdgInvoiceManager.DataAccess│
        ┌─────────────┐         │  (EF Core, DbContext, Repo) │
        │  RabbitMQ   │         └──────────────┬──────────────┘
        └──────┬──────┘                        │
               │                               ▼
               ▼                        ┌─────────────┐
┌─────────────────────────────┐         │ MSSQL Server│
│   InvoiceCreatedConsumer    │         │  (Database) │
│ (Background Event Handling) │         └─────────────┘
└─────────────────────────────┘

```

---
✨ Özellikler
JWT Tabanlı Kimlik Doğrulama: Kullanıcı kayıt/giriş, rol bazlı yetkilendirme (User / Admin) ve token koruması.  


Fatura CRUD İşlemleri: Fatura oluşturma, listeleme (sayfalama destekli), tekil görüntüleme, güncelleme ve silme akışları.

Rol Bazlı Erişim Kontrolü: Kullanıcılar sadece kendi faturalarını görür; adminler tüm faturalara erişebilir. Güncelleme ve silme işlemleri sadece Admin rolüne açıktır.

Redis ile Önbellekleme: Fatura listeleri ve tekil fatura kayıtları cache'lenerek veritabanı yükü azaltılır ve sorgu performansı artırılır.  


RabbitMQ / MassTransit ile Asenkron İşleme: Fatura oluşturulduğunda InvoiceCreatedEvent kuyruğa yayınlanır ve arka planda InvoiceCreatedConsumer ile işlenir.  


Otomatik Vergi Hesaplama: Girilen fatura tutarı üzerinden %20 KDV ve toplam tutar iş mantığı katmanında otomatik hesaplanır.

Swagger / OpenAPI: Tüm endpoint'ler için interaktif API dokümantasyonu ve arayüz üzerinden JWT Bearer test desteği.  


Docker & Docker Compose: API, SQL Server ve Redis servislerini tek bir komutla ayağa kaldırma altyapısı.  


Otomatik Migration & Seed Data: Uygulama ayağa kalkarken veritabanı migration'ları ve varsayılan sistem rolleri (User, Admin) otomatik oluşturulur.



## 🏛️ Katmanlar ve Sorumluluklar

| Katman / Proje | Sorumluluk | Port / Tip |
| --- | --- | --- |
| **`MdgInvoiceManager`** | API Controllers, Auth & Token uç noktaları, Swagger, Middleware | `https://localhost:7001` <br>

<br> `http://localhost:5001` |
| **`MdgInvoiceManager.Business`** | İş mantığı, servis implementasyonları, MassTransit Consumer akışları | Class Library |
| **`MdgInvoiceManager.DataAccess`** | EF Core DbContext, Entity yapılandırmaları, Database Migrations | Class Library |
| **`MdgInvoiceManager.Core`** | Ortak Entities, DTOs, Custom Validation Attributes (`[City]`, `[VknTckn]`) | Class Library |
| **`Mobile Client`** | Fatura hareketleri, mobil giriş ve form yönetimi (Flutter) | Android / iOS |

---

## 🚀 Teknolojiler

### Backend

* **.NET 8** — N-Tier Architecture (Katmanlı Mimari)


* **Entity Framework Core** — ORM & Veri Erişim Katmanı


* **Microsoft SQL Server** — İlişkisel Veritabanı


* **MassTransit & RabbitMQ** — Event-Driven Asenkron Mesajlaşma


* **ASP.NET Core Identity & JWT Bearer** — Role-Based Kimlik Doğrulama & Refresh Token


* **Custom Data Annotations** — İl ve VKN/TCKN Doğrulama Nitelikleri


* **Humanizer** — Formatlama ve Lokalizasyon Desteği


* **Swagger / OpenAPI** — API Dokümantasyonu ve Test Arayüzü



### Mobil (Frontend)

* **Flutter & Dart** — Cross-Platform Mobil Arayüz
* **REST API & HTTP Client** — Backend Entegrasyonu & Interceptors
* **Secure Storage** — JWT ve Kullanıcı Oturum Yönetimi

---

## ⚡ Hızlı Başlangıç

### 1. Repoyu Klonlayın

```bash
git clone https://github.com/<meltemdilan>/MdgInvoiceManager.git
cd MdgInvoiceManager

```

### 2. Veritabanı ve RabbitMQ Ayarları (`appsettings.json`)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=MdgInvoiceDb;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "RabbitMQ": {
    "Host": "localhost",
    "VirtualHost": "/"
  }
}

```

### 3. Veritabanı Migration ve Başlatma

```bash
dotnet ef database update --project MdgInvoiceManager.DataAccess --startup-project MdgInvoiceManager
dotnet run --project MdgInvoiceManager

```

### Erişim Noktaları

| Servis / Arayüz | URL |
| --- | --- |
| **Swagger UI** | `https://localhost:7001/swagger` |
| **RabbitMQ Management** | `http://localhost:15672` *(Varsayılan: guest / guest)* |

### Test Kullanıcıları

| Rol       | E-posta| Şifre |
| ----------| ------ | ----- |
| **Admin** | `admin@mdginvoice.com` | `Admin123!` |
| **User**  | `user@mdginvoice.com` | `User123!` |

---

## 📁 Proje Yapısı

```text
MdgInvoiceManager/
├── MdgInvoiceManager/                   # Web API Katmanı
│   ├── Controllers/                     # AuthController, InvoiceController
│   ├── appsettings.json                 # Yapılandırma ve bağlantı ayarları
│   └── Program.cs                       # DI, Middleware, MassTransit kayıtları
│
├── MdgInvoiceManager.Business/          # İş Mantığı Katmanı
│   ├── Abstract/                        # IAuthService, IInvoiceService
│   ├── Concrete/                        # AuthManager, InvoiceManager
│   └── Consumers/                       # InvoiceCreatedConsumer
│
├── MdgInvoiceManager.DataAccess/        # Veri Erişim Katmanı
│   ├── Data/                            # AppDbContext
│   ├── Migrations/                      # EF Core veritabanı göç dosyaları
│   └── Repositories/                    # InvoiceRepository, IInvoiceRepository
│
└── MdgInvoiceManager.Core/              # Çekirdek & Ortak Katman
    ├── Attributes/                      # CityAttribute, VknTcknAttribute
    ├── Dtos/                            # AuthDtos, RefreshTokenDto, ResponseModel
    └── Entities/                        # Invoice, InvoiceCreatedEvent

```

---


```

---

## 🔐 Yetkilendirme

| İşlem | Admin | Kullanıcı |
| --- | --- | --- |
| Fatura Oluşturma | ✅ | ✅ |
| Kendi Faturalarını Listeleme | ✅ | ✅ |
| Fatura Güncelleme | ✅ | ❌ |
| Fatura İptal / Silme | ✅ | ❌ |
| Tüm Sistem Faturalarını Görme | ✅ | ❌ |
| Kullanıcı / Rol Yönetimi | ✅ | ❌ |

---

## 🗄️ Veritabanı ve Tablo Yapısı

| Tablo / Varlık | Katman | İçerik ve Sorumluluk |
| --- | --- | --- |
| **`Invoices`** | DataAccess / Entities | Fatura başlık bilgileri, tutar, KDV, VKN/TCKN, şehir, senaryo |
| **`AspNetUsers`** | Core / Identity | Kullanıcı giriş bilgileri, hash'lenmiş şifreler |
| **`AspNetRoles`** | Core / Identity | Rol tanımları (`Admin`, `User`) |
| **`AspNetUserRoles`** | Core / Identity | Kullanıcı-rol eşleştirmeleri |

---

---

## 👤 Geliştirici

**Meltem Dilan Gümüş** 
