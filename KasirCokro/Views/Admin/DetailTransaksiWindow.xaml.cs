using DocumentFormat.OpenXml.Spreadsheet;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Border = System.Windows.Controls.Border;

namespace KasirCokro.Views.Admin
{
    public partial class DetailTransaksiWindow : Window
    {
        private string _kodeTransaksi;
        private string _namaPelanggan;
        private List<DetailTransaksiItem> _items = new List<DetailTransaksiItem>();
        private List<PembayaranItem> _pembayaranHistory = new List<PembayaranItem>();
        private decimal _totalSubtotal = 0;
        private decimal _totalTunaiDibayar = 0;
        private decimal _sisaHutang = 0;

        public DetailTransaksiWindow(string kodeTransaksi, string namaPelanggan)
        {
            InitializeComponent();
            _kodeTransaksi = kodeTransaksi;
            _namaPelanggan = namaPelanggan;
            LoadTransactionDetails();
        }

        public DetailTransaksiWindow(PihutangData pihutangData)
        {
            InitializeComponent();
            _kodeTransaksi = pihutangData.Barcode;
            _namaPelanggan = pihutangData.NamaProduk;
            LoadTransactionDetails();
        }

        private async void LoadTransactionDetails()
        {
            try
            {
                await LoadTransactionItems();
                await LoadPembayaranHistory();
                UpdateUI();
                CalculateTotals();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading transaction details: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadTransactionItems()
        {
            string query = @"
                SELECT 
                    t.kode_product,
                    t.nama,
                    t.qty,
                    t.harga,
                    t.subtotal,
                    t.laba,
                    t.retur,
                    t.tanggal_transaksi,
                    t.payment,
                    t.Tunai,
                    p.harga_beli
                FROM transactions t
                LEFT JOIN products p ON t.kode_product = p.barcode
                WHERE t.kode_transaksi = @kodeTransaksi";

            _items.Clear();

            using (MySqlConnection conn = Helpers.DatabaseHelper.GetConnection())
            {
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@kodeTransaksi", _kodeTransaksi);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (await reader.ReadAsync())
                        {
                            _items.Add(new DetailTransaksiItem
                            {
                                KodeBarang = reader["kode_product"].ToString(),
                                NamaBarang = reader["nama"].ToString(),
                                Qty = Convert.ToInt32(reader["qty"]),
                                Harga = Convert.ToDecimal(reader["harga"]),
                                Subtotal = Convert.ToDecimal(reader["subtotal"]),
                                Laba = Convert.ToDecimal(reader["laba"]),
                                Retur = Convert.ToInt32(reader["retur"]),
                                TanggalTransaksi = Convert.ToDateTime(reader["tanggal_transaksi"]),
                                Payment = reader["payment"].ToString(),
                                TunaiDibayar = Convert.ToDecimal(reader["Tunai"]),
                                HargaBeli = reader["harga_beli"] != DBNull.Value ? Convert.ToDecimal(reader["harga_beli"]) : 0
                            });
                        }
                    }
                }
            }
        }

