using System;
using System.IO;
using System.Text;

using System.Data.SQLite;

namespace VeryCDOfflineWebService
{
    public static class Helper
    {
        public static void Convert(String sourceFileName, String targetFileName)
        {
            SQLiteConnectionStringBuilder sourceConnectionBuilder = new SQLiteConnectionStringBuilder()
            {
                DataSource = sourceFileName,
                ReadOnly = true
            };
            SQLiteConnectionStringBuilder targetConnectionBuilder = new SQLiteConnectionStringBuilder()
            {
                DataSource = targetFileName
            };

            using (SQLiteConnection sourceConnection = new SQLiteConnection(sourceConnectionBuilder.ToString()))
            {
                using (SQLiteConnection targetConnection = new SQLiteConnection(targetConnectionBuilder.ToString()))
                {
                    sourceConnection.Open();
                    targetConnection.Open();

                    using (SQLiteCommand createTableCommand = new SQLiteCommand(Helper.SQL.CreateTable, targetConnection))
                    {
                        createTableCommand.ExecuteNonQuery();
                    }

                    using (SQLiteCommand selectCommand = new SQLiteCommand(Helper.SQL.SelectAll, sourceConnection))
                    {
                        using (SQLiteDataReader reader = selectCommand.ExecuteReader())
                        {
                            Int32 count = 0;
                            SQLiteTransaction transaction = null;

                            while (reader.Read())
                            {
                                if (count == 0)
                                {
                                    transaction = targetConnection.BeginTransaction();
                                }

                                using (SQLiteCommand insertCommand = new SQLiteCommand(Helper.SQL.InsertEntry, targetConnection, transaction))
                                {
                                    try
                                    {
                                        insertCommand.Parameters.AddWithValue("@Title", reader["title"]);
                                        insertCommand.Parameters.AddWithValue("@Description", $"{reader["brief"]}{Environment.NewLine}{Environment.NewLine}{reader["content"]}");


                                        String[] links = reader["ed2k"].ToString()
                                            .Replace("", Environment.NewLine)
                                            .Split(new String[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                                        StringBuilder sb = new StringBuilder();

                                        for (Int32 i = 0; i < links.Length; i += 2)
                                        {
                                            sb.AppendLine(links[i]);
                                        }

                                        insertCommand.Parameters.AddWithValue("@Link", sb.ToString());
                                        insertCommand.Parameters.AddWithValue("@Category", reader["category1"]);
                                        insertCommand.Parameters.AddWithValue("@SubCategory", reader["category2"]);
                                        insertCommand.Parameters.AddWithValue("@PublishTime", DateTime.Parse(reader["pubtime"].ToString()).ToString(Helper.DateTimeFormat));
                                        insertCommand.Parameters.AddWithValue("@UpdateTime", DateTime.Parse(reader["updtime"].ToString()).ToString(Helper.DateTimeFormat));

                                        insertCommand.ExecuteNonQuery();
                                    }
                                    catch
                                    {
                                        String errorLine = $"! - {reader[0]}: {reader["title"]}";

                                        File.AppendAllText(targetFileName + ".log", errorLine + Environment.NewLine);
                                        Console.WriteLine(errorLine);
                                    }
                                }

                                count++;

                                if (count == Helper.BatchSize)
                                {
                                    transaction.Commit();
                                    transaction.Dispose();

                                    count = 0;
                                }
                            }

                            if (count != 0)
                            {
                                transaction.Commit();
                                transaction.Dispose();
                            }

                            using (SQLiteCommand indexCommand = new SQLiteCommand(Helper.SQL.CreateIndex, targetConnection))
                            {
                                indexCommand.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
        }

        public const Int32 BatchSize = 1000;

        public const String DateTimeFormat = "yyyy-MM-dd HH:mm:ss.FFFFFFF";

        public static class SQL
        {
            public const String CreateTable = @"CREATE TABLE IF NOT EXISTS Entries (ID INTEGER PRIMARY KEY, Title TEXT NOT NULL, Description TEXT NOT NULL, Link TEXT NOT NULL, Category TEXT NOT NULL, SubCategory TEXT NOT NULL, PublishTime Text NOT NULL, UpdateTime Text NOT NULL)";
            public const String SelectAll = @"SELECT * FROM verycd";
            public const String InsertEntry = @"INSERT INTO Entries VALUES (NULL, @Title, @Description, @Link, @Category, @SubCategory, @PublishTime, @UpdateTime)";
            public const String CreateIndex = @"CREATE INDEX TitleIndex ON Entries (Title)";
        }
    }
}