using KasirCokro.Helpers;
using KasirCokro.Views.Auth;
using KasirCokro.Views.Kasir;
using Microsoft.Win32;
using MySql.Data.MySqlClient;
using Mysqlx.Notice;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace KasirCokro.Views.Kasir
{
    public partial class RiwayatTransaksi : Window, INotifyPropertyChanged
    {
        #region Fields
        private ObservableCollection<TransaksiModel> _allTransaksi;
        private ObservableCollection<TransaksiModel> _filteredTransaksi;
        private int _currentPage = 1;
        private int _itemsPerPage = 10;
        private int _totalPages = 1;
        private int _totalTransaksi;
        private decimal _totalPenjualan;
        private decimal _rataRataPenjualan;
        private bool _isLoading = false;
        #endregion

        #region Properties
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public int TotalTransaksi
        {
            get => _totalTransaksi;
            set { _totalTransaksi = value; OnPropertyChanged(); }
        }

        public decimal TotalPenjualan
        {
            get => _totalPenjualan;
            set { _totalPenjualan = value; OnPropertyChanged(); }
        }

        public decimal RataRataPenjualan
        {
            get => _rataRataPenjualan;
            set { _rataRataPenjualan = value; OnPropertyChanged(); }
        }

        public ObservableCollection<TransaksiModel> FilteredTransaksi
        {
            get => _filteredTransaksi;
            set { _filteredTransaksi = value; OnPropertyChanged(); }
        }

        public int CurrentPage
        {
            get => _currentPage;
            set { _currentPage = value; OnPropertyChanged(); }
        }

        public int TotalPages
        {
            get => _totalPages;
            set { _totalPages = value; OnPropertyChanged(); }
        }

        public string PageInfo => $"Halaman {CurrentPage} dari {TotalPages}";
        #endregion

        #region Constructor
        public RiwayatTransaksi()
        {
            InitializeComponent();
            _allTransaksi = new ObservableCollection<TransaksiModel>();
            FilteredTransaksi = new ObservableCollection<TransaksiModel>();
            DataContext = this;
            _ = LoadTransaksiAsync();
        }
        #endregion

        #region Data Loading
        private void UpdateSummary()
        {
            TotalTransaksi = _allTransaksi.Count;
            TotalPenjualan = _allTransaksi.Sum(t => t.TotalHarga);
            RataRataPenjualan = TotalTransaksi > 0 ? TotalPenjualan / TotalTransaksi : 0;
        }

        private void LoadPage()
        {
            int startIndex = (CurrentPage - 1) * _itemsPerPage;
            var pageData = _allTransaksi.Skip(startIndex).Take(_itemsPerPage).ToList();

            FilteredTransaksi.Clear();
            foreach (var item in pageData)
            {
                FilteredTransaksi.Add(item);
            }

            btnPrevious.IsEnabled = CurrentPage > 1;
            btnNext.IsEnabled = CurrentPage < TotalPages;
            OnPropertyChanged(nameof(PageInfo));
        }

        private async Task LoadTransaksiAsync()
        {
            try
            {
                IsLoading = true;
                _allTransaksi.Clear();
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Close();
                    await conn.OpenAsync();
                    string query = @"
                SELECT 
                    t.id,
                    t.kode_transaksi,
                    t.tanggal_transaksi,
                    t.total,
                    COUNT(td.id) as total_items
                FROM transactions t
                LEFT JOIN transaction_details td ON t.id = td.transaksi_id
                GROUP BY t.id, t.kode_transaksi, t.tanggal_transaksi, t.total
                ORDER BY t.tanggal_transaksi DESC";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var transaksi = new TransaksiModel
                                {
                                    TransaksiID = Convert.ToInt32(reader["id"]),
                                    KodeTransaksi = reader["kode_transaksi"].ToString(),
                                    TanggalTransaksi = Convert.ToDateTime(reader["tanggal_transaksi"]),
                                    TotalHarga = Convert.ToDecimal(reader["total"]),
                                    Jumlah = reader.IsDBNull(reader.GetOrdinal("total_items")) ? 0 : Convert.ToInt32(reader["total_items"])
                                };
                                _allTransaksi.Add(transaksi);
                            }
                        }
                    }
                }
                UpdateSummary();
                LoadPage();
            }
            catch (Exception ex)
            {
                MessageBox.Show($@"Error loading transaksi: {ex.Message}
Menggunakan data sample...", "Warning",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                await LoadSampleDataAsync();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadSampleDataAsync()
        {
            await Task.Run(() =>
            {
                _allTransaksi.Clear();
                var random = new Random();
                for (int i = 1; i <= 50; i++)
                {
                    var totalHarga = random.Next(10000, 500000);
                    var transaksi = new TransaksiModel
                    {
                        TransaksiID = i,
                        KodeTransaksi = $"TRX-{DateTime.Now:yyyyMM}-{i:D4}",
                        TanggalTransaksi = DateTime.Now.AddDays(-random.Next(0, 90)),
                        TotalHarga = totalHarga,
                        Jumlah = random.Next(1, 10)
                    };
                    _allTransaksi.Add(transaksi);
                }
            });

            UpdateSummary();
            LoadPage();
        }
        #endregion

        #region Pagination Controls
        private void BtnPrevious_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                LoadPage();
            }
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            int totalPages = (int)Math.Ceiling((double)_allTransaksi.Count / _itemsPerPage);
            if (_currentPage < totalPages)
            {
                _currentPage++;
                LoadPage();
            }
        }
        #endregion

        #region Export Functions
        private void btnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel Files|*.xlsx",
                    DefaultExt = "xlsx",
                    FileName = $"RiwayatTransaksi_{DateTime.Now:yyyyMMdd_HHmmss}"
                };
                if (saveFileDialog.ShowDialog() == true)
                {
                    ExportToExcel(saveFileDialog.FileName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting to Excel: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportToExcel(string fileName)
        {
            OfficeOpenXml.ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Riwayat Transaksi");

                worksheet.Cells[1, 1].Value = "No";
                worksheet.Cells[1, 2].Value = "Kode Transaksi";
                worksheet.Cells[1, 3].Value = "Tanggal";
                worksheet.Cells[1, 4].Value = "Total Items";
                worksheet.Cells[1, 5].Value = "Total Harga";

                for (int i = 0; i < _allTransaksi.Count; i++)
                {
                    var transaksi = _allTransaksi[i];
                    worksheet.Cells[i + 2, 1].Value = i + 1;
                    worksheet.Cells[i + 2, 2].Value = transaksi.KodeTransaksi;
                    worksheet.Cells[i + 2, 3].Value = transaksi.TanggalTransaksi.ToString("dd/MM/yyyy HH:mm");
                    worksheet.Cells[i + 2, 4].Value = transaksi.Jumlah;
                    worksheet.Cells[i + 2, 5].Value = transaksi.TotalHarga;
                }

                var totalHargaRange = worksheet.Cells[2, 5, _allTransaksi.Count + 1, 5];
                totalHargaRange.Style.Numberformat.Format = "Rp #,##0";

                worksheet.Cells.AutoFitColumns();

                var headerRange = worksheet.Cells[1, 1, 1, 5];
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);

                package.SaveAs(new FileInfo(fileName));
                MessageBox.Show($"Data berhasil diekspor ke: {fileName}", "Success",
                               MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        #endregion

        #region Navigation Buttons
        private void BtnDashboard_Click(object sender, RoutedEventArgs e)
        {
            var dashboard = new DashboardKasir();
            dashboard.Show();
            this.Close();
        }

        private void BtnTransaksi_Click(object sender, RoutedEventArgs e)
        {
            var transaksi = new TransaksiPenjualan();
            transaksi.Show();
            this.Close();
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow1 LoginWindow1 = new LoginWindow1();
            LoginWindow1.Show();
            this.Close();
        }
        #endregion

        #region INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region TransaksiModel Class
        public class TransaksiModel : INotifyPropertyChanged
        {
            private int _transaksiID;
            private string _kodeTransaksi;
            private DateTime _tanggalTransaksi;
            private decimal _totalHarga;
            private int _jumlah;

            public int TransaksiID
            {
                get => _transaksiID;
                set { _transaksiID = value; OnPropertyChanged(); }
            }

            public string KodeTransaksi
            {
                get => _kodeTransaksi;
                set { _kodeTransaksi = value; OnPropertyChanged(); }
            }

            public DateTime TanggalTransaksi
            {
                get => _tanggalTransaksi;
                set { _tanggalTransaksi = value; OnPropertyChanged(); }
            }

            public decimal TotalHarga
            {
                get => _totalHarga;
                set { _totalHarga = value; OnPropertyChanged(); }
            }

            public int Jumlah
            {
                get => _jumlah;
                set { _jumlah = value; OnPropertyChanged(); }
            }

            public string TanggalFormatted => TanggalTransaksi.ToString("dd/MM/yyyy HH:mm");
            public string TotalHargaFormatted => TotalHarga.ToString("C", new CultureInfo("id-ID"));

            public event PropertyChangedEventHandler PropertyChanged;
            protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion
    }
}