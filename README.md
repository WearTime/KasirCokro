# 🥁 Nijika's KasirCokro Application

<div align="center">
 <img src="https://media1.tenor.com/m/hfSkLmaB-zQAAAAd/nijika-ijichi.gif" alt="Ijichi Nijika" width="200"/>
 
 *"Mari kita atur semua transaksi dengan rapi seperti beat drum yang sempurna!"*
 
 [![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.8-blue.svg)](https://dotnet.microsoft.com/)
 [![WPF](https://img.shields.io/badge/WPF-Windows-orange.svg)](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/)
 [![MySQL](https://img.shields.io/badge/MySQL-8.0-blue.svg)](https://www.mysql.com/)
 [![License](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
</div>

## 🎵 Tentang Aplikasi

Aplikasi kasir ini dibuat dengan semangat **Ijichi Nijika** dari Bocchi the Rock! Seperti cara Nijika mengorganisir band Kessoku Band, aplikasi ini membantu mengorganisir transaksi penjualan dengan rapi dan efisien.

> *"Seperti drumbeat yang konsisten, sistem kasir ini akan membantu bisnis Anda berjalan dengan tempo yang tepat!"* - Nijika

## 🎸 Fitur Utama - "Kessoku Band Management Style"

### 💰 **Rhythm Section - Manajemen Transaksi**
- **New Transaction Beat**: Membuat transaksi baru dengan antarmuka yang user-friendly
- **Item Harmony**: Pencatatan detail item dan harga dengan akurasi tinggi
- **Auto Calculate Solo**: Kalkulasi otomatis total pembayaran
- **Transaction History Archive**: Riwayat transaksi yang lengkap seperti rekaman album

### 📦 **Merchandise Management - Manajemen Produk**
- **Product Stage Setup**: Tambah, edit, dan hapus produk dengan mudah
- **Quick Search Performance**: Pencarian produk yang cepat dan responsif
- **Category Organization**: Kategori produk yang terstruktur rapi
- **Stock Monitor**: Monitoring stok seperti Nijika yang selalu aware kondisi band

### 🏪 **Supplier Network - Manajemen Supplier**
- **Supplier Database**: Database supplier yang komprehensif
- **Contact Information Hub**: Informasi kontak dan detail supplier
- **Purchase History Track**: Riwayat pembelian dari setiap supplier

### 🎛️ **Admin Control Panel - Dashboard Admin**
- **Daily Performance Stats**: Overview penjualan harian
- **Best Seller Charts**: Statistik produk terlaris
- **Financial Reports**: Laporan keuangan yang detail
- **Sales Performance Graph**: Grafik performa penjualan

### 💸 **Payment & Debt Management**
- **Payment Processing**: Sistem pembayaran yang smooth
- **Debt Tracking**: Pelacakan piutang dan hutang
- **Transaction Flow**: Alur transaksi masuk dan keluar

### 🎯 **Role-Based Access**
- **Admin Panel**: Full access untuk administrator
- **Kasir Dashboard**: Interface khusus untuk kasir
- **Secure Authentication**: Sistem login yang aman
  
## 🎤 Tech Stack - "Kessoku Band Equipment"

### 🎸 **Frontend - Visual Performance**
- **WPF (Windows Presentation Foundation)** - Main UI framework
- **XAML** - Markup language untuk design interface
- **Material Design Themes** - Modern UI components
- **MahApps.Metro** - Additional WPF enhancements
- **LiveCharts** - Data visualization untuk grafik

### 🥁 **Backend & Database - Rhythm Section**
- **C# .NET Framework 4.8** - Core programming language
- **MySQL 8.0** - Database management system
- **Entity Framework** - ORM untuk database operations
- **MySql.Data** - MySQL connector

### 🎵 **Libraries & Packages - Supporting Band**
- **BCrypt.Net-Next** - Password hashing security
- **ClosedXML** - Excel file processing
- **System.Drawing.Common** - Image processing
- **MaterialDesignThemes** - UI styling
- **Google.Protobuf** - Data serialization

## 🎼 Project Structure - "Band Organization"

```
📁 KasirCokro/
├── 📁 Helpers/                    # 🔧 Support Crew
│   ├── 🎯 DatabaseHelper.cs         # Database connection manager
│   ├── 🔐 PasswordHelper.cs         # Security & authentication
│   ├── 🖨️ RawPrinterHelper.cs       # Thermal printer support
│   └── ✅ ValidationHelper.cs        # Input validation
│
├── 📁 Models/                     # 🎵 Data Models
│   ├── 📦 Products.cs               # Product data structure
│   └── 🏪 Suppliers.cs              # Supplier data structure
│
├── 📁 Resources/                  # 🎨 Visual Assets
│   ├── 🔒 lock_icon.png            # Security icon
│   └── 👤 user_icon.png            # User profile icon
│
├── 📁 Views/                      # 🎭 User Interface
│   ├── 📁 Admin/                    # 👑 Admin Control Panel
│   │   ├── 💳 AddPaymentHutang.xaml    # Payment debt management
│   │   ├── 📄 BarangKeluarPage.xaml    # Outgoing items
│   │   ├── 📝 BarangMasukPage.xaml     # Incoming items
│   │   ├── 🏠 DashboardAdmin.xaml      # Admin main dashboard
│   │   ├── 🔍 DetailTransaksiWindow.xaml # Transaction details
│   │   ├── 💰 KasPage.xaml             # Cash management
│   │   ├── 💸 PihutangPage.xaml        # Debt management
│   │   ├── 📦 ProductForm.xaml         # Product form
│   │   ├── 📋 ProductsPage.xaml        # Products management
│   │   ├── 🏪 SupplierForm.xaml        # Supplier form
│   │   ├── 📊 SuppliersPage.xaml       # Suppliers management
│   │   ├── 💼 TransactionKeluarPage.xaml # Outgoing transactions
│   │   └── 📈 TransactionMasukPage.xaml  # Incoming transactions
│   │
│   ├── 📁 Auth/                     # 🔐 Authentication
│   │   └── 🚪 LoginWindow.xaml         # Login interface
│   │
│   └── 📁 Kasir/                    # 💰 Cashier Interface
│       ├── 🏠 DashboardKasir.xaml      # Cashier dashboard
│       ├── 🧾 RiwayatTransaksi.xaml    # Transaction history
│       └── 🛒 TransaksiDetail.xaml     # Transaction details
│
├── ⚙️ App.config                   # Application configuration
├── 📱 App.xaml                     # Application resources
└── 📦 packages.config              # NuGet packages
```

## 🎵 Installation & Setup - "Band Preparation"

### 📋 Prerequisites - "Equipment Check"
Pastikan Anda memiliki semua equipment berikut:
- **Windows 10/11** - Operating system
- **.NET Framework 4.8** atau lebih baru
- **MySQL Server 8.0** atau lebih baru
- **Visual Studio 2019/2022** dengan workload .NET desktop development
- **MySQL Workbench** (recommended untuk database management)

### 🎸 Setup Steps - "Sound Check"

#### 1. **Repository Setup**
```bash
# Clone the repository
git clone https://github.com/WearTime/KasirCokro.git
cd KasirCokro
```

#### 2. **Database Setup - "Tuning the Bass"**
```sql
-- Buat database baru
CREATE DATABASE cokro;

-- Gunakan database
USE cokro;

-- Import schema database (jika ada file SQL)
-- mysql -u root -p cokro < database/cokro.sql
```

#### 3. **Connection String Configuration**
Edit file `DatabaseHelper.cs` di folder `Helpers/`:
```csharp
private static string connectionString = 
    "server=localhost;user=root;database=cokro;port=3306;password=your_password";
```

Atau edit `App.config` jika menggunakan configuration file:
```xml
<configuration>
  <connectionStrings>
    <add name="DefaultConnection" 
         connectionString="server=localhost;user=root;database=cokro;port=3306;password=your_password" 
         providerName="MySql.Data.MySqlClient" />
  </connectionStrings>
</configuration>
```

#### 4. **Build & Run - "Let's Rock!"**
```bash
# Restore NuGet packages
nuget restore

# Build solution
dotnet build

# Run aplikasi
dotnet run
```

## 🎶 Usage Guide - "Playing the Perfect Set"

### 🥁 **Admin Dashboard - "Band Leader Mode"**
1. **Dashboard Overview**: Lihat statistik penjualan harian dan performa toko
2. **Product Management**: Kelola produk, kategori, dan stok
3. **Supplier Management**: Maintain database supplier dan riwayat pembelian
4. **Transaction Monitoring**: Monitor semua transaksi masuk dan keluar
5. **Financial Reports**: Generate laporan keuangan dan analisis

### 💰 **Kasir Interface - "Performance Mode"**
1. **New Transaction**: Buat transaksi baru dengan scan barcode atau manual input
2. **Payment Processing**: Proses pembayaran dengan berbagai metode
3. **Receipt Printing**: Print struk untuk customer
4. **Transaction History**: Lihat riwayat transaksi yang telah dilakukan

### 🎵 **Key Features Usage**

#### **Product Management**
- Add new products dengan informasi lengkap
- Set pricing dan manage stock levels
- Categorize products untuk easy navigation
- Track product performance

#### **Transaction Flow**
- Scan atau input product codes
- Calculate total with tax and discounts
- Process payments (cash, card, transfer)
- Generate receipts and update inventory

#### **Reporting System**
- Daily sales reports
- Monthly performance analysis
- Inventory status reports
- Financial summaries

## 🎸 Development Guidelines - "Band Practice Rules"

### 🎵 **Coding Standards - "Musical Harmony"**
- **Naming Convention**: 
  - Classes: `PascalCase` (e.g., `ProductManager`)
  - Methods: `PascalCase` (e.g., `GetProductById`)
  - Variables: `camelCase` (e.g., `productId`)
  - Constants: `UPPER_CASE` (e.g., `MAX_QUANTITY`)

- **MVVM Pattern**: Follow Model-View-ViewModel pattern untuk WPF
- **Error Handling**: Implement proper try-catch blocks
- **Documentation**: Comment complex logic dan business rules

### 🥁 **Database Guidelines - "Rhythm Section"**
- Use parameterized queries untuk prevent SQL injection
- Implement proper transaction management
- Follow database naming conventions
- Create indexes untuk frequently queried columns

### 🎤 **UI/UX Best Practices - "Stage Performance"**
- Follow Material Design principles
- Implement responsive design
- Use appropriate animations dan transitions
- Ensure accessibility compliance

## 🔧 Troubleshooting - "When the Beat Goes Wrong"

### 🥁 **Database Connection Issues**
```csharp
// Check these common issues:
// 1. MySQL service is running
// 2. Correct connection string in DatabaseHelper.cs
// 3. Database 'cokro' exists
// 4. User has proper permissions

// Test connection:
if (!DatabaseHelper.TestConnection())
{
    Console.WriteLine("Database connection failed!");
    // Check MySQL service status
    // Verify credentials
}
```

### 🖨️ **Printer Issues - "Sound System Problems"**
```csharp
// Common printer problems:
// 1. Printer driver not installed
// 2. USB/Network connection issues
// 3. Paper jam or empty paper
// 4. Wrong printer selected

// Test printer:
RawPrinterHelper.TestPrint("Test Print");
```

### 💻 **Application Errors**
- **Startup Issues**: Check .NET Framework version
- **UI Rendering**: Verify Material Design themes installation
- **Performance**: Monitor memory usage dan optimize queries
- **Security**: Ensure BCrypt is working properly

### 🎵 **Common Solutions**

#### **"The App Won't Start!"**
1. Check .NET Framework 4.8 installation
2. Verify all NuGet packages are restored
3. Check MySQL service status
4. Review App.config configuration

#### **"Database Connection Failed!"**
1. Test MySQL connection manually
2. Check firewall settings
3. Verify database credentials
4. Ensure database exists

#### **"Printer Not Working!"**
1. Check printer connection
2. Verify printer driver
3. Test with different printer
4. Check RawPrinterHelper settings

## 🎤 Support & Community - "Band Support"

### 📞 **Getting Help**
- **GitHub Issues**: Report bugs dan request features
- **Email Support**: Not available yet

### 🥁 **Bug Reports**
When reporting bugs, please include:
- OS version dan .NET Framework version
- MySQL version
- Steps to reproduce
- Expected vs actual behavior
- Screenshots (if applicable)

### 🎵 **Feature Requests**
- Describe the feature in detail
- Explain use case dan benefits
- Provide mockups (if UI related)
- Consider implementation complexity

## 📄 License - "Copyright & Credits"

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

### 🎼 **Third-Party Licenses**
- Material Design In XAML Toolkit - MIT License
- MySql.Data - GPL v2 License
- BCrypt.Net-Next - MIT License
- LiveCharts - MIT License

### 🏆 **Acknowledgments**
- **[@TokittoChizuru](https://github.com/TokittoChizuru)** - For your valuable contribution to this project.
- **[@Arestalia](https://github.com/Arestalia)** - For your collaboration and efforts throughout this project.
- **Material Design Team** - For beautiful UI components
- **MySQL Team** - For reliable database system
- **Open Source Community** - For amazing libraries dan tools

---

<div align="center">
  <img src="https://i.imgur.com/nijika-smile.gif" alt="Nijika Smile" width="200"/>
  
  <p><em>"Seperti drum beat yang sempurna, aplikasi ini akan menjaga ritme bisnis Anda tetap harmonis!"</em></p>
  
  <p>🥁 Made with ❤️ by WearTime Development Team</p>
  <p>🎵 Inspired by Ijichi Nijika from Bocchi the Rock!</p>
  
  <p>
    <a href="https://github.com/WearTime/KasirCokro">⭐ Star this project</a> |
    <a href="https://github.com/WearTime/KasirCokro/issues">🐛 Report Bug</a> |
    <a href="https://github.com/WearTime/KasirCokro/pulls">🔧 Request Feature</a>
  </p>
</div>

---
<div align="center">
  
 ![Typing SVG](https://readme-typing-svg.herokuapp.com/?font=Fira+Code&weight=300&duration=3400&color=DCF729&width=435&lines=%E2%9C%A8+Remember%2C+every+great+performance;starts+with+a+solid+rhythm.+;Let+Nijika%27s+spirit+guide+your+;business+to+success!+%F0%9F%A5%81%E2%9C%A8;)

</div>
