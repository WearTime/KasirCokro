using KasirCokro.Models;
using KasirCokro.Views.Auth;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Windows;

namespace KasirCokro.Views.Admin
{
	public partial class ProductForm : Window
	{
		public bool IsSaved { get; private set; }
		private readonly Product _product;

		public ProductForm(Product product)
		{
			InitializeComponent();
			_product = product;

			LoadSuppliers();

			txtHargaBeli.TextChanged += OnPriceChanged;
			txtHargaJual.TextChanged += OnPriceChanged;
			txtMarkUp.TextChanged += OnPriceChanged;

			if (product != null)
			{
				txtBarcode.Text = product.Barcode ?? "";
				txtNamaProduk.Text = product.NamaProduk ?? "";
				txtHargaBeli.Text = product.HargaBeli?.ToString() ?? "0";
				txtHargaJual.Text = product.HargaJual.ToString();
				txtLaba.Text = product.Laba.ToString() ?? "0";
				txtStok.Text = product.Stok.ToString();
				txtStokAwal.Text = product.StokAwal.ToString() ?? "0";
				txtMarkUp.Text = product.MarkUp?.ToString() ?? "0";
				txtPersentase.Text = product.Persentase ?? "";
				txtHarta.Text = product.Harta?.ToString() ?? "0";

				if (product.SupplierId > 0)
				{
					cmbSupplier.SelectedValue = product.SupplierId;
				}
			}

			CalculatePercentage();
		}

		private void OnPriceChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
		{
			CalculatePercentage();
		}

		private void CalculatePercentage()
		{
			try
			{
				if (decimal.TryParse(txtHargaBeli.Text, out decimal hargaBeli) &&
	decimal.TryParse(txtHargaJual.Text, out decimal hargaJual) &&
	hargaBeli > 0)
				{
					decimal percentage = ((hargaJual - hargaBeli) / hargaBeli) * 100;
					txtPersentase.Text = Math.Round(percentage, 2).ToString() + "%";
				}
				else
				{
					txtPersentase.Text = "0%";
				}
			}
			catch
			{
				txtPersentase.Text = "0%";
			}
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

				cmbSupplier.ItemsSource = suppliers;
			}
			catch (Exception ex)
			{
				MessageBox.Show("Gagal memuat data supplier: " + ex.Message);
			}
		}

		private void BtnBatal_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

		private void BtnSimpan_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				string barcode = txtBarcode.Text.Trim();
				string namaProduk = txtNamaProduk.Text.Trim();

				if (!Helpers.ValidationHelper.IsValidBarcode(barcode))
				{
					MessageBox.Show("Barcode tidak valid! Gunakan format alphanumerik 6-50 karakter.");
					txtBarcode.Focus();
					return;
				}

				if (!Helpers.ValidationHelper.IsValidProductName(namaProduk))
				{
					MessageBox.Show("Nama produk tidak valid! Minimum 2 karakter, maksimum 100 karakter.");
					txtNamaProduk.Focus();
					return;
				}

				if (cmbSupplier.SelectedValue == null)
				{
					MessageBox.Show("Supplier harus dipilih!");
					cmbSupplier.Focus();
					return;
				}

				if (!Helpers.ValidationHelper.TryParseDecimal(txtHargaBeli.Text, out decimal hargaBeli))
				{
					MessageBox.Show("Harga beli tidak valid!");
					txtHargaBeli.Focus();
					return;
				}

				if (!Helpers.ValidationHelper.TryParseDecimal(txtHargaJual.Text, out decimal hargaJual))
				{
					MessageBox.Show("Harga jual tidak valid!");
					txtHargaJual.Focus();
					return;
				}

				if (!Helpers.ValidationHelper.TryParseDecimal(txtLaba.Text, out decimal laba))
				{
					MessageBox.Show("Laba tidak valid!");
					txtLaba.Focus();
					return;
				}

				decimal pendapatan = hargaJual - hargaBeli;

				if (!Helpers.ValidationHelper.TryParseInt(txtStok.Text, out int stok))
				{
					MessageBox.Show("Stok tidak valid!");
					txtStok.Focus();
					return;
				}

				if (!Helpers.ValidationHelper.TryParseInt(txtStokAwal.Text, out int stokAwal))
				{
					MessageBox.Show("Stok awal tidak valid!");
					txtStokAwal.Focus();
					return;
				}

				if (!Helpers.ValidationHelper.TryParseDecimal(txtMarkUp.Text, out decimal markUp))
				{
					MessageBox.Show("Mark up tidak valid!");
					txtMarkUp.Focus();
					return;
				}

