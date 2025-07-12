using ClosedXML.Excel;
using KasirCokro.Views.Auth;
using Microsoft.Win32;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace KasirCokro.Views.Admin
{
    public partial class TransactionKeluarPage : Window
    {
        private ObservableCollection<TransactionKeluarModel> transactionList;

        public TransactionKeluarPage()
        {
            InitializeComponent();
            transactionList = new ObservableCollection<TransactionKeluarModel>();
            dgKas.ItemsSource = transactionList;

            dpTanggalFrom.SelectedDate = DateTime.Now;

            LoadTransactionData();
        }

        private async void LoadTransactionData()
        {
            try
            {
                ShowLoading(true);

                DateTime selectedDate = dpTanggalFrom.SelectedDate ?? DateTime.Now;

                using (MySqlConnection conn = Helpers.DatabaseHelper.GetConnection())
                {

                    string query = @"
                        SELECT 
                            t.tanggal_transaksi as TanggalKeluar,
                            t.tgl_pelunasan as TanggalPembayaran,
                            t.kode_product as KodeProduk,
                            t.nama as NamaBarang,
                            t.harga as Harga,
                            t.qty as Quantity,
                            t.subtotal as Subtotal,
                            t.laba as Laba,
                            t.kode_transaksi as KodeTransaksi,
                            t.payment as Payment,
                            t.namaPelanggan as NamaPelanggan,
                            t.status as Status
                        FROM transactions t
                        LEFT JOIN pembayaran p ON t.kode_transaksi = p.kode_transaksi AND p.jenis = 'piutang'
                        WHERE DATE(t.tanggal_transaksi) = @tanggal
                        ORDER BY t.tanggal_transaksi DESC";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@tanggal", selectedDate.Date);

                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            transactionList.Clear();

                            while (await reader.ReadAsync())
                            {
                                DateTime tanggalKeluar = Convert.ToDateTime(reader["TanggalKeluar"]);
                                DateTime? tanggalPembayaran = reader["TanggalPembayaran"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["TanggalPembayaran"]);
                                string kodeProduk = reader["KodeProduk"]?.ToString() ?? "";
                                string namaBarang = reader["NamaBarang"]?.ToString() ?? "";
                                decimal harga = Convert.ToDecimal(reader["Harga"]);
                                int quantity = Convert.ToInt32(reader["Quantity"]);
                                decimal subtotal = Convert.ToDecimal(reader["Subtotal"]);
                                decimal laba = Convert.ToDecimal(reader["Laba"]);
                                string kodeTransaksi = reader["KodeTransaksi"]?.ToString() ?? "";
                                string payment = reader["Payment"]?.ToString() ?? "";
                                string namaPelanggan = reader["NamaPelanggan"]?.ToString() ?? "";
                                string status = reader["Status"]?.ToString() ?? "";

                                transactionList.Add(new TransactionKeluarModel
                                {
                                    TanggalKeluar = tanggalKeluar,
                                    TanggalPembayaran = tanggalPembayaran,
                                    KodeProduk = kodeProduk,
                                    NamaBarang = namaBarang,
                                    Harga = harga,
                                    Quantity = quantity,
                                    Subtotal = subtotal,
                                    Laba = laba,
                                    KodeTransaksi = kodeTransaksi,
                                    Payment = payment,
                                    NamaPelanggan = namaPelanggan,
                                    Status = status
                                });
                            }
                        }
                    }
                }

                UpdateSummary();

                txtPeriode.Text = $"Periode: {selectedDate:dd MMMM yyyy}";
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

        private void UpdateSummary()
        {
            if (transactionList.Count == 0)
            {
                txtTotalTransaksi.Text = "Rp 0";
                txtTotalItemTransaksi.Text = "0";
                txtQtyTransaksi.Text = "0";
                txtLabaTransaksi.Text = "Rp 0";
                return;
            }

            decimal totalTransaksi = transactionList.Sum(t => t.Subtotal);
            int totalItem = transactionList.Count;
            int totalQty = transactionList.Sum(t => t.Quantity);
            decimal totalLaba = transactionList.Sum(t => t.Laba);

            txtTotalTransaksi.Text = $"Rp {totalTransaksi:N0}";
            txtTotalItemTransaksi.Text = totalItem.ToString();
            txtQtyTransaksi.Text = totalQty.ToString();
            txtLabaTransaksi.Text = $"Rp {totalLaba:N0}";
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
            if (dpTanggalFrom.SelectedDate.HasValue)
            {
                LoadTransactionData();
            }
        }

        private void BtnFilter_Click(object sender, RoutedEventArgs e)
        {
            LoadTransactionData();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadTransactionData();
        }

        private async void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (transactionList.Count == 0)
                {
                    MessageBox.Show("Tidak ada data untuk diexport!", "Peringatan",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ShowLoading(true);

                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "Excel Files|*.xlsx",
                    Title = "Export Data Transaksi Keluar",
                    FileName = $"TransaksiKeluar_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    await Task.Run(() => ExportToExcel(saveDialog.FileName));
                    ShowSuccessMessage($"Data berhasil di-export ke:\n{saveDialog.FileName}");
                }
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

        private void ExportToExcel(string filePath)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Transaksi Keluar");

                worksheet.Cell(1, 1).Value = "LAPORAN TRANSAKSI KELUAR";
                worksheet.Cell(1, 1).Style.Font.Bold = true;
                worksheet.Cell(1, 1).Style.Font.FontSize = 16;
                worksheet.Range(1, 1, 1, 9).Merge();
                worksheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                worksheet.Cell(2, 1).Value = $"Periode: {dpTanggalFrom.SelectedDate:dd MMMM yyyy}";
                worksheet.Cell(2, 1).Style.Font.Bold = true;
                worksheet.Range(2, 1, 2, 9).Merge();
                worksheet.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                worksheet.Cell(4, 1).Value = "RINGKASAN";
                worksheet.Cell(4, 1).Style.Font.Bold = true;
                worksheet.Cell(4, 1).Style.Fill.BackgroundColor = XLColor.LightGray;

                worksheet.Cell(5, 1).Value = "Total Transaksi:";
                worksheet.Cell(5, 2).Value = transactionList.Sum(t => t.Subtotal);
                worksheet.Cell(5, 2).Style.NumberFormat.Format = "#,##0";

                worksheet.Cell(6, 1).Value = "Total Item:";
                worksheet.Cell(6, 2).Value = transactionList.Count;

                worksheet.Cell(7, 1).Value = "Total Quantity:";
                worksheet.Cell(7, 2).Value = transactionList.Sum(t => t.Quantity);

                worksheet.Cell(8, 1).Value = "Total Laba:";
                worksheet.Cell(8, 2).Value = transactionList.Sum(t => t.Laba);
                worksheet.Cell(8, 2).Style.NumberFormat.Format = "#,##0";

                int headerRow = 10;
                worksheet.Cell(headerRow, 1).Value = "No";
                worksheet.Cell(headerRow, 2).Value = "Tanggal Keluar";
                worksheet.Cell(headerRow, 3).Value = "Tanggal Pembayaran";
                worksheet.Cell(headerRow, 4).Value = "Kode Produk";
                worksheet.Cell(headerRow, 5).Value = "Nama Barang";
                worksheet.Cell(headerRow, 6).Value = "Harga";
                worksheet.Cell(headerRow, 7).Value = "Quantity";
                worksheet.Cell(headerRow, 8).Value = "Subtotal";
                worksheet.Cell(headerRow, 9).Value = "Laba";
                worksheet.Cell(headerRow, 10).Value = "Payment";
                worksheet.Cell(headerRow, 11).Value = "Customer";
                worksheet.Cell(headerRow, 12).Value = "Status";

                var headerRange = worksheet.Range(headerRow, 1, headerRow, 12);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                int dataRow = headerRow + 1;
                for (int i = 0; i < transactionList.Count; i++)
                {
                    var transaction = transactionList[i];

                    worksheet.Cell(dataRow + i, 1).Value = i + 1;
                    worksheet.Cell(dataRow + i, 2).Value = transaction.TanggalKeluar;
                    worksheet.Cell(dataRow + i, 3).Value = transaction.TanggalPembayaran?.ToString("dd/MM/yyyy HH:mm") ?? "Belum Lunas";
                    worksheet.Cell(dataRow + i, 4).Value = transaction.KodeProduk;
                    worksheet.Cell(dataRow + i, 5).Value = transaction.NamaBarang;
                    worksheet.Cell(dataRow + i, 6).Value = transaction.Harga;
                    worksheet.Cell(dataRow + i, 7).Value = transaction.Quantity;
                    worksheet.Cell(dataRow + i, 8).Value = transaction.Subtotal;
                    worksheet.Cell(dataRow + i, 9).Value = transaction.Laba;
                    worksheet.Cell(dataRow + i, 10).Value = transaction.Payment;
                    worksheet.Cell(dataRow + i, 11).Value = transaction.NamaPelanggan;
                    worksheet.Cell(dataRow + i, 12).Value = transaction.Status;
                }

                var dataRange = worksheet.Range(dataRow, 1, dataRow + transactionList.Count - 1, 12);
                dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                worksheet.Range(dataRow, 6, dataRow + transactionList.Count - 1, 6).Style.NumberFormat.Format = "#,##0";
                worksheet.Range(dataRow, 8, dataRow + transactionList.Count - 1, 8).Style.NumberFormat.Format = "#,##0";
                worksheet.Range(dataRow, 9, dataRow + transactionList.Count - 1, 9).Style.NumberFormat.Format = "#,##0";

                for (int i = 0; i < transactionList.Count; i++)
                {
                    if (transactionList[i].TanggalPembayaran.HasValue)
                    {
                        worksheet.Cell(dataRow + i, 3).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
                    }
                }

                worksheet.Range(dataRow, 2, dataRow + transactionList.Count - 1, 2).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";

                worksheet.Columns().AdjustToContents();

                workbook.SaveAs(filePath);
            }
        }

        private async void BtnHapus_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dgKas.SelectedItem is TransactionKeluarModel selectedTransaction)
                {
                    MessageBoxResult result = MessageBox.Show(
                        $"Apakah Anda yakin ingin menghapus transaksi {selectedTransaction.KodeTransaksi}?",
                        "Konfirmasi Hapus",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        ShowLoading(true);

                        using (MySqlConnection conn = Helpers.DatabaseHelper.GetConnection())
                        {

                            using (MySqlTransaction transaction = await conn.BeginTransactionAsync())
                            {
                                try
                                {
                                    string updateStokQuery = @"
                                        UPDATE products 
                                        SET stok = stok + @qty 
                                        WHERE barcode = @kodeProduk";

                                    using (MySqlCommand cmd = new MySqlCommand(updateStokQuery, conn, transaction))
                                    {
                                        cmd.Parameters.AddWithValue("@qty", selectedTransaction.Quantity);
                                        cmd.Parameters.AddWithValue("@kodeProduk", selectedTransaction.KodeProduk);
                                        await cmd.ExecuteNonQueryAsync();
                                    }

                                    string deletePembayaranQuery = @"
                                        DELETE FROM pembayaran 
                                        WHERE kode_transaksi = @kodeTransaksi AND jenis = 'piutang'";

                                    using (MySqlCommand cmd = new MySqlCommand(deletePembayaranQuery, conn, transaction))
                                    {
                                        cmd.Parameters.AddWithValue("@kodeTransaksi", selectedTransaction.KodeTransaksi);
                                        await cmd.ExecuteNonQueryAsync();
                                    }

                                    string deleteTransactionQuery = @"
                                        DELETE FROM transactions 
                                        WHERE kode_transaksi = @kodeTransaksi";

                                    using (MySqlCommand cmd = new MySqlCommand(deleteTransactionQuery, conn, transaction))
                                    {
                                        cmd.Parameters.AddWithValue("@kodeTransaksi", selectedTransaction.KodeTransaksi);
                                        await cmd.ExecuteNonQueryAsync();
                                    }

                                    await transaction.CommitAsync();

                                    ShowSuccessMessage("Transaksi berhasil dihapus!");
                                    LoadTransactionData();
                                }
                                catch (Exception)
                                {
                                    await transaction.RollbackAsync();
                                    throw;
                                }
                            }
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Pilih transaksi yang ingin dihapus!", "Peringatan",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error menghapus transaksi: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ShowLoading(false);
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
            var kasPage = new KasPage();
            kasPage.Show();
            this.Close();
        }

        private void TransactionMasuk_Click(object sender, RoutedEventArgs e)
        {
            var transactionMasukPage = new TransactionMasukPage();
            transactionMasukPage.Show();
            this.Close();
        }
        private void TransactionKeluar_Click(object sender, RoutedEventArgs e)
        {
            LoadTransactionData();
        }

        private void Pihutang_Click(object sender, RoutedEventArgs e)
        {
            PihutangPage pihutangPage = new PihutangPage();
            pihutangPage.Show();
            this.Close();
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(
                "Apakah Anda yakin ingin logout?",
                "Konfirmasi Logout",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
            }
        }
    }

    public class TransactionKeluarModel
    {
        public DateTime TanggalKeluar { get; set; }
        public DateTime? TanggalPembayaran { get; set; }
        public string KodeProduk { get; set; }
        public string NamaBarang { get; set; }
        public decimal Harga { get; set; }
        public int Quantity { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Laba { get; set; }
        public string KodeTransaksi { get; set; }
        public string Payment { get; set; }
        public string NamaPelanggan { get; set; }
        public string Status { get; set; }

        public string Waktu => TanggalKeluar.ToString("dd/MM/yyyy");
        public string Barcode => TanggalPembayaran?.ToString("dd/MM/yyyy") ?? "Belum Lunas";
        public string NamaProduk => KodeProduk;
        public string Supplier => NamaBarang;
        public decimal HargaBeli => Harga;
        public decimal Total => Subtotal;
    }
}