using System;
using System.Diagnostics;
using System.IO;
using System.Text;

using System.Data.SQLite;

namespace VeryCDOfflineWebService
{
    public static class Helper
    {
        public static void Convert(String sourceFileName, String targetFileName)
        {
            if (File.Exists(targetFileName))
            {
                File.Delete(targetFileName);
            }
            if (File.Exists(targetFileName + ".log"))
            {
                File.Delete(targetFileName + ".log");
            }

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

                    targetConnection.EnableExtensions(true);
                    targetConnection.LoadExtension("SQLite.Interop.dll", "sqlite3_fts5_init");

                    Helper.CreateDataSchema(targetConnection);
                    Helper.FillDataEntries(targetFileName, sourceConnection, targetConnection);
                }
            }
        }

        private static void CreateDataSchema(SQLiteConnection targetConnection)
        {
            using (SQLiteCommand createTableCommand = new SQLiteCommand(Helper.SQL.CreateTable, targetConnection))
            {
                createTableCommand.ExecuteNonQuery();
            }

            using (SQLiteCommand createFTSTableCommand = new SQLiteCommand(Helper.SQL.CreateFTSTable, targetConnection))
            {
                createFTSTableCommand.ExecuteNonQuery();
            }

            using (SQLiteCommand createFTSInsertTriggerCommand = new SQLiteCommand(Helper.SQL.CreateFTSInsertTrigger, targetConnection))
            {
                createFTSInsertTriggerCommand.ExecuteNonQuery();
            }

            using (SQLiteCommand createFTSUpdateTriggerCommand = new SQLiteCommand(Helper.SQL.CreateFTSUpdateTrigger, targetConnection))
            {
                createFTSUpdateTriggerCommand.ExecuteNonQuery();
            }

            using (SQLiteCommand createFTSDeleteTriggerCommand = new SQLiteCommand(Helper.SQL.CreateFTSDeleteTrigger, targetConnection))
            {
                createFTSDeleteTriggerCommand.ExecuteNonQuery();
            }
        }

        private static void FillDataEntries(string targetFileName, SQLiteConnection sourceConnection, SQLiteConnection targetConnection)
        {
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
                                    .Replace("`", Environment.NewLine)
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
                            catch (Exception ex)
                            {
                                String errorLine = $"! - {reader[0]}: {reader["title"]}";

                                File.AppendAllText(targetFileName + ".log", errorLine + Environment.NewLine);
                                Console.WriteLine(errorLine);

                                Trace.WriteLine(ex);
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

        public const Int32 BatchSize = 10000;
        public const String DateTimeFormat = "yyyy-MM-dd HH:mm:ss.FFFFFFF";

        public static class SQL
        {
            public const String CreateTable = "CREATE TABLE IF NOT EXISTS Entries (ID INTEGER PRIMARY KEY, Title TEXT NOT NULL, Description TEXT NOT NULL, Link TEXT NOT NULL, Category TEXT NOT NULL, SubCategory TEXT NOT NULL, PublishTime Text NOT NULL, UpdateTime Text NOT NULL);";
            public const String SelectAll = "SELECT * FROM verycd;";
            public const String InsertEntry = "INSERT INTO Entries VALUES (NULL, @Title, @Description, @Link, @Category, @SubCategory, @PublishTime, @UpdateTime);";
            public const String CreateIndex = "CREATE INDEX TitleIndex ON Entries (Title);";

            public const String CreateFTSTable = "CREATE VIRTUAL TABLE Entries_FTS USING fts5(Title, Description, content='Entries', content_rowid='ID');";
            public const String CreateFTSInsertTrigger
                = "CREATE TRIGGER Entries_FTS_Insert_Trigger AFTER INSERT ON Entries"
                + " BEGIN"
                + " INSERT INTO Entries_FTS (rowid, Title, Description) VALUES (new.ID, new.Title, new.Description);"
                + " END;";
            public const String CreateFTSUpdateTrigger
                = "CREATE TRIGGER Entries_FTS_Update_Trigger AFTER UPDATE ON Entries"
                + " BEGIN"
                + " INSERT INTO Entries_FTS (Entries_FTS, rowid, Title, Description) VALUES ('delete', old.ID, old.Title, old.Description);"
                + " INSERT INTO Entries_FTS (rowid, Title, Description) VALUES (new.ID, new.Title, new.Description);"
                + " END;";
            public const String CreateFTSDeleteTrigger
                = "CREATE TRIGGER Entries_FTS_Delete_Trigger AFTER DELETE ON Entries"
                + " BEGIN"
                + " INSERT INTO Entries_FTS (Entries_FTS, rowid, Title, Description) VALUES ('delete', old.ID, old.Title, old.Description);"
                + " END;";
        }
    }
}