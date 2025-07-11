-- phpMyAdmin SQL Dump
-- version 5.2.1
-- https://www.phpmyadmin.net/
--
-- Host: 127.0.0.1
-- Generation Time: Jul 11, 2025 at 10:35 AM
-- Server version: 10.4.32-MariaDB
-- PHP Version: 8.2.12

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- Database: `cokro`
--

-- --------------------------------------------------------

--
-- Table structure for table `pembayaran`
--

CREATE TABLE `pembayaran` (
  `id` int(11) NOT NULL,
  `tgl_pembelian` date NOT NULL,
  `tunai` decimal(10,0) NOT NULL,
  `tgl_pembayaran` date NOT NULL,
  `kode_transaksi` varchar(50) NOT NULL,
  `jenis` enum('piutang','hutang') NOT NULL COMMENT 'piutang: untuk transaction, hutang: untuk transaction_in'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `products`
--

CREATE TABLE `products` (
  `id` int(11) NOT NULL,
  `barcode` varchar(50) NOT NULL,
  `nama_produk` varchar(100) NOT NULL,
  `harga_jual` decimal(10,2) NOT NULL,
  `stok` int(11) NOT NULL,
  `stok_awal` int(11) DEFAULT 0,
  `supplier_id` int(11) NOT NULL,
  `harga_beli` decimal(10,2) DEFAULT NULL,
  `mark_up` decimal(10,2) DEFAULT NULL,
  `pendapatan` decimal(10,2) NOT NULL DEFAULT 0.00,
  `laba` decimal(10,2) NOT NULL DEFAULT 0.00,
  `harta` decimal(10,2) DEFAULT NULL,
  `persentase` varchar(50) DEFAULT NULL,
  `created_at` timestamp NOT NULL DEFAULT current_timestamp(),
  `updated_at` timestamp NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `products`
--

INSERT INTO `products` (`id`, `barcode`, `nama_produk`, `harga_jual`, `stok`, `stok_awal`, `supplier_id`, `harga_beli`, `mark_up`, `pendapatan`, `laba`, `harta`, `persentase`, `created_at`, `updated_at`) VALUES
(4, '8990044000123', 'POCKY STRAW SINGLE 12 GR', 30000.00, 7, 0, 3, 20000.00, 100000.00, 10000.00, 100000.00, 200000.00, '50,0', '2025-07-11 01:48:05', '2025-07-11 06:14:23');

-- --------------------------------------------------------

--
-- Table structure for table `suppliers`
--

CREATE TABLE `suppliers` (
  `id` int(11) NOT NULL,
  `nama_supplier` varchar(100) NOT NULL,
  `kontak` varchar(50) DEFAULT NULL,
  `alamat` text DEFAULT NULL,
  `created_at` timestamp NOT NULL DEFAULT current_timestamp(),
  `updated_at` timestamp NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `suppliers`
--

INSERT INTO `suppliers` (`id`, `nama_supplier`, `kontak`, `alamat`, `created_at`, `updated_at`) VALUES
(3, 'Bangkit', 'bangkitsann28@gmail.com', 'kangkung', '2025-07-10 17:45:02', '2025-07-10 17:45:02'),
(4, 'Rizqi', 'rizqiaku@gmail.com', 'Jepang', '2025-07-10 17:45:02', '2025-07-10 17:45:02');

-- --------------------------------------------------------

--
-- Table structure for table `transactions`
--

CREATE TABLE `transactions` (
  `id` int(11) NOT NULL,
  `kode_transaksi` varchar(50) NOT NULL,
  `kode_product` varchar(255) NOT NULL,
  `nama` varchar(255) NOT NULL,
  `qty` int(11) NOT NULL,
  `harga` decimal(10,2) NOT NULL,
  `subtotal` decimal(10,2) NOT NULL,
  `mark_up` decimal(10,2) NOT NULL,
  `laba` decimal(10,2) NOT NULL,
  `payment` enum('tunai','kredit') NOT NULL DEFAULT 'tunai',
  `namaPelanggan` varchar(255) NOT NULL DEFAULT '-',
  `Tunai` decimal(10,0) NOT NULL,
  `status` varchar(30) NOT NULL DEFAULT '-',
  `retur` int(11) NOT NULL DEFAULT 0,
  `tanggal_transaksi` datetime DEFAULT current_timestamp(),
  `tgl_pelunasan` datetime DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `transaction_in`
--

CREATE TABLE `transaction_in` (
  `id` int(11) NOT NULL,
  `kode_transaksi` varchar(50) NOT NULL,
  `tgl` datetime NOT NULL,
  `no_faktur` varchar(255) NOT NULL,
  `kode_product` varchar(255) NOT NULL,
  `nama` varchar(255) NOT NULL,
  `qty` int(11) NOT NULL,
  `suplier` varchar(255) NOT NULL,
  `payment` enum('tunai','kredit') NOT NULL DEFAULT 'tunai',
  `harga` decimal(10,0) NOT NULL,
  `subtotal` decimal(10,0) NOT NULL,
  `retur` int(11) NOT NULL DEFAULT 0
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `transaction_in`
--

INSERT INTO `transaction_in` (`id`, `kode_transaksi`, `tgl`, `no_faktur`, `kode_product`, `nama`, `qty`, `suplier`, `payment`, `harga`, `subtotal`, `retur`) VALUES
(4885, 'TI20250711125450', '2025-07-11 13:00:00', 'TI20250711125450', '8990044000123', 'POCKY STRAW SINGLE 12 GR', 5, 'Bangkit', 'tunai', 20000, 100000, 0),
(4886, 'TI20250711131423', '2025-07-11 13:14:23', 'TI20250711131423', '8990044000123', 'POCKY STRAW SINGLE 12 GR', 1, 'Rizqi', 'kredit', 20000, 20000, 0);

-- --------------------------------------------------------

--
-- Table structure for table `users`
--

CREATE TABLE `users` (
  `id` int(11) NOT NULL,
  `username` varchar(50) NOT NULL,
  `password` varchar(255) NOT NULL,
  `role` enum('admin','kasir') NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `users`
--

INSERT INTO `users` (`id`, `username`, `password`, `role`) VALUES
(3, 'admin', '$2a$12$drGowvm5yUqn2GFYv65X/uaWdy2agsNUYd6sPkX7nuhMWQV3t.XmC', 'admin'),
(4, 'kasir', '$2a$12$peIET/7TdGqPPtZhX1riX.oIEzNb8PqWT5LyHUMMBBANNWt7oQfna', 'kasir');

--
-- Indexes for dumped tables
--

--
-- Indexes for table `pembayaran`
--
ALTER TABLE `pembayaran`
  ADD PRIMARY KEY (`id`),
  ADD KEY `fk_pembayaran_transactions` (`kode_transaksi`),
  ADD KEY `idx_pembayaran_jenis` (`jenis`);

--
-- Indexes for table `products`
--
ALTER TABLE `products`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `barcode` (`barcode`),
  ADD KEY `fk_products_supplier` (`supplier_id`);

--
-- Indexes for table `suppliers`
--
ALTER TABLE `suppliers`
  ADD PRIMARY KEY (`id`);

--
-- Indexes for table `transactions`
--
ALTER TABLE `transactions`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `kode_transaksi` (`kode_transaksi`),
  ADD KEY `fk_transactions_product` (`kode_product`);

--
-- Indexes for table `transaction_in`
--
ALTER TABLE `transaction_in`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `kode_transaksi` (`kode_transaksi`),
  ADD KEY `fk_transactionsin_product` (`kode_product`);

--
-- Indexes for table `users`
--
ALTER TABLE `users`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `username` (`username`);

--
-- AUTO_INCREMENT for dumped tables
--

--
-- AUTO_INCREMENT for table `pembayaran`
--
ALTER TABLE `pembayaran`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=15741;

--
-- AUTO_INCREMENT for table `products`
--
ALTER TABLE `products`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=6;

--
-- AUTO_INCREMENT for table `suppliers`
--
ALTER TABLE `suppliers`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=5;

--
-- AUTO_INCREMENT for table `transactions`
--
ALTER TABLE `transactions`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=4;

--
-- AUTO_INCREMENT for table `transaction_in`
--
ALTER TABLE `transaction_in`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=4887;

--
-- AUTO_INCREMENT for table `users`
--
ALTER TABLE `users`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=5;

--
-- Constraints for dumped tables
--

--
-- Constraints for table `pembayaran`
--
ALTER TABLE `pembayaran`
  ADD CONSTRAINT `fk_pembayaran_transaction_in` FOREIGN KEY (`kode_transaksi`) REFERENCES `transaction_in` (`kode_transaksi`) ON DELETE CASCADE ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_pembayaran_transactions` FOREIGN KEY (`kode_transaksi`) REFERENCES `transactions` (`kode_transaksi`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Constraints for table `products`
--
ALTER TABLE `products`
  ADD CONSTRAINT `fk_products_supplier` FOREIGN KEY (`supplier_id`) REFERENCES `suppliers` (`id`) ON UPDATE CASCADE;

--
-- Constraints for table `transactions`
--
ALTER TABLE `transactions`
  ADD CONSTRAINT `fk_transactions_product` FOREIGN KEY (`kode_product`) REFERENCES `products` (`barcode`) ON UPDATE CASCADE;

--
-- Constraints for table `transaction_in`
--
ALTER TABLE `transaction_in`
  ADD CONSTRAINT `fk_transactionsin_product` FOREIGN KEY (`kode_product`) REFERENCES `products` (`barcode`) ON UPDATE CASCADE;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
