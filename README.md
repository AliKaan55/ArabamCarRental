# 🚗 ArabamRental

<div align="center">

![ASP.NET MVC](https://img.shields.io/badge/ASP.NET_MVC-5-blue?style=for-the-badge&logo=dotnet)
![SQLite](https://img.shields.io/badge/SQLite-3-green?style=for-the-badge&logo=sqlite)
![Bootstrap](https://img.shields.io/badge/Bootstrap-5-purple?style=for-the-badge&logo=bootstrap)
![.NET Framework](https://img.shields.io/badge/.NET_Framework-4.8-orange?style=for-the-badge&logo=dotnet)

**Modern ve şık tasarımlı ASP.NET MVC 5 araç kiralama uygulaması.**

</div>

---

## ✨ Özellikler

- 🔐 **Kullanıcı Sistemi** — Kayıt, giriş, şifre sıfırlama (token tabanlı), beni hatırla
- 🚘 **Araç Yönetimi** — Filtreleme (vites, yakıt, marka), detay sayfası, görsel yükleme
- 📅 **Kiralama Akışı** — Tarih seçimi, otomatik fiyat hesaplama, iptal desteği
- ⭐ **Yorum Sistemi** — Puanlama, yorum yazma, admin onayı
- 🛡️ **Admin Paneli** — Araç/müşteri/kiralama/yorum yönetimi
- 🔒 **Güvenlik** — SHA-256 şifre hashleme, CSRF koruması, parametreli sorgular

---

## 🛠️ Teknolojiler

| Katman | Teknoloji |
|--------|-----------|
| Backend | ASP.NET MVC 5, C#, .NET Framework 4.8 |
| Veritabanı | SQLite (System.Data.SQLite) |
| ORM | Entity Framework 6 |
| Frontend | Bootstrap 5, jQuery 3.7 |
| Güvenlik | SHA-256, AntiForgeryToken |

---

## 🚀 Kurulum

### Gereksinimler
- Visual Studio 2019 veya üzeri
- .NET Framework 4.8
- İnternet bağlantısı (NuGet restore için)

### Adımlar

```bash
# 1. Repoyu klonla
git clone https://github.com/KULLANICI_ADINIZ/ArabamCarRental.git
```

1. Visual Studio'da `ArabamCarRental.sln` dosyasını aç
2. Solution Explorer'da projeye sağ tıkla → **Restore NuGet Packages**
3. `Web.config` içinde admin şifresini güncelle (aşağıya bak)
4. **F5** ile çalıştır

> **Not:** İlk çalıştırmada `App_Data/ArabamCarRental.db` otomatik oluşturulur.

---

## ⚙️ Yapılandırma

`Web.config` dosyasında admin giriş bilgilerini değiştirin:

```xml
<add key="AdminUsername" value="admin" />
<add key="AdminPassword" value="GÜÇLÜ_BİR_ŞİFRE_GİRİN" />
```

> ⚠️ **Deploy öncesi mutlaka güncelleyin.**

Admin paneline erişim: `/Admin/Login`

---

## 📁 Proje Yapısı

```
ArabamCarRental/
├── Controllers/
│   ├── AccountController.cs   # Giriş, kayıt, şifre sıfırlama
│   ├── AdminController.cs     # Admin paneli
│   ├── CarController.cs       # Araç listeleme ve kiralama
│   └── HomeController.cs      # Ana sayfa
├── Models/
│   ├── Car.cs
│   ├── Customer.cs
│   ├── CarReview.cs
│   └── TransactionViewModel.cs
├── Services/
│   ├── AccountService.cs      # Kullanıcı işlemleri
│   └── CarService.cs          # Araç ve kiralama işlemleri
├── Views/                     # Razor view'ları
├── Content/                   # CSS, görseller
├── App_Data/                  # SQLite veritabanı (gitignore'da)
└── Web.config                 # Yapılandırma
```

---

## 🔒 Güvenlik

- Kullanıcı şifreleri **SHA-256** ile hashlenerek saklanır
- Tüm formlar **AntiForgeryToken** ile korunur
- SQL injection'a karşı **parametreli sorgular** kullanılır
- `App_Data/*.db` dosyaları `.gitignore` ile repoya dahil edilmez
- `debug="false"` ile hata detayları gizlenir

---

## 📄 Lisans

Bu proje MIT lisansı altında dağıtılmaktadır.

---

<div align="center">
  <sub>ArabamRental — 2026</sub>
</div>
