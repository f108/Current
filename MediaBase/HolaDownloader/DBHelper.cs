using System;
using System.Text;
using Npgsql;

namespace HolaDownloader
{
    static class DBHelper
    {
        public static string ExecuteScalar(string sql)
        {
            string ret;
            using (NpgsqlConnection sqlConnection = new NpgsqlConnection(Program.connectionString))
            {
                sqlConnection.Open();
                ret = ExecuteScalar(sqlConnection, sql);
                sqlConnection.Close();
            }
            return ret;
        }

        public static string ExecuteScalar(NpgsqlConnection sqlConnection, string sql)
        {
            string ret;
            using (var cmd = new NpgsqlCommand(sql, sqlConnection))
            {
                ret = (string)cmd.ExecuteScalar();
            }
            return ret;
        }

        public static void ExecuteNonQuery(string sql)
        {
            using (NpgsqlConnection sqlConnection = new NpgsqlConnection(Program.connectionString))
            {
                sqlConnection.Open();
                ExecuteNonQuery(sqlConnection, sql);
                sqlConnection.Close();
            }
        }

        public static void ExecuteNonQuery(NpgsqlConnection sqlConnection, string sql)
        {
            using (var cmd = new NpgsqlCommand(sql, sqlConnection))
            {
                cmd.ExecuteNonQuery();
            }
        }

    }

}
