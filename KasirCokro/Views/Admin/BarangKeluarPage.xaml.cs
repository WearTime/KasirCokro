using KasirCokro.Views.Auth;
using MySql.Data.MySqlClient;
using SixLabors.Fonts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Font = System.Drawing.Font;

namespace KasirCokro.Views.Admin
{
    public partial class BarangKeluarPage : Window, INotifyPropertyChanged
    {
        private ObservableCollection<KeranjangItem> keranjangItems;
        private ProdukItem currentSelectedProduct;
        private decimal totalTransaksi = 0;
        private int totalItem = 0;
        private string currentTransactionCode;

        public event PropertyChangedEventHandler PropertyChanged;

        public BarangKeluarPage()
        {
            InitializeComponent();
            InitializeKeranjang();
            LoadDataProduk();
            GenerateTransactionCode();
            txtBarcode.Focus();
        }

        private void InitializeKeranjang()
        {
            keranjangItems = new ObservableCollection<KeranjangItem>();
            dgKeranjang.ItemsSource = keranjangItems;
            keranjangItems.CollectionChanged += KeranjangItems_CollectionChanged;
        }

        private void KeranjangItems_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateTotalTransaksi();
        }

        private void GenerateTransactionCode()
        {
            currentTransactionCode = "TRX" + DateTime.Now.ToString("yyyyMMddHHmmss");
        }

