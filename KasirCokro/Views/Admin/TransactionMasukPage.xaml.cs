using ClosedXML.Excel;
using KasirCokro.Views.Auth;
using MySql.Data.MySqlClient;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Windows;
using System.Windows.Controls;
using Button = System.Windows.Controls.Button;

namespace KasirCokro.Views.Admin
{
    public partial class TransactionMasukPage : Window
    {
        private ObservableCollection<TransactionMasukModel> _transactionMasukList;

        public TransactionMasukPage()
        {
            InitializeComponent();
            InitializeData();
        }

        private async void InitializeData()
        {
            _transactionMasukList = new ObservableCollection<TransactionMasukModel>();
            dgKas.ItemsSource = _transactionMasukList;

            dpTanggalFrom.SelectedDate = DateTime.Today;

            await LoadTransactionMasukData();
        }

        private async Task LoadTransactionMasukData(DateTime? tanggal = null)
        {
            ShowLoading(true);

            try
            {
                DateTime filterDate = tanggal ?? DateTime.Today;

                _transactionMasukList.Clear();

                using (MySqlConnection conn = Helpers.DatabaseHelper.GetConnection())
                {
                    string query = @"
                        SELECT 
                            ti.id,
                            ti.tgl,
                            ti.kode_product,
                            ti.nama,
                            ti.suplier,
                            ti.qty,
                            ti.harga,
                            ti.subtotal,
                            ti.payment,
                            ti.kode_transaksi,
                            ti.no_faktur
                        FROM transaction_in ti
                        WHERE DATE(ti.tgl) = @tanggal
                        ORDER BY ti.id DESC";

                    using (MySqlCommand command = new MySqlCommand(query, conn))
                    {
                        command.Parameters.AddWithValue("@tanggal", filterDate.Date);

                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (await reader.ReadAsync())
                            {
                                var transactionMasuk = new TransactionMasukModel
                                {
                                    Id = reader.GetInt32("id"),
                                    Waktu = reader.GetDateTime("tgl").ToString("dd/MM/yyyy"),
                                    Barcode = reader.GetString("kode_product"),
                                    NamaProduk = reader.GetString("nama"),
                                    Supplier = reader.GetString("suplier"),
                                    Quantity = reader.GetInt32("qty"),
                                    HargaBeli = reader.GetDecimal("harga"),
                                    Total = reader.GetDecimal("subtotal"),
                                    IsKredit = reader.GetString("payment").ToLower() == "kredit",
                                    KodeTransaksi = reader.GetString("kode_transaksi"),
                                    NoFaktur = reader.GetString("no_faktur")
                                };

                                _transactionMasukList.Add(transactionMasuk);
                            }
                        }
                    }
                }

                txtTotalTransaksi.Text = _transactionMasukList.Count.ToString();

                txtPeriode.Text = $"Periode: {filterDate:dd MMMM yyyy}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ShowLoading(false);
            }
        }

