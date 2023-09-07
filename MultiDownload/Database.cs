using System;
using System.Data.SQLite;

namespace MultiDownload
{
    class Database
    {
        SQLiteConnection con;
        String dbFile;

        public Database(String filename)
        {
            string connectionString = "Data Source="+ filename+"; Version=3;";
            con = new SQLiteConnection(connectionString);
            dbFile = filename;
        }

        public void CreateIfNotExist()
        {
            try
            {
                con.Open();

                string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS Users (
                    Id INTEGER PRIMARY KEY,
                    Username TEXT UNIQUE,
                    Pass TEXT
                )";

                using (SQLiteCommand command = new SQLiteCommand(createTableQuery, con))
                {
                    command.ExecuteNonQuery();
                }

                con.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("SQLite veritabanı bağlantısı başarısız: " + ex.Message);
                con.Close();
            }
        }

        public bool CreateUser(String username, String password)
        {
            try
            {
                con.Open();

                string createTableQuery = "Insert into Users(username,pass) Values(@Username, @Password)";

                using (SQLiteCommand command = new SQLiteCommand(createTableQuery, con))
                {
                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@Password", password);

                    command.ExecuteScalar();
                }

                con.Close();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("SQLite veritabanı bağlantısı başarısız: " + ex.Message);
                con.Close();
            }
            return false;
        }

        public bool CheckUser(String username, String password)
        {
            try
            {
                con.Open();

                string query = "SELECT COUNT(*) FROM Users WHERE username = @Username AND pass = @Password";
                using (SQLiteCommand command = new SQLiteCommand(query, con))
                {
                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@Password", password);

                    int count = Convert.ToInt32(command.ExecuteScalar());

                    if (count > 0)
                    {
                        return true;
                    }
                }

                con.Close();

            }
            catch (Exception ex)
            {
                Console.WriteLine("SQLite veritabanı bağlantısı başarısız: " + ex.Message);
                con.Close();
            }
            return false;
        }
    }
}
