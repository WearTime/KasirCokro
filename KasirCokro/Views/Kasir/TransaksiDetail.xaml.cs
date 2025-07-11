using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using KasirCokro.Helpers;
using MySql.Data.MySqlClient;

namespace KasirCokro.Views.Kasir
{
    public partial class TransaksiDetail : Window, INotifyPropertyChanged
    {
        #region Fields
        private int _transaksiId;
        private TransaksiDetailModel _transaksiInfo;
        private ObservableCollection<TransaksiItemModel> _transaksiItems;
        private bool _isLoading = false;
        #endregion

        #region Properties
        public TransaksiDetailModel TransaksiInfo
        {
            get => _transaksiInfo;
            set { _transaksiInfo = value; OnPropertyChanged(); }
        }

        public ObservableCollection<TransaksiItemModel> TransaksiItems
        {
            get => _transaksiItems;
            set { _transaksiItems = value; OnPropertyChanged(); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }
        #endregion

        #region Constructor
        public TransaksiDetail(int transaksiId)
        {
            InitializeComponent();
            _transaksiId = transaksiId;
            InitializeData();
            DataContext = this;
            _ = LoadTransaksiDetailAsync();
        }

        private void InitializeData()
        {
            TransaksiItems = new ObservableCollection<TransaksiItemModel>();
            TransaksiInfo = new TransaksiDetailModel();
        }
        #endregion

        #region Data Loading
        private async Task LoadTransaksiDetailAsync()
        {
            try
            {
                IsLoading = true;
                await LoadTransaksiInfoAsync();
                await LoadTransaksiItemsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading transaksi detail: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
                await LoadSampleDataAsync();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadTransaksiInfoAsync()
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                await conn.OpenAsync();
                string query = @"
                    SELECT 
                        t.id as TransaksiID,
                        t.kode_transaksi as NoTransaksi,
                        t.created_at as TanggalTransaksi,
                        t.total as Total,
                        t.diskon as DiskonAmount,
                        t.pajak as PajakAmount,
                        t.subtotal as Subtotal,
                        t.status as StatusTransaksi,
                        t.user_id as UserID,
                        t.metode_pembayaran as MetodePembayaran,
                        t.uang_diterima as UangDiterima,
                        t.kembalian as Kembalian,
                        t.catatan as Catatan,
                        u.username as NamaKasir,
                        u.nama_lengkap as NamaLengkapKasir
                    FROM transactions t
                    LEFT JOIN users u ON t.user_id = u.id
                    WHERE t.id = @transaksiId";

                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@transaksiId", _transaksiId);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            TransaksiInfo = new TransaksiDetailModel
                            {
                                TransaksiID = Convert.ToInt32(reader["TransaksiID"]),
                                NoTransaksi = reader["NoTransaksi"].ToString(),
                                TanggalTransaksi = Convert.ToDateTime(reader["TanggalTransaksi"]),
                                Subtotal = reader.IsDBNull(reader.GetOrdinal("Subtotal")) ? 0 : Convert.ToDecimal(reader["Subtotal"]),
                                PajakAmount = reader.IsDBNull(reader.GetOrdinal("PajakAmount")) ? 0 : Convert.ToDecimal(reader["PajakAmount"]),
                                DiskonAmount = reader.IsDBNull(reader.GetOrdinal("DiskonAmount")) ? 0 : Convert.ToDecimal(reader["DiskonAmount"]),
                                Total = Convert.ToDecimal(reader["Total"]),
                                StatusTransaksi = reader.IsDBNull(reader.GetOrdinal("StatusTransaksi")) ? "Selesai" : reader["StatusTransaksi"].ToString(),
                                NamaKasir = reader.IsDBNull(reader.GetOrdinal("NamaKasir")) ? "Unknown" : reader["NamaKasir"].ToString(),
                                NamaLengkapKasir = reader.IsDBNull(reader.GetOrdinal("NamaLengkapKasir")) ? "" : reader["NamaLengkapKasir"].ToString(),
                                MetodePembayaran = reader.IsDBNull(reader.GetOrdinal("MetodePembayaran")) ? "Tunai" : reader["MetodePembayaran"].ToString(),
                                UangDiterima = reader.IsDBNull(reader.GetOrdinal("UangDiterima")) ? 0 : Convert.ToDecimal(reader["UangDiterima"]),
                                Kembalian = reader.IsDBNull(reader.GetOrdinal("Kembalian")) ? 0 : Convert.ToDecimal(reader["Kembalian"]),
                                Catatan = reader.IsDBNull(reader.GetOrdinal("Catatan")) ? "" : reader["Catatan"].ToString()

                            };
                        }
                    }
                }
            }
        }

        private async Task LoadTransaksiItemsAsync()
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                await conn.OpenAsync();
                string query = @"
                    SELECT 
                        td.id as DetailID,
                        td.produk_id as ProdukID,
                        td.nama_produk as NamaProduk,
                        td.harga as HargaSatuan,
                        td.jumlah as Jumlah,
                        td.subtotal as SubtotalItem,
                        td.diskon as DiskonItem,
                        p.kode_produk as KodeProduk,
                        p.kategori as Kategori,
                        p.satuan as Satuan
                    FROM transaction_details td
                    LEFT JOIN products p ON td.produk_id = p.id
                    WHERE td.transaksi_id = @transaksiId
                    ORDER BY td.id";

                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@transaksiId", _transaksiId);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        TransaksiItems.Clear();
                        while (await reader.ReadAsync())
                        {
                            var item = new TransaksiItemModel
                            {
                                DetailID = Convert.ToInt32(reader["DetailID"]),
                                ProdukID = reader.IsDBNull(reader.GetOrdinal("ProdukID")) ? 0 : Convert.ToInt32(reader["ProdukID"]),
                                KodeProduk = reader.IsDBNull(reader.GetOrdinal("KodeProduk")) ? "" : reader["KodeProduk"].ToString(),
                                NamaProduk = reader["NamaProduk"].ToString(),
                                HargaSatuan = Convert.ToDecimal(reader["HargaSatuan"]),
                                Jumlah = Convert.ToInt32(reader["Jumlah"]),
                                SubtotalItem = Convert.ToDecimal(reader["SubtotalItem"]),
                                DiskonItem = reader.IsDBNull(reader.GetOrdinal("DiskonItem")) ? 0 : Convert.ToDecimal(reader["DiskonItem"]),
                                Kategori = reader.IsDBNull(reader.GetOrdinal("Kategori")) ? "" : reader["Kategori"].ToString(),
                                Satuan = reader.IsDBNull(reader.GetOrdinal("Satuan")) ? "pcs" : reader["Satuan"].ToString()

                            };
                            TransaksiItems.Add(item);
                        }
                    }
                }
            }
        }

        private async Task LoadSampleDataAsync()
        {
            await Task.Run(() =>
            {
                var random = new Random();

                // Sample transaction info
                TransaksiInfo = new TransaksiDetailModel
                {
                    TransaksiID = _transaksiId,
                    NoTransaksi = $"TRX-{DateTime.Now:yyyyMM}-{_transaksiId:D4}",
                    TanggalTransaksi = DateTime.Now.AddHours(-random.Next(1, 24)),
                    Subtotal = 150000,
                    PajakAmount = 15000,
                    DiskonAmount = 5000,
                    Total = 160000,
                    StatusTransaksi = "Selesai",
                    NamaKasir = "John Doe",
                    NamaLengkapKasir = "John Doe Smith",
                    MetodePembayaran = "Tunai",
                    UangDiterima = 200000,
                    Kembalian = 40000,
                    Catatan = "Transaksi sample untuk testing"
                };

                // Sample transaction items
                Application.Current.Dispatcher.Invoke(() =>
                {
                    TransaksiItems.Clear();
                    var sampleItems = new[]
                    {
                        new TransaksiItemModel { DetailID = 1, KodeProduk = "PRD001", NamaProduk = "Nasi Gudeg", HargaSatuan = 25000, Jumlah = 2, SubtotalItem = 50000, Kategori = "Makanan", Satuan = "porsi" },
                        new TransaksiItemModel { DetailID = 2, KodeProduk = "PRD002", NamaProduk = "Es Teh Manis", HargaSatuan = 5000, Jumlah = 3, SubtotalItem = 15000, Kategori = "Minuman", Satuan = "gelas" },
                        new TransaksiItemModel { DetailID = 3, KodeProduk = "PRD003", NamaProduk = "Ayam Bakar", HargaSatuan = 35000, Jumlah = 1, SubtotalItem = 35000, Kategori = "Makanan", Satuan = "porsi" },
                        new TransaksiItemModel { DetailID = 4, KodeProduk = "PRD004", NamaProduk = "Kerupuk", HargaSatuan = 10000, Jumlah = 5, SubtotalItem = 50000, Kategori = "Makanan", Satuan = "pack" }
                    };

                    foreach (var item in sampleItems)
                    {
                        TransaksiItems.Add(item);
                    }
                });
            });
        }
        #endregion

        #region Event Handlers
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Fitur print akan segera tersedia", "Info",
                           MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            _ = LoadTransaksiDetailAsync();
        }
        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }

    #region Models
    public class TransaksiDetailModel : INotifyPropertyChanged
    {
        private int _transaksiID;
        private string _noTransaksi;
        private DateTime _tanggalTransaksi;
        private decimal _subtotal;
        private decimal _pajakAmount;
        private decimal _diskonAmount;
        private decimal _total;
        private string _statusTransaksi;
        private string _namaKasir;
        private string _namaLengkapKasir;
        private string _metodePembayaran;
        private decimal _uangDiterima;
        private decimal _kembalian;
        private string _catatan;

        public int TransaksiID
        {
            get => _transaksiID;
            set { _transaksiID = value; OnPropertyChanged(); }
        }

        public string NoTransaksi
        {
            get => _noTransaksi;
            set { _noTransaksi = value; OnPropertyChanged(); }
        }

        public DateTime TanggalTransaksi
        {
            get => _tanggalTransaksi;
            set { _tanggalTransaksi = value; OnPropertyChanged(); OnPropertyChanged(nameof(TanggalFormatted)); OnPropertyChanged(nameof(WaktuFormatted)); }
        }

        public decimal Subtotal
        {
            get => _subtotal;
            set { _subtotal = value; OnPropertyChanged(); OnPropertyChanged(nameof(SubtotalFormatted)); }
        }

        public decimal PajakAmount
        {
            get => _pajakAmount;
            set { _pajakAmount = value; OnPropertyChanged(); OnPropertyChanged(nameof(PajakFormatted)); }
        }

        public decimal DiskonAmount
        {
            get => _diskonAmount;
            set { _diskonAmount = value; OnPropertyChanged(); OnPropertyChanged(nameof(DiskonFormatted)); }
        }

        public decimal Total
        {
            get => _total;
            set { _total = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalFormatted)); }
        }

        public string StatusTransaksi
        {
            get => _statusTransaksi;
            set { _statusTransaksi = value; OnPropertyChanged(); }
        }

        public string NamaKasir
        {
            get => _namaKasir;
            set { _namaKasir = value; OnPropertyChanged(); }
        }

        public string NamaLengkapKasir
        {
            get => _namaLengkapKasir;
            set { _namaLengkapKasir = value; OnPropertyChanged(); }
        }

        public string MetodePembayaran
        {
            get => _metodePembayaran;
            set { _metodePembayaran = value; OnPropertyChanged(); }
        }

        public decimal UangDiterima
        {
            get => _uangDiterima;
            set { _uangDiterima = value; OnPropertyChanged(); OnPropertyChanged(nameof(UangDiterimaFormatted)); }
        }

        public decimal Kembalian
        {
            get => _kembalian;
            set { _kembalian = value; OnPropertyChanged(); OnPropertyChanged(nameof(KembalianFormatted)); }
        }

        public string Catatan
        {
            get => _catatan;
            set { _catatan = value; OnPropertyChanged(); }
        }

        // Formatted Properties
        public string TanggalFormatted => TanggalTransaksi.ToString("dd MMMM yyyy");
        public string WaktuFormatted => TanggalTransaksi.ToString("HH:mm:ss");
        public string SubtotalFormatted => Subtotal.ToString("C0", new CultureInfo("id-ID"));
        public string PajakFormatted => PajakAmount.ToString("C0", new CultureInfo("id-ID"));
        public string DiskonFormatted => DiskonAmount.ToString("C0", new CultureInfo("id-ID"));
        public string TotalFormatted => Total.ToString("C0", new CultureInfo("id-ID"));
        public string UangDiterimaFormatted => UangDiterima.ToString("C0", new CultureInfo("id-ID"));
        public string KembalianFormatted => Kembalian.ToString("C0", new CultureInfo("id-ID"));

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class TransaksiItemModel : INotifyPropertyChanged
    {
        private int _detailID;
        private int _produkID;
        private string _kodeProduk;
        private string _namaProduk;
        private decimal _hargaSatuan;
        private int _jumlah;
        private decimal _subtotalItem;
        private decimal _diskonItem;
        private string _kategori;
        private string _satuan;

        public int DetailID
        {
            get => _detailID;
            set { _detailID = value; OnPropertyChanged(); }
        }

        public int ProdukID
        {
            get => _produkID;
            set { _produkID = value; OnPropertyChanged(); }
        }

        public string KodeProduk
        {
            get => _kodeProduk;
            set { _kodeProduk = value; OnPropertyChanged(); }
        }

        public string NamaProduk
        {
            get => _namaProduk;
            set { _namaProduk = value; OnPropertyChanged(); }
        }

        public decimal HargaSatuan
        {
            get => _hargaSatuan;
            set { _hargaSatuan = value; OnPropertyChanged(); OnPropertyChanged(nameof(HargaFormatted)); }
        }

        public int Jumlah
        {
            get => _jumlah;
            set { _jumlah = value; OnPropertyChanged(); }
        }

        public decimal SubtotalItem
        {
            get => _subtotalItem;
            set { _subtotalItem = value; OnPropertyChanged(); OnPropertyChanged(nameof(SubtotalFormatted)); }
        }

        public decimal DiskonItem
        {
            get => _diskonItem;
            set { _diskonItem = value; OnPropertyChanged(); OnPropertyChanged(nameof(DiskonFormatted)); }
        }

        public string Kategori
        {
            get => _kategori;
            set { _kategori = value; OnPropertyChanged(); }
        }

        public string Satuan
        {
            get => _satuan;
            set { _satuan = value; OnPropertyChanged(); }
        }

        // Formatted Properties
        public string HargaFormatted => HargaSatuan.ToString("C0", new CultureInfo("id-ID"));
        public string SubtotalFormatted => SubtotalItem.ToString("C0", new CultureInfo("id-ID"));
        public string DiskonFormatted => DiskonItem > 0 ? DiskonItem.ToString("C0", new CultureInfo("id-ID")) : "-";

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    #endregion
}