        private void ShowLoading(bool show)
        {
            loadingOverlay.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ShowSuccessMessage(string message)
        {
            txtSuccessMessage.Text = message;
            successMessage.Visibility = Visibility.Visible;

            Task.Delay(3000).ContinueWith(_ =>
{
    Dispatcher.Invoke(() =>
    {
        successMessage.Visibility = Visibility.Collapsed;
    });
});
        }

        private async void BtnFilter_Click(object sender, RoutedEventArgs e)
        {
            if (dpTanggalFrom.SelectedDate.HasValue)
            {
                await LoadTransactionMasukData(dpTanggalFrom.SelectedDate.Value);
            }
            else
            {
                MessageBox.Show("Pilih tanggal terlebih dahulu!", "Peringatan",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            dpTanggalFrom.SelectedDate = DateTime.Today;
            await LoadTransactionMasukData();
        }

        private async void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dpTanggalFrom.SelectedDate.HasValue)
            {
                await LoadTransactionMasukData(dpTanggalFrom.SelectedDate.Value);
            }
        }

        private async void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_transactionMasukList.Count == 0)
                {
                    MessageBox.Show("Tidak ada data untuk di-export!", "Peringatan",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ShowLoading(true);

                await Task.Run(() =>
                {
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Transaksi Masuk");

                        worksheet.Cell(1, 1).Value = "Tanggal";
                        worksheet.Cell(1, 2).Value = "No Faktur";
                        worksheet.Cell(1, 3).Value = "Barcode";
                        worksheet.Cell(1, 4).Value = "Nama Produk";
                        worksheet.Cell(1, 5).Value = "Supplier";
                        worksheet.Cell(1, 6).Value = "Quantity";
                        worksheet.Cell(1, 7).Value = "Harga Beli";
                        worksheet.Cell(1, 8).Value = "Total";
                        worksheet.Cell(1, 9).Value = "Status";

                        var headerRange = worksheet.Range(1, 1, 1, 9);
                        headerRange.Style.Font.Bold = true;
                        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        for (int i = 0; i < _transactionMasukList.Count; i++)
                        {
                            var item = _transactionMasukList[i];
                            int row = i + 2;

                            worksheet.Cell(row, 1).Value = item.Waktu;
                            worksheet.Cell(row, 2).Value = item.NoFaktur;
                            worksheet.Cell(row, 3).Value = item.Barcode;
                            worksheet.Cell(row, 4).Value = item.NamaProduk;
                            worksheet.Cell(row, 5).Value = item.Supplier;
                            worksheet.Cell(row, 6).Value = item.Quantity;
                            worksheet.Cell(row, 7).Value = item.HargaBeli;
                            worksheet.Cell(row, 8).Value = item.Total;
                            worksheet.Cell(row, 9).Value = item.IsKredit ? "Kredit" : "Tunai";

                            worksheet.Cell(row, 7).Style.NumberFormat.Format = "Rp #,##0";
                            worksheet.Cell(row, 8).Style.NumberFormat.Format = "Rp #,##0";
                        }

                        if (_transactionMasukList.Count > 0)
                        {
                            var dataRange = worksheet.Range(2, 1, _transactionMasukList.Count + 1, 9);
                            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                        }

                        worksheet.Columns().AdjustToContents();

                        int summaryRow = _transactionMasukList.Count + 3;
                        worksheet.Cell(summaryRow, 7).Value = "Total:";
                        worksheet.Cell(summaryRow, 8).FormulaA1 = $"=SUM(H2:H{_transactionMasukList.Count + 1})";
                        worksheet.Cell(summaryRow, 7).Style.Font.Bold = true;
                        worksheet.Cell(summaryRow, 8).Style.Font.Bold = true;
                        worksheet.Cell(summaryRow, 8).Style.NumberFormat.Format = "Rp #,##0";

                        string fileName = $"TransaksiMasuk_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                        string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);

                        workbook.SaveAs(filePath);

                        Dispatcher.Invoke(() =>
                        {
                            ShowSuccessMessage($"File berhasil di-export ke Desktop: {fileName}");

                            Process.Start(new ProcessStartInfo
                            {
                                FileName = filePath,
                                UseShellExecute = true
                            });
                        });
                    }
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
        private async void BtnHapus_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button button = sender as Button;

                TransactionMasukModel selectedItem = button.DataContext as TransactionMasukModel;

                if (selectedItem == null)
                {
                    MessageBox.Show("Item tidak ditemukan!", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                MessageBoxResult result = MessageBox.Show(
    $"Apakah Anda yakin ingin menghapus transaksi ini?\n\n" +
    $"Produk: {selectedItem.NamaProduk}\n" +
    $"Supplier: {selectedItem.Supplier}\n" +
    $"Quantity: {selectedItem.Quantity}\n" +
    $"Total: Rp {selectedItem.Total:N0}",
    "Konfirmasi Penghapusan",
    MessageBoxButton.YesNo,
    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                ShowLoading(true);

                bool deleteSuccess = await DeleteTransactionFromDatabase(selectedItem);

                if (deleteSuccess)
                {
                    _transactionMasukList.Remove(selectedItem);

                    txtTotalTransaksi.Text = _transactionMasukList.Count.ToString();

                    ShowSuccessMessage("Transaksi berhasil dihapus!");
                }
                else
                {
                    MessageBox.Show("Gagal menghapus transaksi dari database!", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saat menghapus transaksi: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ShowLoading(false);
            }
        }

        private async Task<bool> DeleteTransactionFromDatabase(TransactionMasukModel transaction)
        {
            try
            {
                using (MySqlConnection conn = Helpers.DatabaseHelper.GetConnection())
                {
                    string deleteQuery = @"
                DELETE FROM transaction_in 
                WHERE kode_transaksi = @kode_transaksi 
                AND kode_product = @kode_product 
                AND DATE(tgl) = @tanggal
                AND qty = @qty 
                AND subtotal = @subtotal";

                    using (MySqlCommand command = new MySqlCommand(deleteQuery, conn))
                    {
                        command.Parameters.AddWithValue("@kode_transaksi", transaction.KodeTransaksi);
                        command.Parameters.AddWithValue("@kode_product", transaction.Barcode);

                        DateTime tanggalTransaksi;
                        if (DateTime.TryParseExact(transaction.Waktu, "dd/MM/yyyy",
                            System.Globalization.CultureInfo.InvariantCulture,
                            System.Globalization.DateTimeStyles.None, out tanggalTransaksi))
                        {
                            command.Parameters.AddWithValue("@tanggal", tanggalTransaksi.Date);
                        }
                        else
                        {
                            command.Parameters.AddWithValue("@tanggal", DateTime.Today);
                        }

                        command.Parameters.AddWithValue("@qty", transaction.Quantity);
                        command.Parameters.AddWithValue("@subtotal", transaction.Total);

                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting transaction: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> DeleteTransactionByIdFromDatabase(int transactionId)
        {
            try
            {
                using (MySqlConnection conn = Helpers.DatabaseHelper.GetConnection())
                {
                    string deleteQuery = "DELETE FROM transaction_in WHERE id = @id";

                    using (MySqlCommand command = new MySqlCommand(deleteQuery, conn))
                    {
                        command.Parameters.AddWithValue("@id", transactionId);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting transaction by ID: {ex.Message}");
                return false;
            }
        }

        private void BtnDashboard_Click(object sender, RoutedEventArgs e)
        {
            var dashboardWindow = new DashboardAdmin();
            dashboardWindow.Show();
            this.Close();
        }

        private void Product_Click(object sender, RoutedEventArgs e)
        {
            var productWindow = new ProductsPage();
            productWindow.Show();
            this.Close();
        }

        private void Supplier_Click(object sender, RoutedEventArgs e)
        {
            var supplierWindow = new SuppliersPage();
            supplierWindow.Show();
            this.Close();
        }

        private void BrngMasuk_Click(object sender, RoutedEventArgs e)
        {
            var barangMasukWindow = new BarangMasukPage();
            barangMasukWindow.Show();
            this.Close();
        }

        private void BrngKeluar_Click(object sender, RoutedEventArgs e)
        {
            var barangKeluarWindow = new BarangKeluarPage();
            barangKeluarWindow.Show();
            this.Close();
        }

        private void Kas_Click(object sender, RoutedEventArgs e)
        {
            KasPage kasPage = new KasPage();
            kasPage.Show();
            this.Close();
        }

        private void TransactionMasuk_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(async () => await LoadTransactionMasukData());
        }

        private void TransactionKeluar_Click(object sender, RoutedEventArgs e)
        {
            var transactionKeluarWindow = new TransactionKeluarPage();
            transactionKeluarWindow.Show();
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
                var loginWindow = new LoginWindow1();
                loginWindow.Show();
                this.Close();
            }
        }
    }

    public class TransactionMasukModel : INotifyPropertyChanged
    {
        private int _id;
        private string _waktu;
        private string _barcode;
        private string _namaProduk;
        private string _supplier;
        private int _quantity;
        private decimal _hargaBeli;
        private decimal _total;
        private bool _isKredit;
        private string _kodeTransaksi;
        private string _noFaktur;

        public int Id
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged(nameof(Id));
            }
        }

        public string Waktu
        {
            get => _waktu;
            set
            {
                _waktu = value;
                OnPropertyChanged(nameof(Waktu));
            }
        }

        public string Barcode
        {
            get => _barcode;
            set
            {
                _barcode = value;
                OnPropertyChanged(nameof(Barcode));
            }
        }

        public string NamaProduk
        {
            get => _namaProduk;
            set
            {
                _namaProduk = value;
                OnPropertyChanged(nameof(NamaProduk));
            }
        }

        public string Supplier
        {
            get => _supplier;
            set
            {
                _supplier = value;
                OnPropertyChanged(nameof(Supplier));
            }
        }

        public int Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value;
                OnPropertyChanged(nameof(Quantity));
            }
        }

        public decimal HargaBeli
        {
            get => _hargaBeli;
            set
            {
                _hargaBeli = value;
                OnPropertyChanged(nameof(HargaBeli));
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

        public bool IsKredit
        {
            get => _isKredit;
            set
            {
                _isKredit = value;
                OnPropertyChanged(nameof(IsKredit));
            }
        }

        public string KodeTransaksi
        {
            get => _kodeTransaksi;
            set
            {
                _kodeTransaksi = value;
                OnPropertyChanged(nameof(KodeTransaksi));
            }
        }

        public string NoFaktur
        {
            get => _noFaktur;
            set
            {
                _noFaktur = value;
                OnPropertyChanged(nameof(NoFaktur));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}