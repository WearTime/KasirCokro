using KasirCokro.Helpers;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace KasirCokro.Views.Kasir
{
    public partial class TransaksiPenjualan : Window
    {
        private List<KeranjangItem> keranjang = new List<KeranjangItem>();

        public TransaksiPenjualan()
        {
            InitializeComponent();
        }

                public class KeranjangItem
        {
            public string Barcode { get; set; }
            public string NamaProduk { get; set; }
            public decimal HargaJual { get; set; }
            public int Jumlah { get; set; }
            public decimal Subtotal => HargaJual * Jumlah;
        }

        private void TxtBarcode_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                string barcode = txtBarcode.Text.Trim();
                TambahProduk(barcode);
                txtBarcode.Clear();
            }
        }

        private void TambahProduk(string barcode)
        {
            try
            {
                using (MySqlConnection conn = Helpers.DatabaseHelper.GetConnection())
                {
                    string query = "SELECT id, nama_produk, harga_jual FROM products WHERE barcode = @barcode";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@barcode", barcode);

                    var reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        int productId = Convert.ToInt32(reader["id"]);
                        string nama = reader["nama_produk"].ToString();
                        decimal harga = Convert.ToDecimal(reader["harga_jual"]);

                                                var item = keranjang.Find(i => i.Barcode == barcode);
                        if (item != null)
                        {
                            item.Jumlah += 1;
                        }
                        else
                        {
                            keranjang.Add(new KeranjangItem
                            {
                                Barcode = barcode,
                                NamaProduk = nama,
                                HargaJual = harga,
                                Jumlah = 1
                            });
                        }

                        dgKeranjang.ItemsSource = null;
                        dgKeranjang.ItemsSource = keranjang;
                        UpdateTotal();
                    }
                    else
                    {
                        MessageBox.Show("Produk tidak ditemukan!");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void UpdateTotal()
        {
            decimal total = 0;
            foreach (var item in keranjang)
            {
                total += item.Subtotal;
            }
            txtTotal.Text = $"Rp {total:N0}";
        }

        private void BtnCetak_Click(object sender, RoutedEventArgs e)
        {
            if (keranjang.Count == 0)
            {
                MessageBox.Show("Belum ada produk dalam keranjang.");
                return;
            }

                        int transaksiId = SimpanTransaksiKeDatabase();

                        CetakStruk(transaksiId);

                        keranjang.Clear();
            dgKeranjang.ItemsSource = null;
            txtTotal.Text = "Rp 0";
        }

        private int SimpanTransaksiKeDatabase()
        {
            string kodeTransaksi = "TRX-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
            decimal total = 0;
            foreach (var item in keranjang)
                total += item.Subtotal;

            int transaksiId = -1;

            using (MySqlConnection conn = Helpers.DatabaseHelper.GetConnection())
            {
                string insertTransaksi = "INSERT INTO transactions (kode_transaksi, jenis, total) VALUES (@kode, 'penjualan', @total); SELECT LAST_INSERT_ID();";
                MySqlCommand cmd = new MySqlCommand(insertTransaksi, conn);
                cmd.Parameters.AddWithValue("@kode", kodeTransaksi);
                cmd.Parameters.AddWithValue("@total", total);
                transaksiId = Convert.ToInt32(cmd.ExecuteScalar());

                foreach (var item in keranjang)
                {
                    string insertDetail = "INSERT INTO transaction_details (transaksi_id, product_id, jumlah, harga) VALUES (@transaksi_id, (SELECT id FROM products WHERE barcode = @barcode), @jumlah, @harga)";
                    MySqlCommand detailCmd = new MySqlCommand(insertDetail, conn);
                    detailCmd.Parameters.AddWithValue("@transaksi_id", transaksiId);
                    detailCmd.Parameters.AddWithValue("@barcode", item.Barcode);
                    detailCmd.Parameters.AddWithValue("@jumlah", item.Jumlah);
                    detailCmd.Parameters.AddWithValue("@harga", item.HargaJual);
                    detailCmd.ExecuteNonQuery();
                }
            }

            return transaksiId;
        }

        private void CetakStruk(int transaksiId)
        {
            try
            {
                RawPrinterHelper.PrintStruk(transaksiId, keranjang);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal cetak struk: " + ex.Message);
            }
        }

        private void BtnManual_Click(object sender, RoutedEventArgs e)
        {
                        MessageBox.Show("Fitur tambah manual akan ditambahkan nanti.");
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
                        keranjang.Clear();
            dgKeranjang.ItemsSource = null;
            txtTotal.Text = "Rp 0";
            txtBarcode.Clear();
            txtBarcode.Focus();
            MessageBox.Show("Keranjang telah dikosongkan.");
        }

        private void BtnRiwayat_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var riwayat = new RiwayatTransaksi();
                riwayat.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error membuka riwayat: " + ex.Message);
            }
        }

        private void BtnSimpanDraft_Click(object sender, RoutedEventArgs e)
        {
            if (keranjang.Count == 0)
            {
                MessageBox.Show("Belum ada produk dalam keranjang untuk disimpan.");
                return;
            }

            try
            {
                                                MessageBox.Show("Draft transaksi berhasil disimpan.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error menyimpan draft: " + ex.Message);
            }
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Apakah Anda yakin ingin logout?", "Konfirmasi Logout",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                                                this.Close();

                                                            }
        }

        private void BtnKosongkan_Click(object sender, RoutedEventArgs e)
        {
            if (keranjang.Count == 0)
            {
                MessageBox.Show("Keranjang sudah kosong.");
                return;
            }

            var result = MessageBox.Show("Apakah Anda yakin ingin mengosongkan keranjang?",
                "Konfirmasi", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                keranjang.Clear();
                dgKeranjang.ItemsSource = null;
                txtTotal.Text = "Rp 0";
                txtBarcode.Clear();
                txtBarcode.Focus();
            }
        }

        private void BtnHapus_Click(object sender, RoutedEventArgs e)
        {
                        if (dgKeranjang.SelectedItem != null)
            {
                var selectedItem = (KeranjangItem)dgKeranjang.SelectedItem;
                keranjang.Remove(selectedItem);

                dgKeranjang.ItemsSource = null;
                dgKeranjang.ItemsSource = keranjang;
                UpdateTotal();

                MessageBox.Show($"Produk {selectedItem.NamaProduk} telah dihapus dari keranjang.");
            }
            else
            {
                MessageBox.Show("Pilih produk yang ingin dihapus dari keranjang.");
            }
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
                        if (dgKeranjang.SelectedItem != null)
            {
                var selectedItem = (KeranjangItem)dgKeranjang.SelectedItem;

                                string input = Microsoft.VisualBasic.Interaction.InputBox(
                    $"Masukkan jumlah untuk {selectedItem.NamaProduk}:",
                    "Edit Jumlah",
                    selectedItem.Jumlah.ToString());

                if (int.TryParse(input, out int jumlahBaru) && jumlahBaru > 0)
                {
                    selectedItem.Jumlah = jumlahBaru;
                    dgKeranjang.ItemsSource = null;
                    dgKeranjang.ItemsSource = keranjang;
                    UpdateTotal();
                }
                else if (!string.IsNullOrEmpty(input))
                {
                    MessageBox.Show("Jumlah harus berupa angka positif.");
                }
            }
            else
            {
                MessageBox.Show("Pilih produk yang ingin diedit dari keranjang.");
            }
        }

        private void BtnDashboard_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                
                 var dashboardWindow = new DashboardKasir();
                dashboardWindow.Show();
                 this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error membuka dashboard: " + ex.Message);
            }
        }

        private void BtnBayar_Click(object sender, RoutedEventArgs e)
        {
            if (keranjang.Count == 0)
            {
                MessageBox.Show("Belum ada produk dalam keranjang.");
                return;
            }

            try
            {
                                decimal total = 0;
                foreach (var item in keranjang)
                {
                    total += item.Subtotal;
                }

                                string input = Microsoft.VisualBasic.Interaction.InputBox(
                    $"Total: Rp {total:N0}\nMasukkan jumlah bayar:",
                    "Pembayaran",
                    total.ToString());

                if (decimal.TryParse(input, out decimal jumlahBayar))
                {
                    if (jumlahBayar >= total)
                    {
                        decimal kembalian = jumlahBayar - total;

                                                int transaksiId = SimpanTransaksiKeDatabase();

                                                MessageBox.Show($"Pembayaran berhasil!\n" +
                                      $"Total: Rp {total:N0}\n" +
                                      $"Bayar: Rp {jumlahBayar:N0}\n" +
                                      $"Kembalian: Rp {kembalian:N0}");

                                                CetakStruk(transaksiId);

                                                keranjang.Clear();
                        dgKeranjang.ItemsSource = null;
                        txtTotal.Text = "Rp 0";
                        txtBarcode.Clear();
                        txtBarcode.Focus();
                    }
                    else
                    {
                        MessageBox.Show("Jumlah bayar kurang dari total belanja.");
                    }
                }
                else if (!string.IsNullOrEmpty(input))
                {
                    MessageBox.Show("Masukkan jumlah bayar yang valid.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error dalam proses pembayaran: " + ex.Message);
            }
        }
    }
}