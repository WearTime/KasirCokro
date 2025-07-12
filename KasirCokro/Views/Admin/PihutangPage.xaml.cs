using KasirCokro.Views.Auth;
using Microsoft.Win32;
using MySql.Data.MySqlClient;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Windows;
using System.Windows.Controls;
using Button = System.Windows.Controls.Button;

namespace KasirCokro.Views.Admin
{
	public partial class PihutangPage : Window
	{
		private List<PihutangData> originalData = new List<PihutangData>();
		private List<PihutangData> filteredData = new List<PihutangData>();

		public PihutangPage()
		{
			InitializeComponent();
			LoadData();
			dpTanggalFrom.SelectedDate = DateTime.Now;
			txtPeriode.Text = $"Periode: {DateTime.Now:dd/MM/yyyy}";
		}

		private async void LoadData()
		{
			try
			{
				ShowLoading(true);
				await LoadPihutangData();
				CalculateTotals();
				ShowLoading(false);
			}
			catch (Exception ex)
			{
				ShowLoading(false);
				MessageBox.Show($"Error loading data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private async Task LoadPihutangData()
		{
			string query = @"
                SELECT 
                    t.kode_transaksi,
                    t.tanggal_transaksi,
                    t.namaPelanggan,
                    t.qty,
                    t.harga,
                    t.subtotal,
                    t.Tunai,
                    (t.subtotal - t.Tunai) as sisa_hutang,
                    t.nama as nama_produk,
                    t.kode_product
                FROM transactions t
                WHERE t.payment = 'kredit' 
                    AND (t.subtotal - t.Tunai) > 0
                ORDER BY t.tanggal_transaksi DESC";

			originalData.Clear();

			using (MySqlConnection conn = Helpers.DatabaseHelper.GetConnection())
			{
				using (MySqlCommand cmd = new MySqlCommand(query, conn))
				{
					using (MySqlDataReader reader = cmd.ExecuteReader())
					{
						while (await reader.ReadAsync())
						{
							originalData.Add(new PihutangData
							{
								Barcode = reader["kode_transaksi"].ToString(),
								Waktu = Convert.ToDateTime(reader["tanggal_transaksi"]).ToString("dd/MM/yyyy HH:mm"),
								NamaProduk = reader["namaPelanggan"].ToString(),
								Quantity = Convert.ToInt32(reader["qty"]),
								HargaBeli = Convert.ToDecimal(reader["harga"]),
								Total = Convert.ToDecimal(reader["sisa_hutang"]),
								NamaBarang = reader["nama_produk"].ToString(),
								KodeBarang = reader["kode_product"].ToString(),
								TunaiDibayar = Convert.ToDecimal(reader["Tunai"]),
								SubtotalAsli = Convert.ToDecimal(reader["subtotal"])
							});
						}
					}
				}
			}

			filteredData = new List<PihutangData>(originalData);
			dgKas.ItemsSource = filteredData;
		}

		private void CalculateTotals()
		{
			int totalQty = filteredData.Sum(x => x.Quantity);
			decimal totalPihutang = filteredData.Sum(x => x.Total);
			int totalItem = filteredData.Count;

			txtQtyPihutang.Text = totalQty.ToString();
			txtTotalPihutang.Text = $"Rp {totalPihutang:N0}";
			txtTotalItemPihutang.Text = totalItem.ToString();
		}

		private void ShowLoading(bool isLoading)
		{
			loadingOverlay.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
		}

		private void ShowSuccessMessage(string message)
		{
			txtSuccessMessage.Text = message;
			successMessage.Visibility = Visibility.Visible;

			var timer = new System.Windows.Threading.DispatcherTimer();
			timer.Interval = TimeSpan.FromSeconds(2);
			timer.Tick += (s, e) =>
			{
				successMessage.Visibility = Visibility.Collapsed;
				timer.Stop();
			};
			timer.Start();
		}

		private void BtnDetail_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Button btn = sender as Button;
				PihutangData selectedItem = btn.DataContext as PihutangData;

				if (selectedItem != null)
				{
					var detailWindow = new DetailTransaksiWindow(selectedItem);
					detailWindow.Owner = this;
					detailWindow.ShowDialog();

					LoadData();
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error opening detail: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void BtnFilter_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (dpTanggalFrom.SelectedDate.HasValue)
				{
					DateTime selectedDate = dpTanggalFrom.SelectedDate.Value;

					filteredData = originalData.Where(x =>
						DateTime.ParseExact(x.Waktu, "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture).Date == selectedDate.Date
					).ToList();

					dgKas.ItemsSource = filteredData;
					CalculateTotals();
					txtPeriode.Text = $"Periode: {selectedDate:dd/MM/yyyy}";
				}
				else
				{
					MessageBox.Show("Silakan pilih tanggal terlebih dahulu!", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error filtering data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void BtnRefresh_Click(object sender, RoutedEventArgs e)
		{
			LoadData();
			dpTanggalFrom.SelectedDate = DateTime.Now;
			txtPeriode.Text = $"Periode: {DateTime.Now:dd/MM/yyyy}";
		}

		private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
		{
			if (dpTanggalFrom.SelectedDate.HasValue)
			{
				BtnFilter_Click(sender, e);
			}
		}

		private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				SaveFileDialog saveDialog = new SaveFileDialog
				{
					Filter = "Excel Files|*.xlsx",
					Title = "Export Data Pihutang",
					FileName = $"Data_Pihutang_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
				};

				if (saveDialog.ShowDialog() == true)
				{
					ExportToExcel(saveDialog.FileName);
					ShowSuccessMessage($"Data berhasil di-export ke:\n{saveDialog.FileName}");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error exporting to Excel: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void ExportToExcel(string filePath)
		{
			using (var package = new ExcelPackage())
			{
				var worksheet = package.Workbook.Worksheets.Add("Data Pihutang");

				worksheet.Cells[1, 1].Value = "Kode Transaksi";
				worksheet.Cells[1, 2].Value = "Tanggal";
				worksheet.Cells[1, 3].Value = "Nama Pelanggan";
				worksheet.Cells[1, 4].Value = "Nama Produk";
				worksheet.Cells[1, 5].Value = "Qty";
				worksheet.Cells[1, 6].Value = "Harga";
				worksheet.Cells[1, 7].Value = "Subtotal";
				worksheet.Cells[1, 8].Value = "Tunai Dibayar";
				worksheet.Cells[1, 9].Value = "Sisa Pihutang";

				int row = 2;
				foreach (var item in filteredData)
				{
					worksheet.Cells[row, 1].Value = item.Barcode;
					worksheet.Cells[row, 2].Value = item.Waktu;
					worksheet.Cells[row, 3].Value = item.NamaProduk;
					worksheet.Cells[row, 4].Value = item.NamaBarang;
					worksheet.Cells[row, 5].Value = item.Quantity;
					worksheet.Cells[row, 6].Value = item.HargaBeli;
					worksheet.Cells[row, 7].Value = item.SubtotalAsli;
					worksheet.Cells[row, 8].Value = item.TunaiDibayar;
					worksheet.Cells[row, 9].Value = item.Total;
					row++;
				}

				using (var range = worksheet.Cells[1, 1, 1, 9])
				{
					range.Style.Font.Bold = true;
					range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
					range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
				}

				worksheet.Cells.AutoFitColumns();
				package.SaveAs(new FileInfo(filePath));
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
			var product = new ProductsPage();
			product.Show();
			this.Close();
		}

		private void Supplier_Click(object sender, RoutedEventArgs e)
		{
			var supplier = new SuppliersPage();
			supplier.Show();
			this.Close();
		}

		private void BrngMasuk_Click(object sender, RoutedEventArgs e)
		{
			var brngMasuk = new BarangMasukPage();
			brngMasuk.Show();
			this.Close();
		}

		private void BrngKeluar_Click(object sender, RoutedEventArgs e)
		{
			var brngKeluar = new BarangKeluarPage();
			brngKeluar.Show();
			this.Close();
		}

		private void Kas_Click(object sender, RoutedEventArgs e)
		{
			var kas = new KasPage();
			kas.Show();
			this.Close();
		}

		private void TransactionMasuk_Click(object sender, RoutedEventArgs e)
		{
			var transMasuk = new TransactionMasukPage();
			transMasuk.Show();
			this.Close();
		}

		private void TransactionKeluar_Click(object sender, RoutedEventArgs e)
		{
			var transKeluar = new TransactionKeluarPage();
			transKeluar.Show();
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
				var login = new LoginWindow();
				login.Show();
				this.Close();
			}
		}
	}

	public class PihutangData : INotifyPropertyChanged
	{
		public string Barcode { get; set; }
		public string Waktu { get; set; }
		public string NamaProduk { get; set; }
		public int Quantity { get; set; }
		public decimal HargaBeli { get; set; }
		public decimal Total { get; set; }
		public string NamaBarang { get; set; }
		public string KodeBarang { get; set; }
		public decimal TunaiDibayar { get; set; }
		public decimal SubtotalAsli { get; set; }

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}