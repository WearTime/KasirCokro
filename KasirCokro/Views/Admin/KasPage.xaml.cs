using ClosedXML.Excel;
using KasirCokro.Views.Auth;
using Microsoft.Win32;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace KasirCokro.Views.Admin
{
    public partial class KasPage : Window
    {
        private ObservableCollection<KasData> kasDataCollection;

        public KasPage()
        {
            InitializeComponent();
            InitializePage();
        }

        private void InitializePage()
        {
            dpTanggalFrom.SelectedDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            dpTanggalTo.SelectedDate = DateTime.Now;

            kasDataCollection = new ObservableCollection<KasData>();
            dgKas.ItemsSource = kasDataCollection;

            LoadKasData();
        }

        private async void LoadKasData()
        {
            try
            {
                ShowLoading(true);

                await Task.Run(() =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        kasDataCollection.Clear();

                        DateTime fromDate = dpTanggalFrom.SelectedDate ?? DateTime.Now.AddMonths(-1);
                        DateTime toDate = dpTanggalTo.SelectedDate ?? DateTime.Now;

                        var kasData = GetKasDataFromDatabase(fromDate, toDate);

                        foreach (var data in kasData)
                        {
                            kasDataCollection.Add(data);
                        }

                        UpdateSummary();
                        UpdatePeriodeText(fromDate, toDate);
                    });
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ShowLoading(false);
            }
        }

        private List<KasData> GetKasDataFromDatabase(DateTime fromDate, DateTime toDate)
        {
            var result = new List<KasData>();

            try
            {
                using (MySqlConnection conn = Helpers.DatabaseHelper.GetConnection())
                {

                    
                    string query = @"
                        SELECT 
                            DATE(t.tanggal_transaksi) as tanggal,
                            COALESCE(SUM(CASE WHEN t.payment = 'tunai' THEN t.subtotal ELSE 0 END), 0) as pendapatan,
                            COALESCE(SUM(CASE WHEN t.payment = 'kredit' THEN t.subtotal ELSE 0 END), 0) as hutang,
                            COALESCE(SUM(t.subtotal), 0) as total
                        FROM transactions t
                        WHERE DATE(t.tanggal_transaksi) BETWEEN @fromDate AND @toDate
                        GROUP BY DATE(t.tanggal_transaksi)
                        ORDER BY DATE(t.tanggal_transaksi) ASC";

                    using (var command = new MySqlCommand(query, conn))
                    {
                        command.Parameters.AddWithValue("@fromDate", fromDate.Date);
                        command.Parameters.AddWithValue("@toDate", toDate.Date);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                result.Add(new KasData
                                {
                                    Tanggal = reader.GetDateTime("tanggal"),
                                    Pendapatan = reader.GetDecimal("pendapatan"),
                                    Hutang = reader.GetDecimal("hutang"),
                                    Total = reader.GetDecimal("total"),
                                    Keterangan = "Transaksi Harian"
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Database error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return result;
        }

        private void UpdateSummary()
        {
            try
            {
                decimal totalKas = kasDataCollection.Sum(x => x.Total);
                int totalTransaksi = kasDataCollection.Count;

                txtTotalKas.Text = $"Rp {totalKas:N0}";
                txtTotalTransaksi.Text = totalTransaksi.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating summary: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdatePeriodeText(DateTime fromDate, DateTime toDate)
        {
            txtPeriode.Text = $"Periode: {fromDate:dd/MM/yyyy} - {toDate:dd/MM/yyyy}";
        }

        private void ShowLoading(bool show)
        {
            loadingOverlay.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ShowSuccessMessage(string message)
        {
            txtSuccessMessage.Text = message;
            successMessage.Visibility = Visibility.Visible;

            
            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(3);
            timer.Tick += (s, e) =>
            {
                successMessage.Visibility = Visibility.Collapsed;
                timer.Stop();
            };
            timer.Start();
        }

        
        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dpTanggalFrom.SelectedDate.HasValue && dpTanggalTo.SelectedDate.HasValue)
            {
                if (dpTanggalFrom.SelectedDate > dpTanggalTo.SelectedDate)
                {
                    MessageBox.Show("Tanggal 'Dari' tidak boleh lebih besar dari tanggal 'Sampai'",
                                  "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                LoadKasData();
            }
        }

        private void BtnFilter_Click(object sender, RoutedEventArgs e)
        {
            if (!dpTanggalFrom.SelectedDate.HasValue || !dpTanggalTo.SelectedDate.HasValue)
            {
                MessageBox.Show("Pilih tanggal dari dan sampai terlebih dahulu",
                              "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            LoadKasData();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadKasData();
        }

        private async void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (kasDataCollection.Count == 0)
                {
                    MessageBox.Show("Tidak ada data untuk di-export", "Peringatan",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ShowLoading(true);

                await Task.Run(() =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ExportToExcel();
                    });
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saat export: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ShowLoading(false);
            }
        }

        private void ExportToExcel()
        {
            try
            {
                
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel files (*.xlsx)|*.xlsx",
                    FileName = $"Data_Kas_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Data Kas");

                        
                        worksheet.Cell(1, 1).Value = "LAPORAN KAS";
                        worksheet.Cell(1, 1).Style.Font.Bold = true;
                        worksheet.Cell(1, 1).Style.Font.FontSize = 16;
                        worksheet.Range(1, 1, 1, 5).Merge();

                        
                        DateTime fromDate = dpTanggalFrom.SelectedDate ?? DateTime.Now.AddMonths(-1);
                        DateTime toDate = dpTanggalTo.SelectedDate ?? DateTime.Now;
                        worksheet.Cell(2, 1).Value = $"Periode: {fromDate:dd/MM/yyyy} - {toDate:dd/MM/yyyy}";
                        worksheet.Range(2, 1, 2, 5).Merge();

                        
                        worksheet.Cell(4, 1).Value = "No";
                        worksheet.Cell(4, 2).Value = "Tanggal";
                        worksheet.Cell(4, 3).Value = "Pendapatan";
                        worksheet.Cell(4, 4).Value = "Hutang";
                        worksheet.Cell(4, 5).Value = "Total";
                        worksheet.Cell(4, 6).Value = "Keterangan";

                        
                        var headerRange = worksheet.Range(4, 1, 4, 6);
                        headerRange.Style.Font.Bold = true;
                        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                        
                        int row = 5;
                        int no = 1;
                        decimal totalPendapatan = 0;
                        decimal totalHutang = 0;
                        decimal grandTotal = 0;

                        foreach (var data in kasDataCollection)
                        {
                            worksheet.Cell(row, 1).Value = no++;
                            worksheet.Cell(row, 2).Value = data.Tanggal.ToString("dd/MM/yyyy");
                            worksheet.Cell(row, 3).Value = data.Pendapatan;
                            worksheet.Cell(row, 4).Value = data.Hutang;
                            worksheet.Cell(row, 5).Value = data.Total;
                            worksheet.Cell(row, 6).Value = data.Keterangan;

                            
                            worksheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0";
                            worksheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0";
                            worksheet.Cell(row, 5).Style.NumberFormat.Format = "#,##0";

                            totalPendapatan += data.Pendapatan;
                            totalHutang += data.Hutang;
                            grandTotal += data.Total;

                            row++;
                        }

                        
                        worksheet.Cell(row, 1).Value = "TOTAL";
                        worksheet.Cell(row, 2).Value = "";
                        worksheet.Cell(row, 3).Value = totalPendapatan;
                        worksheet.Cell(row, 4).Value = totalHutang;
                        worksheet.Cell(row, 5).Value = grandTotal;
                        worksheet.Cell(row, 6).Value = "";

                        
                        var totalRange = worksheet.Range(row, 1, row, 6);
                        totalRange.Style.Font.Bold = true;
                        totalRange.Style.Fill.BackgroundColor = XLColor.Yellow;
                        totalRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                        
                        worksheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0";
                        worksheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0";
                        worksheet.Cell(row, 5).Style.NumberFormat.Format = "#,##0";

                        worksheet.Columns().AdjustToContents();

                        var dataRange = worksheet.Range(4, 1, row, 6);
                        dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                        workbook.SaveAs(saveFileDialog.FileName);
                    }

                    ShowSuccessMessage($"Data berhasil di-export ke:\n{saveFileDialog.FileName}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saat export Excel: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        
        private void BtnDashboard_Click(object sender, RoutedEventArgs e)
        {
            var dashboard = new DashboardAdmin();
            dashboard.Show();
            this.Close();
        }

        private void Product_Click(object sender, RoutedEventArgs e)
        {
            var productPage = new ProductsPage();
            productPage.Show();
            this.Close();
        }

        private void Supplier_Click(object sender, RoutedEventArgs e)
        {
            var supplierPage = new SuppliersPage();
            supplierPage.Show();
            this.Close();
        }

        private void BrngMasuk_Click(object sender, RoutedEventArgs e)
        {
            var barangMasukPage = new BarangMasukPage();
            barangMasukPage.Show();
            this.Close();
        }

        private void BrngKeluar_Click(object sender, RoutedEventArgs e)
        {
            var barangKeluarPage = new BarangKeluarPage();
            barangKeluarPage.Show();
            this.Close();
        }

        private void Kas_Click(object sender, RoutedEventArgs e)
        {
            LoadKasData();
        }

        private void TransactionMasuk_Click(object sender, RoutedEventArgs e)
        {
            TransactionMasukPage transactionMasuk = new TransactionMasukPage();
            transactionMasuk.Show();
            this.Close();
        }

        private void TransactionKeluar_Click(object sender, RoutedEventArgs e)
        {
            TransactionKeluarPage transactionKeluar = new TransactionKeluarPage();
            transactionKeluar.Show();
            this.Close();
        }

        private void Pihutang_Click(object sender, RoutedEventArgs e)
        {
            PihutangPage pihutangPage = new PihutangPage();
            pihutangPage.Show();
            this.Close();
        }
        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Apakah Anda yakin ingin logout?", "Konfirmasi",
                                       MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                
                var loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
            }
        }
    }

    
    public class KasData : INotifyPropertyChanged
    {
        private DateTime _tanggal;
        private decimal _pendapatan;
        private decimal _hutang;
        private decimal _total;
        private string _keterangan;

        public DateTime Tanggal
        {
            get => _tanggal;
            set
            {
                _tanggal = value;
                OnPropertyChanged(nameof(Tanggal));
            }
        }

        public decimal Pendapatan
        {
            get => _pendapatan;
            set
            {
                _pendapatan = value;
                OnPropertyChanged(nameof(Pendapatan));
            }
        }

        public decimal Hutang
        {
            get => _hutang;
            set
            {
                _hutang = value;
                OnPropertyChanged(nameof(Hutang));
            }
        }

        public decimal Total
        {
            get => _total;
            set
            {
                _total = value;
                OnPropertyChanged(nameof(Total));
            }
        }

        public string Keterangan
        {
            get => _keterangan;
            set
            {
                _keterangan = value;
                OnPropertyChanged(nameof(Keterangan));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}