        private async Task LoadPembayaranHistory()
        {
            string query = @"
                SELECT 
                    p.tgl_pembayaran,
                    p.tunai,
                    p.tgl_pembelian
                FROM pembayaran p
                WHERE p.kode_transaksi = @kodeTransaksi 
                    AND p.jenis = 'piutang'
                ORDER BY p.tgl_pembayaran ASC";

            _pembayaranHistory.Clear();

            using (MySqlConnection conn = Helpers.DatabaseHelper.GetConnection())
            {
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@kodeTransaksi", _kodeTransaksi);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (await reader.ReadAsync())
                        {
                            _pembayaranHistory.Add(new PembayaranItem
                            {
                                TanggalPembayaran = Convert.ToDateTime(reader["tgl_pembayaran"]),
                                Tunai = Convert.ToDecimal(reader["tunai"]),
                                TanggalPembelian = Convert.ToDateTime(reader["tgl_pembelian"])
                            });
                        }
                    }
                }
            }
        }

        private void UpdateUI()
        {
            txtCustomerName.Text = _namaPelanggan;
            if (_items.Count > 0)
            {
                txtTransactionDate.Text = _items[0].TanggalTransaksi.ToString("dd/MM/yyyy");
            }
            dgItems.ItemsSource = _items;
            UpdatePaymentHistoryUI();
        }

        private void UpdatePaymentHistoryUI()
        {
            
            var paymentHistoryPanel = FindName("PaymentHistoryPanel") as StackPanel;
            if (paymentHistoryPanel == null)
            {
                var paymentHistoryGrid = FindName("dgPaymentHistory") as DataGrid;
                if (paymentHistoryGrid != null)
                {
                    
                    var allPayments = new List<PaymentDisplayItem>();

                    
                    foreach (var payment in _pembayaranHistory)
                    {
                        allPayments.Add(new PaymentDisplayItem
                        {
                            Tanggal = payment.TanggalPembayaran,
                            Jumlah = payment.Tunai,
                            Keterangan = "Pembayaran"
                        });
                    }

                    paymentHistoryGrid.ItemsSource = allPayments;
                    return;
                }
            }

            if (paymentHistoryPanel != null)
            {
                paymentHistoryPanel.Children.Clear();

                
                foreach (var payment in _pembayaranHistory)
                {
                    AddPaymentHistoryItem(paymentHistoryPanel, payment.TanggalPembayaran, payment.Tunai, false);
                }
            }
        }

        private void AddPaymentHistoryItem(StackPanel panel, DateTime tanggal, decimal jumlah, bool isInitial)
        {
            var border = new Border
            {
                BorderBrush = System.Windows.Media.Brushes.LightGray,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 2, 0, 2),
                Padding = new Thickness(10),
                Background = System.Windows.Media.Brushes.LightGreen
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var lblTanggal = new Label
            {
                Content = tanggal.ToString("dd/MM/yyyy"),
                FontWeight = FontWeights.Bold
            };
            Grid.SetColumn(lblTanggal, 0);

            var lblJumlah = new Label
            {
                Content = $"Rp {jumlah:N0}",
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetColumn(lblJumlah, 1);

            var lblKeterangan = new Label
            {
                Content = "Pembayaran",
                FontStyle = FontStyles.Italic,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetColumn(lblKeterangan, 2);

            grid.Children.Add(lblTanggal);
            grid.Children.Add(lblJumlah);
            grid.Children.Add(lblKeterangan);

            border.Child = grid;
            panel.Children.Add(border);
        }

        private void CalculateTotals()
        {
            int totalQty = _items.Sum(x => x.Qty);
            int totalRetur = _items.Sum(x => x.Retur);
            decimal totalSubtotal = _items.Sum(x => x.Subtotal);
            decimal totalLaba = _items.Sum(x => x.Laba);

            decimal totalTunaiDibayar = _pembayaranHistory.Sum(x => x.Tunai);

            decimal sisaHutang = totalSubtotal - totalTunaiDibayar;

            txtTotalQty.Text = totalQty.ToString();
            txtTotalRetur.Text = totalRetur.ToString();
            txtTotalSubtotal.Text = totalSubtotal.ToString("N0");
            txtTotalLaba.Text = totalLaba.ToString("N0");
            txtSisaHutang.Text = sisaHutang.ToString("N0");

            var txtTotalDibayar = FindName("txtTotalDibayar") as TextBlock;
            if (txtTotalDibayar != null)
            {
                txtTotalDibayar.Text = totalTunaiDibayar.ToString("N0");
            }

            if (sisaHutang <= 0)
            {
                txtStatus.Text = "Lunas";
                txtStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green);
            }
            else
            {
                txtStatus.Text = "Belum Lunas";
                txtStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.DarkOrange);
            }

            _totalSubtotal = totalSubtotal;
            _totalTunaiDibayar = totalTunaiDibayar;
            _sisaHutang = sisaHutang;
        }

        private async void BtnAddPayment_Click(object sender, RoutedEventArgs e)
        {
            if (_sisaHutang <= 0)
            {
                MessageBox.Show("Transaksi ini sudah lunas!", "Informasi",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var addPaymentWindow = new AddPaymentHutang(_kodeTransaksi, _sisaHutang);
            addPaymentWindow.Owner = this;

            if (addPaymentWindow.ShowDialog() == true)
            {
                
                await LoadPembayaranHistory();
                CalculateTotals();
                UpdatePaymentHistoryUI();
            }
        }

        private async void BtnReturItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button btn = sender as Button;
                DetailTransaksiItem selectedItem = btn.DataContext as DetailTransaksiItem;

                if (selectedItem == null)
                {
                    MessageBox.Show("Item tidak ditemukan!", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (selectedItem.Qty <= selectedItem.Retur)
                {
                    MessageBox.Show("Quantity sudah habis diretur!", "Peringatan",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show($"Apakah Anda yakin ingin meretur 1 item {selectedItem.NamaBarang}?",
                    "Konfirmasi Retur", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    await ProcessRetur(selectedItem);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing return: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ProcessRetur(DetailTransaksiItem item)
        {
            using (MySqlConnection conn = Helpers.DatabaseHelper.GetConnection())
            {
                using (MySqlTransaction transaction = conn.BeginTransaction())
                {
                    try
                    {
                        string updateTransactionQuery = @"
                            UPDATE transactions 
                            SET retur = retur + 1,
                                subtotal = subtotal - @harga,
                                laba = laba - @labaPerItem
                            WHERE kode_transaksi = @kodeTransaksi 
                                AND kode_product = @kodeProduct";

                        decimal labaPerItem = item.Laba / item.Qty; 

                        using (MySqlCommand cmd = new MySqlCommand(updateTransactionQuery, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@harga", item.Harga);
                            cmd.Parameters.AddWithValue("@labaPerItem", labaPerItem);
                            cmd.Parameters.AddWithValue("@kodeTransaksi", _kodeTransaksi);
                            cmd.Parameters.AddWithValue("@kodeProduct", item.KodeBarang);
                            await cmd.ExecuteNonQueryAsync();
                        }

                        
                        string updateStockQuery = @"
                            UPDATE products 
                            SET stok = stok + 1 
                            WHERE barcode = @barcode";

                        using (MySqlCommand cmd = new MySqlCommand(updateStockQuery, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@barcode", item.KodeBarang);
                            await cmd.ExecuteNonQueryAsync();
                        }

                        transaction.Commit();

                        MessageBox.Show("Retur berhasil diproses!", "Sukses",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        await LoadTransactionItems();
                        UpdateUI();
                        CalculateTotals();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw new Exception($"Error processing return: {ex.Message}");
                    }
                }
            }
        }

        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                
                MessageBox.Show("Fitur print akan segera tersedia!", "Informasi",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error printing: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    public class DetailTransaksiItem : INotifyPropertyChanged
    {
        public string KodeBarang { get; set; }
        public string NamaBarang { get; set; }
        public int Qty { get; set; }
        public decimal Harga { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Laba { get; set; }
        public int Retur { get; set; }
        public DateTime TanggalTransaksi { get; set; }
        public string Payment { get; set; }
        public decimal TunaiDibayar { get; set; }
        public decimal HargaBeli { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class PembayaranItem : INotifyPropertyChanged
    {
        public DateTime TanggalPembayaran { get; set; }
        public decimal Tunai { get; set; }
        public DateTime TanggalPembelian { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class PaymentDisplayItem
    {
        public DateTime Tanggal { get; set; }
        public decimal Jumlah { get; set; }
        public string Keterangan { get; set; }
    }
}