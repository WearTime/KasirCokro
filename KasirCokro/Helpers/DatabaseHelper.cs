using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace KasirCokro.Helpers
{
    public static class DatabaseHelper
    {
        private static string connectionString = "server=localhost;user=root;database=cokro;port=3306;password=";
        public static MySqlConnection GetConnection()
        {
            try
            {
                MySqlConnection conn = new MySqlConnection(connectionString);
                conn.Open();
                return conn;
            }
            catch (Exception ex)
            {
                throw new Exception("Koneksi Gagal" + ex.Message);
            }
        }

        public static bool TestConnection()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString)) ;
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
