using ClosedXML.Excel;
using KasirCokro.Models;
using KasirCokro.Views.Auth;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace KasirCokro.Views.Admin
{
    public partial class BarangMasukPage : Window
    {
        private Product selectedProduct = null;

        public BarangMasukPage()
        {
            InitializeComponent();
            LoadDataBarangMasuk();
            UpdateTotalDisplay();
            // Set focus ke textbox barcode
            txtBarcode.Focus();
        }

        private void LoadDataBarangMasuk()
        {
            var list = new List<BarangMasuk>();

            try
            {
                using (MySqlConnection conn = Helpers.DatabaseHelper.GetConnection())
                {
                    string query = @"
                        SELECT 
                            ti.id,
                            ti.tgl,
                            ti.kode_product,
                            ti.nama,
                            ti.qty,
                            ti.suplier,
                            ti.harga,
                            ti.subtotal,
                            ti.payment,
                            DATE_FORMAT(ti.tgl, '%H:%i') as waktu_format
                        FROM transaction_in ti 
                        WHERE DATE(ti.tgl) = CURDATE() 
                        ORDER BY ti.tgl DESC";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    var reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        list.Add(new BarangMasuk
                        {
                            Id = reader.GetInt32("id"),
                            Waktu = reader.GetString("waktu_format"),
                            Barcode = reader.GetString("kode_product"),
                            NamaProduk = reader.GetString("nama"),
                            Supplier = reader.GetString("suplier"),
                            Quantity = reader.GetInt32("qty"),
                            HargaBeli = reader.GetDecimal("harga"),
                            Total = reader.GetDecimal("subtotal"),
                            IsKredit = reader.GetString("payment") == "kredit"
                        });
                    }

                    dgBarangMasuk.ItemsSource = list;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error memuat data barang masuk: " + ex.Message);
            }
        }

        private void UpdateTotalDisplay()
        {
            try
            {
                using (MySqlConnection conn = Helpers.DatabaseHelper.GetConnection())
                {
                    // Total hari ini
                    string queryTotal = "SELECT COALESCE(SUM(subtotal), 0) as total FROM transaction_in WHERE DATE(tgl) = CURDATE()";
                    MySqlCommand cmdTotal = new MySqlCommand(queryTotal, conn);
                    decimal totalHariIni = Convert.ToDecimal(cmdTotal.ExecuteScalar());

                    // Total item hari ini
                    string queryItem = "SELECT COALESCE(SUM(qty), 0) as total_item FROM transaction_in WHERE DATE(tgl) = CURDATE()";
                    MySqlCommand cmdItem = new MySqlCommand(queryItem, conn);
                    int totalItem = Convert.ToInt32(cmdItem.ExecuteScalar());

                    txtTotalHariIni.Text = $"Rp {totalHariIni:N0}";
                    txtTotalItem.Text = totalItem.ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error memuat total: " + ex.Message);
            }
        }

        private void TxtBarcode_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SearchProductByBarcode();
            }
        }

        private void TxtBarcode_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Auto search ketika panjang barcode mencapai 13 karakter (standar EAN-13)
            if (txtBarcode.Text.Length >= 13)
            {
                SearchProductByBarcode();
            }
        }

        private void SearchProductByBarcode()
        {
            string barcode = txtBarcode.Text.Trim();

            if (string.IsNullOrEmpty(barcode))
            {
                MessageBox.Show("Silakan masukkan barcode terlebih dahulu.");
                return;
            }

            try
            {
                using (MySqlConnection conn = Helpers.DatabaseHelper.GetConnection())
                {
                    string query = @"
                SELECT 
                    p.id, p.barcode, p.nama_produk, p.harga_jual, p.stok, 
                    p.harga_beli, p.supplier_id,
                    s.nama_supplier
                FROM products p
                LEFT JOIN suppliers s ON p.supplier_id = s.id
                WHERE p.barcode = @barcode";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@barcode", barcode);

                    var reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        selectedProduct = new Product
                        {
                            Id = reader.GetInt32("id"),
                            Barcode = reader.GetString("barcode"),
                            NamaProduk = reader.GetString("nama_produk"),
                            HargaJual = reader.GetDecimal("harga_jual"),
                            Stok = reader.GetInt32("stok"),
                            // Fixed: Properly handle nullable decimal
                            HargaBeli = reader.GetDecimal("harga_beli"),
                            SupplierId = reader.GetInt32("supplier_id"),
                            // Create Supplier object properly
                            Supplier = new Models.Supplier
                            {
                                Id = reader.GetInt32("supplier_id"),
                                // Fixed: Properly handle nullable string
                                NamaSupplier = reader.GetString("nama_supplier")
                            }
                        };

                        ShowProductModal();
                    }
                    else
                    {
                        MessageBox.Show("Produk dengan barcode tersebut tidak ditemukan.", "Produk Tidak Ditemukan",
                                      MessageBoxButton.OK, MessageBoxImage.Warning);
                        txtBarcode.SelectAll();
                        txtBarcode.Focus();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error mencari produk: " + ex.Message);
            }
        }
        private void ShowProductModal()
        {
            if (selectedProduct != null)
            {
                if (txtModalBarcode == null || txtModalNamaProduk == null ||
                    txtModalStok == null || txtModalSupplier == null ||
                    txtModalQuantity == null || txtModalHargaBeli == null)
                {
                    MessageBox.Show("Error: Kontrol modal belum siap. Silakan coba lagi.");
                    return;
                }

                txtModalBarcode.Text = selectedProduct.Barcode ?? "";
                txtModalNamaProduk.Text = selectedProduct.NamaProduk ?? "";
                txtModalStok.Text = selectedProduct.Stok.ToString();
                txtModalSupplier.Text = selectedProduct.Supplier?.NamaSupplier ?? "";
                txtModalQuantity.Text = "1";
                txtModalHargaBeli.Text = selectedProduct.HargaBeli.ToString();

                if (chkKredit != null)
                {
                    chkKredit.IsChecked = false;
                }

                if (pnlSupplierKredit != null)
                {
                    pnlSupplierKredit.Visibility = Visibility.Collapsed;
                }

                if (txtModalSupplierKredit != null)
                {
                    txtModalSupplierKredit.Text = "";
                }

                UpdateModalTotal();

                if (modalOverlay != null)
                {
                    modalOverlay.Visibility = Visibility.Visible;
                    txtModalQuantity.Focus();
                }
            }
        }
        private bool AreModalControlsInitialized()
        {
            return txtModalBarcode != null &&
                   txtModalNamaProduk != null &&
                   txtModalStok != null &&
                   txtModalSupplier != null &&
                   txtModalQuantity != null &&
                   txtModalHargaBeli != null &&
                   txtModalTotal != null &&
                   chkKredit != null &&
                   pnlSupplierKredit != null &&
                   txtModalSupplierKredit != null &&
                   modalOverlay != null;
        }
        private void UpdateModalTotal()
        {
            try
            {
                // Pastikan kontrol UI sudah diinisialisasi
                if (txtModalQuantity == null || txtModalHargaBeli == null || txtModalTotal == null)
                {
                    return; // Keluar jika kontrol belum siap
                }

                // Pastikan text tidak kosong
                if (string.IsNullOrEmpty(txtModalQuantity.Text) || string.IsNullOrEmpty(txtModalHargaBeli.Text))
                {
                    txtModalTotal.Text = "Rp 0";
                    return;
                }

                // Parse dengan validasi
                if (int.TryParse(txtModalQuantity.Text, out int qty) &&
                    decimal.TryParse(txtModalHargaBeli.Text, out decimal harga))
                {
                    decimal total = qty * harga;
                    txtModalTotal.Text = $"Rp {total:N0}";
                }
                else
                {
                    txtModalTotal.Text = "Rp 0";
                }
            }
            catch (Exception ex)
            {
                // Log error untuk debugging
                System.Diagnostics.Debug.WriteLine($"Error in UpdateModalTotal: {ex.Message}");
                if (txtModalTotal != null)
                {
                    txtModalTotal.Text = "Rp 0";
                }
            }
        }

        private void ChkKredit_Checked(object sender, RoutedEventArgs e)
        {
            pnlSupplierKredit.Visibility = Visibility.Visible;
            txtModalSupplierKredit.Focus();
        }

        private void ChkKredit_Unchecked(object sender, RoutedEventArgs e)
        {
            pnlSupplierKredit.Visibility = Visibility.Collapsed;
            txtModalSupplierKredit.Text = "";
        }

        private void BtnModalBatal_Click(object sender, RoutedEventArgs e)
        {
            modalOverlay.Visibility = Visibility.Collapsed;
            txtBarcode.Text = "";
            txtBarcode.Focus();
        }

        private void BtnModalTambah_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateModalInput())
            {
                SaveBarangMasuk();
            }
        }

        private bool ValidateModalInput()
        {
            if (string.IsNullOrEmpty(txtModalQuantity.Text) || !int.TryParse(txtModalQuantity.Text, out int qty) || qty <= 0)
            {
                MessageBox.Show("Quantity harus berupa angka positif.");
                txtModalQuantity.Focus();
                return false;
            }

            if (string.IsNullOrEmpty(txtModalHargaBeli.Text) || !decimal.TryParse(txtModalHargaBeli.Text, out decimal harga) || harga <= 0)
            {
                MessageBox.Show("Harga beli harus berupa angka positif.");
                txtModalHargaBeli.Focus();
                return false;
            }

            if (chkKredit.IsChecked == true && string.IsNullOrEmpty(txtModalSupplierKredit.Text.Trim()))
            {
                MessageBox.Show("Nama supplier harus diisi untuk pembelian kredit.");
                txtModalSupplierKredit.Focus();
                return false;
            }

            return true;
        }

        private void SaveBarangMasuk()
        {
            try
            {
                using (MySqlConnection conn = Helpers.DatabaseHelper.GetConnection())
                {
                    // Generate kode transaksi
                    string kodeTransaksi = GenerateKodeTransaksi();

                    int qty = int.Parse(txtModalQuantity.Text);
                    decimal harga = decimal.Parse(txtModalHargaBeli.Text);
                    decimal subtotal = qty * harga;
                    string payment = chkKredit.IsChecked == true ? "kredit" : "tunai";
                    string supplier = chkKredit.IsChecked == true ? txtModalSupplierKredit.Text.Trim() : selectedProduct.Supplier.NamaSupplier;

                    // Insert ke transaction_in
                    string queryInsert = @"
                        INSERT INTO transaction_in 
                        (kode_transaksi, tgl, no_faktur, kode_product, nama, qty, suplier, payment, harga, subtotal, retur) 
                        VALUES 
                        (@kode_transaksi, @tgl, @no_faktur, @kode_product, @nama, @qty, @suplier, @payment, @harga, @subtotal, 0)";

                    MySqlCommand cmdInsert = new MySqlCommand(queryInsert, conn);
                    cmdInsert.Parameters.AddWithValue("@kode_transaksi", kodeTransaksi);
                    cmdInsert.Parameters.AddWithValue("@tgl", DateTime.Now);
                    cmdInsert.Parameters.AddWithValue("@no_faktur", kodeTransaksi); // Menggunakan kode transaksi sebagai no faktur
                    cmdInsert.Parameters.AddWithValue("@kode_product", selectedProduct.Barcode);
                    cmdInsert.Parameters.AddWithValue("@nama", selectedProduct.NamaProduk);
                    cmdInsert.Parameters.AddWithValue("@qty", qty);
                    cmdInsert.Parameters.AddWithValue("@suplier", supplier);
                    cmdInsert.Parameters.AddWithValue("@payment", payment);
                    cmdInsert.Parameters.AddWithValue("@harga", harga);
                    cmdInsert.Parameters.AddWithValue("@subtotal", subtotal);

                    cmdInsert.ExecuteNonQuery();

                    // Update stok produk
                    string queryUpdateStok = "UPDATE products SET stok = stok + @qty WHERE barcode = @barcode";
                    MySqlCommand cmdUpdateStok = new MySqlCommand(queryUpdateStok, conn);
                    cmdUpdateStok.Parameters.AddWithValue("@qty", qty);
                    cmdUpdateStok.Parameters.AddWithValue("@barcode", selectedProduct.Barcode);
                    cmdUpdateStok.ExecuteNonQuery();

                    MessageBox.Show("Barang masuk berhasil ditambahkan!", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Refresh data
                    LoadDataBarangMasuk();
                    UpdateTotalDisplay();

                    // Close modal dan reset
                    modalOverlay.Visibility = Visibility.Collapsed;
                    txtBarcode.Text = "";
                    txtBarcode.Focus();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error menyimpan barang masuk: " + ex.Message);
            }
        }

        private string GenerateKodeTransaksi()
        {
            return "TI" + DateTime.Now.ToString("yyyyMMddHHmmss");
        }

        // Tambahkan method ini ke dalam class BarangMasukPage

        private void BtnCariManual_Click(object sender, RoutedEventArgs e)
        {
            ShowManualInputModal();
        }

        private void ShowManualInputModal()
        {
            // Reset form
            txtManualBarcode.Text = "";
            txtManualNamaProduk.Text = "";
            txtManualStok.Text = "";
            txtManualSupplier.Text = "";
            txtManualPendapatan.Text = "";
            txtManualHargaJual.Text = "";
            txtManualPercntage.Text = "0%";
            txtManualLaba.Text = "0";
            txtManualStokAwal.Text = "0";
            txtManualHarta.Text = "0";
            txtManualMarkUp.Text = "0";
            txtManualQuantity.Text = "1";
            txtManualHargaBeli.Text = "";
            chkManualKredit.IsChecked = false;
            pnlManualSupplierKredit.Visibility = Visibility.Collapsed;
            txtManualSupplierKredit.Text = "";

            UpdateManualModalTotal();
            LoadSuppliers();

            modalManualOverlay.Visibility = Visibility.Visible;
            txtManualBarcode.Focus();
        }
        private void CalculatePercentage()
        {
            try
            {
                if (txtManualHargaBeli == null || txtManualHargaJual == null)
                {
                    return; 
                }

                if (decimal.TryParse(txtManualHargaBeli.Text, out decimal hargaBeli) &&
                    decimal.TryParse(txtManualHargaJual.Text, out decimal hargaJual) &&
                    hargaBeli > 0)
                {
                    // Calculate percentage markup: ((Harga Jual - Harga Beli) / Harga Beli) * 100
                    decimal percentage = ((hargaJual - hargaBeli) / hargaBeli) * 100;
                    txtManualPercntage.Text = Math.Round(percentage, 2).ToString() + "%";
                }
                else
                {
                    txtManualPercntage.Text = "0%";
                }
            }
            catch
            {
                txtManualPercntage.Text = "0%";
            }
        }

        private void UpdateManualModalTotal()
        {
            try
            {
                // Pastikan kontrol UI sudah diinisialisasi
                if (txtManualQuantity == null || txtManualHargaBeli == null || txtManualTotal == null)
                {
                    return;
                }

                // Pastikan text tidak kosong
                if (string.IsNullOrEmpty(txtManualQuantity.Text) || string.IsNullOrEmpty(txtManualHargaBeli.Text))
                {
                    txtManualTotal.Text = "Rp 0";
                    return;
                }

                // Parse dengan validasi
                if (int.TryParse(txtManualQuantity.Text, out int qty) &&
                    decimal.TryParse(txtManualHargaBeli.Text, out decimal harga))
                {
                    decimal total = qty * harga;
                    txtManualTotal.Text = $"Rp {total:N0}";
                }
                else
                {
                    txtManualTotal.Text = "Rp 0";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in UpdateManualModalTotal: {ex.Message}");
                if (txtManualTotal != null)
                {
                    txtManualTotal.Text = "Rp 0";
                }
            }
        }

        private void ChkManualKredit_Checked(object sender, RoutedEventArgs e)
        {
            pnlManualSupplierKredit.Visibility = Visibility.Visible;
            txtManualSupplierKredit.Focus();
        }

        private void ChkManualKredit_Unchecked(object sender, RoutedEventArgs e)
        {
            pnlManualSupplierKredit.Visibility = Visibility.Collapsed;
            txtManualSupplierKredit.Text = "";
        }

        private void BtnManualBatal_Click(object sender, RoutedEventArgs e)
        {
            modalManualOverlay.Visibility = Visibility.Collapsed;
            txtBarcode.Focus();
        }

        private void BtnManualTambah_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateManualModalInput())
            {
                SaveManualBarangMasuk();
            }
        }

        private bool ValidateManualModalInput()
        {
            // Validasi Barcode
            if (string.IsNullOrEmpty(txtManualBarcode.Text.Trim()))
            {
                MessageBox.Show("Barcode harus diisi.");
                txtManualBarcode.Focus();
                return false;
            }

            // Validasi Nama Produk
            if (string.IsNullOrEmpty(txtManualNamaProduk.Text.Trim()))
            {
                MessageBox.Show("Nama produk harus diisi.");
                txtManualNamaProduk.Focus();
                return false;
            }


            // Validasi Stok
            if (string.IsNullOrEmpty(txtManualStok.Text) || !int.TryParse(txtManualStok.Text, out int stok) || stok < 0)
            {
                MessageBox.Show("Stok harus berupa angka positif atau nol.");
                txtManualStok.Focus();
                return false;
            }

            if (string.IsNullOrEmpty(txtManualStokAwal.Text) || !int.TryParse(txtManualStokAwal.Text, out int stokAwal) || stokAwal < 0)
            {
                MessageBox.Show("Stok Aawl harus berupa angka positif atau nol.");
                txtManualStok.Focus();
                return false;
            }

            // Validasi Supplier
            if (txtManualSupplier.SelectedValue == null)
            {
                MessageBox.Show("Supplier harus diisi.");
                txtManualSupplier.Focus();
                return false;
            }

            // Validasi Quantity
            if (string.IsNullOrEmpty(txtManualQuantity.Text) || !int.TryParse(txtManualQuantity.Text, out int qty) || qty <= 0)
            {
                MessageBox.Show("Quantity harus berupa angka positif.");
                txtManualQuantity.Focus();
                return false;
            }

            // Validasi Harga Beli
            if (string.IsNullOrEmpty(txtManualHargaBeli.Text) || !decimal.TryParse(txtManualHargaBeli.Text, out decimal harga) || harga <= 0)
            {
                MessageBox.Show("Harga beli harus berupa angka positif.");
                txtManualHargaBeli.Focus();
                return false;
            }

            if (string.IsNullOrEmpty(txtManualHargaJual.Text) || !decimal.TryParse(txtManualHargaJual.Text, out decimal hargaJual) || hargaJual <= 0)
            {
                MessageBox.Show("Harga jual harus berupa angka positif.");
                txtManualHargaBeli.Focus();
                return false;
            }

            if (string.IsNullOrEmpty(txtManualLaba.Text) || !decimal.TryParse(txtManualLaba.Text, out decimal laba) || laba < 0)
            {
                MessageBox.Show("Laba harus berupa angka positif atau nol.");
                txtManualHargaBeli.Focus();
                return false;
            }

            if (string.IsNullOrEmpty(txtManualMarkUp.Text) || !decimal.TryParse(txtManualHargaJual.Text, out decimal markUp) || markUp < 0)
            {
                MessageBox.Show("Markup harus berupa angka positif atau nol.");
                txtManualHargaBeli.Focus();
                return false;
            }
            if (string.IsNullOrEmpty(txtManualPendapatan.Text) || !decimal.TryParse(txtManualPendapatan.Text, out decimal pendapatan) || pendapatan < 0)
            {
                MessageBox.Show("Pendapatan harus berupa angka positif atau nol.");
                txtManualHargaBeli.Focus();
                return false;
            }

            if (string.IsNullOrEmpty(txtManualHarta.Text) || !decimal.TryParse(txtManualHarta.Text, out decimal harta) || harta < 0)
            {
                MessageBox.Show("Harta harus berupa angka positif atau nol.");
                txtManualHargaBeli.Focus();
                return false;
            }



            // Validasi Supplier Kredit
            if (chkManualKredit.IsChecked == true && string.IsNullOrEmpty(txtManualSupplierKredit.Text.Trim()))
            {
                MessageBox.Show("Nama supplier harus diisi untuk pembelian kredit.");
                txtManualSupplierKredit.Focus();
                return false;
            }

            return true;
        }

        private void LoadSuppliers()
        {
            try
            {
                List<Supplier> suppliers = new List<Supplier>();

                using (MySqlConnection conn = Helpers.DatabaseHelper.GetConnection())
                {
                    string query = "SELECT id, nama_supplier FROM suppliers ORDER BY nama_supplier";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    MySqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        suppliers.Add(new Supplier
                        {
                            Id = reader.GetInt32("id"),
                            NamaSupplier = reader.GetString("nama_supplier")
                        });
                    }
                }

                txtManualSupplier.ItemsSource = suppliers;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat data supplier: " + ex.Message);
            }
        }
        private void SaveManualBarangMasuk()
        {
            try
            {
                using (MySqlConnection conn = Helpers.DatabaseHelper.GetConnection())
                {
                    // Cek apakah produk sudah ada
                    string checkQuery = "SELECT COUNT(*) FROM products WHERE barcode = @barcode";
                    MySqlCommand checkCmd = new MySqlCommand(checkQuery, conn);
                    checkCmd.Parameters.AddWithValue("@barcode", txtManualBarcode.Text.Trim());

                    string persentase = txtManualPercntage.Text.Replace("%", "").Trim();
                    if (!decimal.TryParse(persentase, out decimal percentageValue))
                    {
                        persentase = "0";
                    }
                    int productExists = Convert.ToInt32(checkCmd.ExecuteScalar());
                    int supplierId = (int)txtManualSupplier.SelectedValue;
                    if (productExists == 0)
                    {
                        // Insert produk baru jika belum ada
                        string insertProductQuery = @"
                    INSERT INTO products 
                    (barcode, nama_produk, harga_jual, stok, stok_awal, supplier_id, harga_beli, mark_up, pendapatan, laba, harta, persentase, created_at, updated_at) 
                    VALUES 
                    (@barcode, @nama_produk, @harga_jual, @stok, @stok_awal, @supplier_id, @harga_beli, @markup, @pendapatan, @laba, @harta, @pesentase, NOW(), NOW())";

                        MySqlCommand insertProductCmd = new MySqlCommand(insertProductQuery, conn);
                        insertProductCmd.Parameters.AddWithValue("@barcode", txtManualBarcode.Text.Trim());
                        insertProductCmd.Parameters.AddWithValue("@nama_produk", txtManualNamaProduk.Text.Trim());
                        insertProductCmd.Parameters.AddWithValue("@harga_jual", decimal.Parse(txtManualHargaJual.Text));
                        insertProductCmd.Parameters.AddWithValue("@supplier_id", supplierId);
                        insertProductCmd.Parameters.AddWithValue("@stok", int.Parse(txtManualStok.Text));
                        insertProductCmd.Parameters.AddWithValue("@stok_awal", int.Parse(txtManualStokAwal.Text));
                        insertProductCmd.Parameters.AddWithValue("@harga_beli", decimal.Parse(txtManualHargaBeli.Text));
                        insertProductCmd.Parameters.AddWithValue("@markup", decimal.Parse(txtManualMarkUp.Text));
                        insertProductCmd.Parameters.AddWithValue("@pendapatan", decimal.Parse(txtManualPendapatan.Text));
                        insertProductCmd.Parameters.AddWithValue("@laba", decimal.Parse(txtManualLaba.Text));
                        insertProductCmd.Parameters.AddWithValue("@harta", decimal.Parse(txtManualHarta.Text));
                        insertProductCmd.Parameters.AddWithValue("@pesentase", persentase); // Convert percentage to decimal

                        insertProductCmd.ExecuteNonQuery();
                    }

                    // Generate kode transaksi
                    string kodeTransaksi = GenerateKodeTransaksi();

                    int qty = int.Parse(txtManualQuantity.Text);
                    decimal harga = decimal.Parse(txtManualHargaBeli.Text);
                    decimal subtotal = qty * harga;
                    string payment = chkManualKredit.IsChecked == true ? "kredit" : "tunai";
                    string supplier = chkManualKredit.IsChecked == true ? txtManualSupplierKredit.Text.Trim() : txtManualSupplier.Text.Trim();

                    // Insert ke transaction_in
                    string queryInsert = @"
                INSERT INTO transaction_in 
                (kode_transaksi, tgl, no_faktur, kode_product, nama, qty, suplier, payment, harga, subtotal, retur) 
                VALUES 
                (@kode_transaksi, @tgl, @no_faktur, @kode_product, @nama, @qty, @suplier, @payment, @harga, @subtotal, 0)";

                    MySqlCommand cmdInsert = new MySqlCommand(queryInsert, conn);
                    cmdInsert.Parameters.AddWithValue("@kode_transaksi", kodeTransaksi);
                    cmdInsert.Parameters.AddWithValue("@tgl", DateTime.Now);
                    cmdInsert.Parameters.AddWithValue("@no_faktur", kodeTransaksi);
                    cmdInsert.Parameters.AddWithValue("@kode_product", txtManualBarcode.Text.Trim());
                    cmdInsert.Parameters.AddWithValue("@nama", txtManualNamaProduk.Text.Trim());
                    cmdInsert.Parameters.AddWithValue("@qty", qty);
                    cmdInsert.Parameters.AddWithValue("@suplier", supplier);
                    cmdInsert.Parameters.AddWithValue("@payment", payment);
                    cmdInsert.Parameters.AddWithValue("@harga", harga);
                    cmdInsert.Parameters.AddWithValue("@subtotal", subtotal);

                    cmdInsert.ExecuteNonQuery();

                    // Update stok produk
                    string queryUpdateStok = "UPDATE products SET stok = stok + @qty WHERE barcode = @barcode";
                    MySqlCommand cmdUpdateStok = new MySqlCommand(queryUpdateStok, conn);
                    cmdUpdateStok.Parameters.AddWithValue("@qty", qty);
                    cmdUpdateStok.Parameters.AddWithValue("@barcode", txtManualBarcode.Text.Trim());
                    cmdUpdateStok.ExecuteNonQuery();

                    MessageBox.Show("Barang masuk berhasil ditambahkan!", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Refresh data
                    LoadDataBarangMasuk();
                    UpdateTotalDisplay();

                    // Close modal dan reset
                    modalManualOverlay.Visibility = Visibility.Collapsed;
                    txtBarcode.Focus();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error menyimpan barang masuk: " + ex.Message);
            }
        }

        // Event handlers untuk manual input
        private void TxtManualQuantity_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateManualModalTotal();
        }

        private void TxtManualHargaBeli_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateManualModalTotal();
            CalculatePercentage();
        }

        private void TxtManualHargaJual_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalculatePercentage();
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            txtBarcode.Text = "";
            txtBarcode.Focus();
        }

        private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = $"Barang_Masuk_{DateTime.Now:yyyyMMdd}";
            dlg.DefaultExt = ".xlsx";
            dlg.Filter = "Excel Files (*.xlsx)|*.xlsx";

            if (dlg.ShowDialog() == true)
            {
                ExportToExcel(dlg.FileName);
            }
        }

        private void ExportToExcel(string filePath)
        {
            try
            {
                var barangMasuk = dgBarangMasuk.Items.Cast<BarangMasuk>().ToList();

                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Barang Masuk");

                    // Header
                    worksheet.Cell(1, 1).Value = $"Laporan Barang Masuk - {DateTime.Now:dd/MM/yyyy}";
                    worksheet.Cell(1, 1).Style.Font.Bold = true;
                    worksheet.Cell(1, 1).Style.Font.FontSize = 16;

                    // Column headers
                    worksheet.Cell(3, 1).Value = "No.";
                    worksheet.Cell(3, 2).Value = "Waktu";
                    worksheet.Cell(3, 3).Value = "Barcode";
                    worksheet.Cell(3, 4).Value = "Nama Produk";
                    worksheet.Cell(3, 5).Value = "Supplier";
                    worksheet.Cell(3, 6).Value = "Qty";
                    worksheet.Cell(3, 7).Value = "Harga Beli";
                    worksheet.Cell(3, 8).Value = "Total";
                    worksheet.Cell(3, 9).Value = "Status";

                    // Style header
                    var headerRange = worksheet.Range(3, 1, 3, 9);
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                    headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                    // Data
                    int row = 4;
                    for (int i = 0; i < barangMasuk.Count; i++)
                    {
                        var item = barangMasuk[i];
                        worksheet.Cell(row + i, 1).Value = i + 1;
                        worksheet.Cell(row + i, 2).Value = item.Waktu;
                        worksheet.Cell(row + i, 3).Value = item.Barcode;
                        worksheet.Cell(row + i, 4).Value = item.NamaProduk;
                        worksheet.Cell(row + i, 5).Value = item.Supplier;
                        worksheet.Cell(row + i, 6).Value = item.Quantity;
                        worksheet.Cell(row + i, 7).Value = item.HargaBeli;
                        worksheet.Cell(row + i, 8).Value = item.Total;
                        worksheet.Cell(row + i, 9).Value = item.IsKredit ? "Kredit" : "Tunai";
                    }

                    // Format currency columns
                    var hargaBeliRange = worksheet.Range(4, 7, row + barangMasuk.Count - 1, 7);
                    hargaBeliRange.Style.NumberFormat.Format = "#,##0";

                    var totalRange = worksheet.Range(4, 8, row + barangMasuk.Count - 1, 8);
                    totalRange.Style.NumberFormat.Format = "#,##0";

                    // Auto fit columns
                    worksheet.ColumnsUsed().AdjustToContents();

                    // Add borders to data
                    var dataRange = worksheet.Range(3, 1, row + barangMasuk.Count - 1, 9);
                    dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                    workbook.SaveAs(filePath);
                }

                MessageBox.Show("Berhasil mengekspor laporan barang masuk ke Excel!", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal mengekspor ke Excel: " + ex.Message);
            }
        }
        // Event handlers untuk quantity dan harga beli textbox
        private void TxtModalQuantity_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Pastikan semua kontrol sudah siap sebelum update
            if (IsLoaded && txtModalHargaBeli != null && txtModalTotal != null)
            {
                UpdateModalTotal();
            }
        }
        private void TxtModalHargaBeli_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Pastikan semua kontrol sudah siap sebelum update
            if (IsLoaded && txtModalQuantity != null && txtModalTotal != null)
            {
                UpdateModalTotal();
            }
        }

        // Navigation methods
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
            // Navigate to Transaction Keluar page
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
            var result = MessageBox.Show("Yakin ingin logout?", "Konfirmasi",
                                 MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                LoginWindow login = new LoginWindow();
                login.Show();
                this.Close();
            }
        }
    }

    // Model untuk data barang masuk
    public class BarangMasuk
    {
        public int Id { get; set; }
        public string Waktu { get; set; }
        public string Barcode { get; set; }
        public string NamaProduk { get; set; }
        public string Supplier { get; set; }
        public int Quantity { get; set; }
        public decimal HargaBeli { get; set; }
        public decimal Total { get; set; }
        public bool IsKredit { get; set; }
    }

    // Model untuk Supplier (jika belum ada)
    public class Supplier
    {
        public int Id { get; set; }
        public string NamaSupplier { get; set; }
        public string Kontak { get; set; }
        public string Alamat { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}