        private void LoadDataProduk()
        {
            try
            {
                using (MySqlConnection conn = Helpers.DatabaseHelper.GetConnection())
                {

                    string query = @"SELECT p.*, s.nama_supplier 
                                   FROM products p 
                                   LEFT JOIN suppliers s ON p.supplier_id = s.id 
                                   WHERE p.stok > 0";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    dgProdukPencarian.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading products: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TxtBarcode_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string barcode = txtBarcode.Text.Trim();
                if (!string.IsNullOrEmpty(barcode))
                {
                    SearchProductByBarcode(barcode);
                }
            }
        }

        private void TxtBarcode_TextChanged(object sender, TextChangedEventArgs e)
        {

            string barcode = txtBarcode.Text.Trim();

            if (txtBarcode.Text.Length >= 13)
            {
                SearchProductByBarcode(barcode);
            }
        }

        private void SearchProductByBarcode(string barcode)
        {
            try
            {
                using (MySqlConnection conn = Helpers.DatabaseHelper.GetConnection())
                {

                    string query = @"SELECT p.*, s.nama_supplier 
                                   FROM products p 
                                   LEFT JOIN suppliers s ON p.supplier_id = s.id 
                                   WHERE p.barcode = @barcode AND p.stok > 0";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@barcode", barcode);

                    MySqlDataReader reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        currentSelectedProduct = new ProdukItem
                        {
                            Id = reader.GetInt32("id"),
                            Barcode = reader.GetString("barcode"),
                            NamaProduk = reader.GetString("nama_produk"),
                            HargaJual = reader.GetDecimal("harga_jual"),
                            Stok = reader.GetInt32("stok"),
                            SupplierId = reader.GetInt32("supplier_id"),
                            NamaSupplier = reader.GetString("nama_supplier"),
                            HargaBeli = reader.GetDecimal("harga_beli"),
                            MarkUp = reader.GetDecimal("mark_up")
                        };

                        reader.Close();
                        ShowProductModal();
                    }
                    else
                    {
                        MessageBox.Show("Produk tidak ditemukan atau stok habis!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                        txtBarcode.Clear();
                        txtBarcode.Focus();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error searching product: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowProductModal()
        {
            if (currentSelectedProduct != null)
            {
                txtModalBarcode.Text = currentSelectedProduct.Barcode;
                txtModalNamaProduk.Text = currentSelectedProduct.NamaProduk;
                txtModalHarga.Text = $"Rp {currentSelectedProduct.HargaJual:N0}";
                txtModalStok.Text = currentSelectedProduct.Stok.ToString();
                txtQuantity.Text = "1";
                UpdateModalSubtotal();

                modalOverlay.Visibility = Visibility.Visible;
                txtQuantity.Focus();
                txtQuantity.SelectAll();
            }
        }

        private void TxtQuantity_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !char.IsDigit(e.Text, 0);
        }

        private void UpdateModalSubtotal()
        {
            if (currentSelectedProduct != null && int.TryParse(txtQuantity.Text, out int qty))
            {
                if (qty > currentSelectedProduct.Stok)
                {
                    borderWarning.Visibility = Visibility.Visible;
                    txtWarning.Text = $"Quantity melebihi stok tersedia ({currentSelectedProduct.Stok})";
                }
                else
                {
                    borderWarning.Visibility = Visibility.Collapsed;
                }

                decimal subtotal = currentSelectedProduct.HargaJual * qty;
                txtModalSubtotal.Text = $"Rp {subtotal:N0}";
            }
        }

        private void BtnTambahKeKeranjang_Click(object sender, RoutedEventArgs e)
        {
            if (currentSelectedProduct != null && int.TryParse(txtQuantity.Text, out int qty))
            {
                if (qty <= 0)
                {
                    MessageBox.Show("Quantity harus lebih dari 0!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (qty > currentSelectedProduct.Stok)
                {
                    MessageBox.Show($"Quantity melebihi stok tersedia ({currentSelectedProduct.Stok})!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }


                var existingItem = keranjangItems.FirstOrDefault(k => k.Barcode == currentSelectedProduct.Barcode);
                if (existingItem != null)
                {
                    existingItem.Quantity += qty;
                    existingItem.UpdateSubtotal();
                }
                else
                {
                    keranjangItems.Add(new KeranjangItem
                    {
                        Barcode = currentSelectedProduct.Barcode,
                        NamaProduk = currentSelectedProduct.NamaProduk,
                        HargaJual = currentSelectedProduct.HargaJual,
                        Quantity = qty,
                        HargaBeli = currentSelectedProduct.HargaBeli,
                        MarkUp = currentSelectedProduct.MarkUp,
                        SupplierId = currentSelectedProduct.SupplierId
                    });

                }

                modalOverlay.Visibility = Visibility.Collapsed;
                txtBarcode.Clear();
                txtBarcode.Focus();
                currentSelectedProduct = null;
            }
        }

        private void BtnBatalModal_Click(object sender, RoutedEventArgs e)
        {
            modalOverlay.Visibility = Visibility.Collapsed;
            txtBarcode.Clear();
            txtBarcode.Focus();
            currentSelectedProduct = null;
        }

        private void BtnCariManual_Click(object sender, RoutedEventArgs e)
        {
            modalPencarian.Visibility = Visibility.Visible;
            txtCariProduk.Focus();
        }

        private void TxtCariProduk_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchTerm = txtCariProduk.Text.Trim();
            if (string.IsNullOrEmpty(searchTerm))
            {
                LoadDataProduk();
                return;
            }

            SearchProducts(searchTerm);
        }

        private void SearchProducts(string searchTerm)
        {
            try
            {
                using (MySqlConnection conn = Helpers.DatabaseHelper.GetConnection())
                {

                    string query = @"SELECT p.*, s.nama_supplier 
                                   FROM products p 
                                   LEFT JOIN suppliers s ON p.supplier_id = s.id 
                                   WHERE (p.nama_produk LIKE @search OR p.barcode LIKE @search) 
                                   AND p.stok > 0";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@search", $"%{searchTerm}%");

                    MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    dgProdukPencarian.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error searching products: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnPilihProduk_Click(object sender, RoutedEventArgs e)
        {
            if (dgProdukPencarian.SelectedItem != null)
            {
                DataRowView row = (DataRowView)dgProdukPencarian.SelectedItem;
                SelectProductFromSearch(row);
            }
        }

        private void DgProdukPencarian_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgProdukPencarian.SelectedItem != null)
            {
                DataRowView row = (DataRowView)dgProdukPencarian.SelectedItem;
                SelectProductFromSearch(row);
            }
        }

        private void SelectProductFromSearch(DataRowView row)
        {
            currentSelectedProduct = new ProdukItem
            {
                Id = Convert.ToInt32(row["id"]),
                Barcode = row["barcode"].ToString(),
                NamaProduk = row["nama_produk"].ToString(),
                HargaJual = Convert.ToDecimal(row["harga_jual"]),
                Stok = Convert.ToInt32(row["stok"]),
                SupplierId = Convert.ToInt32(row["supplier_id"]),
                NamaSupplier = row["nama_supplier"]?.ToString() ?? "",
                HargaBeli = row["harga_beli"] == DBNull.Value ? 0 : Convert.ToDecimal(row["harga_beli"]),
                MarkUp = row["mark_up"] == DBNull.Value ? 0 : Convert.ToDecimal(row["mark_up"])
            };

            modalPencarian.Visibility = Visibility.Collapsed;
            ShowProductModal();
        }

        private void BtnTutupPencarian_Click(object sender, RoutedEventArgs e)
        {
            modalPencarian.Visibility = Visibility.Collapsed;
        }

        private void BtnCari_Click(object sender, RoutedEventArgs e)
        {
            SearchProducts(txtCariProduk.Text.Trim());
        }

        private void BtnHapusItem_Click(object sender, RoutedEventArgs e)
        {
            if (dgKeranjang.SelectedItem is KeranjangItem item)
            {
                keranjangItems.Remove(item);
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            txtBarcode.Clear();
            txtBarcode.Focus();
        }

        private void BtnClearAll_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Hapus semua item dari keranjang?", "Konfirmasi", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                keranjangItems.Clear();
                txtBarcode.Focus();
            }
        }

        private void UpdateTotalTransaksi()
        {
            totalTransaksi = keranjangItems.Sum(k => k.Subtotal);
            totalItem = keranjangItems.Sum(k => k.Quantity);

            txtTotalTransaksi.Text = $"Rp {totalTransaksi:N0}";
            txtTotalItem.Text = totalItem.ToString();

            UpdateKembalian();
        }

        private void CmbMetodePembayaran_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbMetodePembayaran?.SelectedItem is ComboBoxItem item &&
                    pnlNamaPelanggan != null &&
                    txtJumlahBayar != null)
            {
                if (item.Content?.ToString() == "Kredit")
                {
                    pnlNamaPelanggan.Visibility = Visibility.Visible;

                    txtJumlahBayar.Clear();
                    txtJumlahBayar.IsReadOnly = false;
                }
                else
                {
                    pnlNamaPelanggan.Visibility = Visibility.Collapsed;
                    txtJumlahBayar.IsReadOnly = false;
                }
            }
        }

        private void TxtJumlahBayar_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateKembalian();
        }

        private void UpdateKembalian()
        {
            if (decimal.TryParse(txtJumlahBayar.Text, out decimal jumlahBayar))
            {
                string metodePembayaran = ((ComboBoxItem)cmbMetodePembayaran.SelectedItem)?.Content?.ToString();

                if (metodePembayaran == "Kredit")
                {

                    decimal sisaPembayaran = totalTransaksi - jumlahBayar;
                    if (sisaPembayaran > 0)
                    {
                        txtKembalian.Text = $"Sisa: Rp {sisaPembayaran:N0}";
                        txtKembalian.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Orange);
                    }
                    else if (sisaPembayaran < 0)
                    {
                        txtKembalian.Text = $"Kembalian: Rp {Math.Abs(sisaPembayaran):N0}";
                        txtKembalian.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 125, 50));
                    }
                    else
                    {
                        txtKembalian.Text = "Lunas";
                        txtKembalian.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 125, 50));
                    }
                }
                else
                {

                    decimal kembalian = jumlahBayar - totalTransaksi;
                    txtKembalian.Text = $"Rp {kembalian:N0}";

                    if (kembalian < 0)
                    {
                        txtKembalian.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
                    }
                    else
                    {
                        txtKembalian.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 125, 50));
                    }
                }
            }
            else
            {
                txtKembalian.Text = "Rp 0";
            }
        }

        private async void BtnSimpan_Click(object sender, RoutedEventArgs e)
        {
            if (keranjangItems.Count == 0)
            {
                MessageBox.Show("Keranjang kosong!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!decimal.TryParse(txtJumlahBayar.Text, out decimal jumlahBayar))
            {
                MessageBox.Show("Jumlah bayar tidak valid!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string metodePembayaran = ((ComboBoxItem)cmbMetodePembayaran.SelectedItem).Content.ToString().ToLower();
            string namaPelanggan = metodePembayaran == "kredit" ? txtNamaPelanggan.Text.Trim() : "-";

            if (metodePembayaran == "kredit" && string.IsNullOrEmpty(namaPelanggan))
            {
                MessageBox.Show("Nama pelanggan harus diisi untuk pembayaran kredit!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }


            if (metodePembayaran == "kredit" && jumlahBayar <= 0)
            {
                MessageBox.Show("Untuk pembayaran kredit, minimal harus ada pembayaran DP!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }


            if (metodePembayaran == "tunai" && jumlahBayar < totalTransaksi)
            {
                MessageBox.Show("Jumlah bayar kurang untuk pembayaran tunai!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ShowLoading();

            try
            {
                await SimpanTransaksi(metodePembayaran, namaPelanggan, jumlahBayar);
                ShowSuccessMessage("Transaksi berhasil disimpan!");


                await Task.Delay(2000);
                ResetForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving transaction: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                HideLoading();
            }
        }

        private async Task SimpanTransaksi(string metodePembayaran, string namaPelanggan, decimal jumlahBayar)
        {
            using (MySqlConnection conn = Helpers.DatabaseHelper.GetConnection())
            {

                using (MySqlTransaction transaction = await conn.BeginTransactionAsync())
                {
                    try
                    {

                        foreach (var item in keranjangItems)
                        {
                            string insertQuery = @"INSERT INTO transactions 
                                                 (kode_transaksi, kode_product, nama, qty, harga, subtotal, 
                                                  mark_up, laba, payment, namaPelanggan, Tunai, status, 
                                                  tanggal_transaksi, tgl_pelunasan) 
                                                 VALUES 
                                                 (@kode_transaksi, @kode_product, @nama, @qty, @harga, @subtotal, 
                                                  @mark_up, @laba, @payment, @namaPelanggan, @tunai, @status, 
                                                  @tanggal_transaksi, @tgl_pelunasan)";

                            MySqlCommand cmd = new MySqlCommand(insertQuery, conn, transaction);
                            cmd.Parameters.AddWithValue("@kode_transaksi", currentTransactionCode);
                            cmd.Parameters.AddWithValue("@kode_product", item.Barcode);
                            cmd.Parameters.AddWithValue("@nama", item.NamaProduk);
                            cmd.Parameters.AddWithValue("@qty", item.Quantity);
                            cmd.Parameters.AddWithValue("@harga", item.HargaJual);
                            cmd.Parameters.AddWithValue("@subtotal", item.Subtotal);
                            cmd.Parameters.AddWithValue("@mark_up", item.MarkUp);
                            cmd.Parameters.AddWithValue("@laba", (item.HargaJual - item.HargaBeli) * item.Quantity);
                            cmd.Parameters.AddWithValue("@payment", metodePembayaran);
                            cmd.Parameters.AddWithValue("@namaPelanggan", namaPelanggan);
                            cmd.Parameters.AddWithValue("@tunai", jumlahBayar);


                            string status;
                            if (metodePembayaran == "kredit")
                            {
                                status = jumlahBayar >= totalTransaksi ? "Lunas" : "Belum Lunas";
                            }
                            else
                            {
                                status = "Lunas";
                            }
                            cmd.Parameters.AddWithValue("@status", status);
                            cmd.Parameters.AddWithValue("@tanggal_transaksi", DateTime.Now);
                            cmd.Parameters.AddWithValue("@tgl_pelunasan", status == "Lunas" ? DateTime.Now : (DateTime?)null);

                            await cmd.ExecuteNonQueryAsync();


                            string updateStockQuery = @"UPDATE products 
                                                      SET stok = stok - @qty,
                                                          pendapatan = pendapatan + @subtotal,
                                                          laba = laba + @laba
                                                      WHERE barcode = @barcode";

                            MySqlCommand stockCmd = new MySqlCommand(updateStockQuery, conn, transaction);
                            stockCmd.Parameters.AddWithValue("@qty", item.Quantity);
                            stockCmd.Parameters.AddWithValue("@subtotal", item.Subtotal);
                            stockCmd.Parameters.AddWithValue("@laba", (item.HargaJual - item.HargaBeli) * item.Quantity);
                            stockCmd.Parameters.AddWithValue("@barcode", item.Barcode);

                            await stockCmd.ExecuteNonQueryAsync();
                        }


                        if (metodePembayaran == "kredit" && jumlahBayar < totalTransaksi)
                        {
                            string insertPembayaranQuery = @"INSERT INTO pembayaran 
                                                           (tgl_pembelian, tunai, tgl_pembayaran, kode_transaksi, jenis) 
                                                           VALUES 
                                                           (@tgl_pembelian, @tunai, @tgl_pembayaran, @kode_transaksi, @jenis)";

                            MySqlCommand pembayaranCmd = new MySqlCommand(insertPembayaranQuery, conn, transaction);
                            pembayaranCmd.Parameters.AddWithValue("@tgl_pembelian", DateTime.Now.Date);
                            pembayaranCmd.Parameters.AddWithValue("@tunai", jumlahBayar);
                            pembayaranCmd.Parameters.AddWithValue("@tgl_pembayaran", DateTime.Now);
                            pembayaranCmd.Parameters.AddWithValue("@kode_transaksi", currentTransactionCode);
                            pembayaranCmd.Parameters.AddWithValue("@jenis", "piutang");

                            await pembayaranCmd.ExecuteNonQueryAsync();
                        }

                        await transaction.CommitAsync();
                    }
                    catch (Exception)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
        }

        private void BtnCetak_Click(object sender, RoutedEventArgs e)
        {
            if (keranjangItems.Count == 0)
            {
                MessageBox.Show("Keranjang kosong!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            PrintStruk();
        }

        private void PrintStruk()
        {
            try
            {
                PrintDocument printDoc = new PrintDocument();
                printDoc.PrintPage += PrintDoc_PrintPage;
                printDoc.Print();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error printing: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PrintDoc_PrintPage(object sender, PrintPageEventArgs e)
        {
            Font headerFont = new Font("Arial", 12, System.Drawing.FontStyle.Bold);
            Font normalFont = new Font("Arial", 10);
            Font smallFont = new Font("Arial", 8);

            SolidBrush brush = new SolidBrush(Color.Black);

            float yPos = 10;
            float leftMargin = 10;
            float rightMargin = e.PageBounds.Width - 10;


            e.Graphics.DrawString("COKRO STORE", headerFont, brush, leftMargin, yPos);
            yPos += 20;
            e.Graphics.DrawString("Jl. Contoh No. 123", normalFont, brush, leftMargin, yPos);
            yPos += 15;
            e.Graphics.DrawString("Telp: 021-12345678", normalFont, brush, leftMargin, yPos);
            yPos += 25;


            e.Graphics.DrawLine(new Pen(Color.Black), leftMargin, yPos, rightMargin - 150, yPos);
            yPos += 10;


            e.Graphics.DrawString($"No. Transaksi: {currentTransactionCode}", normalFont, brush, leftMargin, yPos);
            yPos += 15;
            e.Graphics.DrawString($"Tanggal: {DateTime.Now:dd/MM/yyyy HH:mm:ss}", normalFont, brush, leftMargin, yPos);
            yPos += 15;
            e.Graphics.DrawString($"Kasir: Kasir User", normalFont, brush, leftMargin, yPos);
            yPos += 20;


            e.Graphics.DrawLine(new Pen(Color.Black), leftMargin, yPos, rightMargin - 150, yPos);
            yPos += 10;


            foreach (var item in keranjangItems)
            {
                e.Graphics.DrawString(item.NamaProduk, normalFont, brush, leftMargin, yPos);
                yPos += 15;
                e.Graphics.DrawString($"{item.Quantity} x Rp {item.HargaJual:N0}", smallFont, brush, leftMargin + 10, yPos);
                e.Graphics.DrawString($"Rp {item.Subtotal:N0}", smallFont, brush, rightMargin - 200, yPos);
                yPos += 20;
            }


            e.Graphics.DrawLine(new Pen(Color.Black), leftMargin, yPos, rightMargin - 150, yPos);
            yPos += 10;


            e.Graphics.DrawString($"Total: Rp {totalTransaksi:N0}", headerFont, brush, leftMargin, yPos);
            yPos += 20;

            if (decimal.TryParse(txtJumlahBayar.Text, out decimal jumlahBayar))
            {
                string metodePembayaran = ((ComboBoxItem)cmbMetodePembayaran.SelectedItem)?.Content?.ToString();

                e.Graphics.DrawString($"Bayar ({metodePembayaran}): Rp {jumlahBayar:N0}", normalFont, brush, leftMargin, yPos);
                yPos += 15;

                if (metodePembayaran == "Kredit")
                {
                    decimal sisaPembayaran = totalTransaksi - jumlahBayar;
                    if (sisaPembayaran > 0)
                    {
                        e.Graphics.DrawString($"Sisa: Rp {sisaPembayaran:N0}", normalFont, brush, leftMargin, yPos);
                        yPos += 15;
                        e.Graphics.DrawString($"Pelanggan: {txtNamaPelanggan.Text}", normalFont, brush, leftMargin, yPos);
                        yPos += 15;
                    }
                    else if (sisaPembayaran < 0)
                    {
                        e.Graphics.DrawString($"Kembalian: Rp {Math.Abs(sisaPembayaran):N0}", normalFont, brush, leftMargin, yPos);
                        yPos += 15;
                    }
                }
                else
                {
                    e.Graphics.DrawString($"Kembalian: Rp {jumlahBayar - totalTransaksi:N0}", normalFont, brush, leftMargin, yPos);
                    yPos += 15;
                }
            }

            yPos += 10;


            e.Graphics.DrawString("Terima kasih atas kunjungan Anda!", normalFont, brush, leftMargin, yPos);
            yPos += 15;
            e.Graphics.DrawString("Barang yang sudah dibeli tidak dapat dikembalikan", smallFont, brush, leftMargin, yPos);
        }

        private void ShowLoading()
        {
            loadingOverlay.Visibility = Visibility.Visible;
        }

        private void HideLoading()
        {
            loadingOverlay.Visibility = Visibility.Collapsed;
        }

        private void ShowSuccessMessage(string message)
        {
            txtSuccessMessage.Text = message;
            successMessage.Visibility = Visibility.Visible;
        }

        private void ResetForm()
        {
            keranjangItems.Clear();
            txtBarcode.Clear();
            txtJumlahBayar.Clear();
            txtNamaPelanggan.Clear();
            cmbMetodePembayaran.SelectedIndex = 0;
            GenerateTransactionCode();
            successMessage.Visibility = Visibility.Collapsed;
            txtBarcode.Focus();
        }


        private void BtnDashboard_Click(object sender, RoutedEventArgs e)
        {
            Views.Admin.DashboardAdmin dashboard = new Views.Admin.DashboardAdmin();
            dashboard.Show();
            this.Close();
        }

        private void Product_Click(object sender, RoutedEventArgs e)
        {
            Views.Admin.ProductsPage products = new Views.Admin.ProductsPage();
            products.Show();
            this.Close();
        }

        private void BrngMasuk_Click(object sender, RoutedEventArgs e)
        {
            Views.Admin.BarangMasukPage barangMasuk = new Views.Admin.BarangMasukPage();
            barangMasuk.Show();
            this.Close();
        }
        private void BrngKeluar_Click(object sender, RoutedEventArgs e)
        {
            Views.Admin.BarangKeluarPage barangMasuk = new Views.Admin.BarangKeluarPage();
            barangMasuk.Show();
            this.Close();
        }

        private void Supplier_Click(object sender, RoutedEventArgs e)
        {
            Views.Admin.SuppliersPage suppliers = new Views.Admin.SuppliersPage();
            suppliers.Show();
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
            TransactionMasukPage transactionMasuk = new TransactionMasukPage();
            transactionMasuk.Show();
            this.Close();
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
            if (MessageBox.Show("Apakah Anda yakin ingin logout?", "Konfirmasi", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {

                var loginPage = new LoginWindow();
                loginPage.Show();
                this.Close();
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }


    public class KeranjangItem : INotifyPropertyChanged
    {
        private int _quantity;
        private decimal _subtotal;

        public string Barcode { get; set; }
        public string NamaProduk { get; set; }
        public decimal HargaJual { get; set; }
        public decimal HargaBeli { get; set; }
        public decimal MarkUp { get; set; }
        public int SupplierId { get; set; }

        public int Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value;
                UpdateSubtotal();
                OnPropertyChanged(nameof(Quantity));
            }
        }

        public decimal Subtotal
        {
            get => _subtotal;
            private set
            {
                _subtotal = value;
                OnPropertyChanged(nameof(Subtotal));
            }
        }

        public void UpdateSubtotal()
        {
            Subtotal = HargaJual * Quantity;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ProdukItem
    {
        public int Id { get; set; }
        public string Barcode { get; set; }
        public string NamaProduk { get; set; }
        public decimal HargaJual { get; set; }
        public decimal HargaBeli { get; set; }
        public decimal MarkUp { get; set; }
        public int Stok { get; set; }
        public int SupplierId { get; set; }
        public string NamaSupplier { get; set; }
    }
}