				if (!Helpers.ValidationHelper.TryParseDecimal(txtHarta.Text, out decimal harta))
				{
					MessageBox.Show("Harta tidak valid!");
					txtHarta.Focus();
					return;
				}

				string persentase = txtPersentase.Text.Replace("%", "").Trim();
				if (!decimal.TryParse(persentase, out decimal percentageValue))
				{
					persentase = "0";
				}

				if (hargaJual <= hargaBeli)
				{
					var result = MessageBox.Show("Harga jual lebih rendah dari harga beli. Yakin ingin melanjutkan?",
												"Konfirmasi", MessageBoxButton.YesNo, MessageBoxImage.Warning);
					if (result == MessageBoxResult.No)
						return;
				}

				int supplierId = (int)cmbSupplier.SelectedValue;

				using (MySqlConnection conn = Helpers.DatabaseHelper.GetConnection())
				{
					if (_product == null || _product.Id == 0)
					{
						string insertQuery = @"INSERT INTO products 
                                     (barcode, nama_produk, supplier_id, harga_beli, harga_jual, 
                                      stok, stok_awal, mark_up, persentase, harta, 
                                      pendapatan, laba) 
                                     VALUES 
                                     (@barcode, @nama_produk, @supplier_id, @harga_beli, @harga_jual, 
                                      @stok, @stok_awal, @mark_up, @persentase, @harta, 
                                      @pendapatan, @laba)";

						MySqlCommand cmd = new MySqlCommand(insertQuery, conn);
						cmd.Parameters.AddWithValue("@barcode", barcode);
						cmd.Parameters.AddWithValue("@nama_produk", namaProduk);
						cmd.Parameters.AddWithValue("@supplier_id", supplierId);
						cmd.Parameters.AddWithValue("@harga_beli", hargaBeli);
						cmd.Parameters.AddWithValue("@harga_jual", hargaJual);
						cmd.Parameters.AddWithValue("@stok", stok);
						cmd.Parameters.AddWithValue("@stok_awal", stokAwal);
						cmd.Parameters.AddWithValue("@mark_up", markUp);
						cmd.Parameters.AddWithValue("@persentase", persentase);
						cmd.Parameters.AddWithValue("@harta", harta);
						cmd.Parameters.AddWithValue("@pendapatan", pendapatan);
						cmd.Parameters.AddWithValue("@laba", laba);

						cmd.ExecuteNonQuery();
					}
					else
					{
						string updateQuery = @"UPDATE products SET 
                                     barcode = @barcode, 
                                     nama_produk = @nama_produk, 
                                     supplier_id = @supplier_id, 
                                     harga_beli = @harga_beli, 
                                     harga_jual = @harga_jual, 
                                     stok = @stok, 
                                     stok_awal = @stok_awal, 
                                     mark_up = @mark_up, 
                                     persentase = @persentase, 
                                     harta = @harta,
                                     pendapatan = @pendapatan, 
                                     laba = @laba
                                     WHERE id = @id";

						MySqlCommand cmd = new MySqlCommand(updateQuery, conn);
						cmd.Parameters.AddWithValue("@id", _product.Id);
						cmd.Parameters.AddWithValue("@barcode", barcode);
						cmd.Parameters.AddWithValue("@nama_produk", namaProduk);
						cmd.Parameters.AddWithValue("@supplier_id", supplierId);
						cmd.Parameters.AddWithValue("@harga_beli", hargaBeli);
						cmd.Parameters.AddWithValue("@harga_jual", hargaJual);
						cmd.Parameters.AddWithValue("@stok", stok);
						cmd.Parameters.AddWithValue("@stok_awal", stokAwal);
						cmd.Parameters.AddWithValue("@mark_up", markUp);
						cmd.Parameters.AddWithValue("@persentase", persentase);
						cmd.Parameters.AddWithValue("@harta", harta);
						cmd.Parameters.AddWithValue("@pendapatan", pendapatan);
						cmd.Parameters.AddWithValue("@laba", laba);

						cmd.ExecuteNonQuery();
					}
				}

				MessageBox.Show("Data produk berhasil disimpan!");
				IsSaved = true;
				this.Close();
			}
			catch (MySqlException ex)
			{
				MessageBox.Show($"Database error: {ex.Message}\nError Code: {ex.Number}");
			}
			catch (FormatException)
			{
				MessageBox.Show("Format angka tidak valid. Pastikan semua field numerik diisi dengan benar.");
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Gagal menyimpan data produk: {ex.Message}");
			}
		}
		private void BtnKembali_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}
	}
}