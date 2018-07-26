using System;
using System.IO;
using System.Text;

using Microsoft.Data.Sqlite;

namespace VeryCDOfflineWebService
{
	public static class Helper
	{
		public static void Convert(String sourceFileName, String targetFileName)
		{
			SqliteConnectionStringBuilder sourceConnectionBuilder = new SqliteConnectionStringBuilder()
			{
				DataSource = sourceFileName,
				Mode = SqliteOpenMode.ReadOnly
			};
			SqliteConnectionStringBuilder targetConnectionBuilder = new SqliteConnectionStringBuilder()
			{
				DataSource = targetFileName,
				Mode = SqliteOpenMode.ReadWriteCreate
			};

			using (SqliteConnection sourceConnection = new SqliteConnection(sourceConnectionBuilder.ToString()))
			{
				using (SqliteConnection targetConnection = new SqliteConnection(targetConnectionBuilder.ToString()))
				{
					sourceConnection.Open();
					targetConnection.Open();

					using (SqliteCommand createTableCommand = new SqliteCommand(Helper.SQL.CreateTable, targetConnection))
					{
						createTableCommand.ExecuteNonQuery();
					}

					using (SqliteCommand selectCommand = new SqliteCommand(Helper.SQL.SelectAll, sourceConnection))
					{
						using (SqliteDataReader reader = selectCommand.ExecuteReader())
						{
							Int32 count = 0;
							SqliteTransaction transaction = null;

							while (reader.Read())
							{
								if (count == 0)
								{
									transaction = targetConnection.BeginTransaction();
								}

								using (SqliteCommand insertCommand = new SqliteCommand(Helper.SQL.InsertEntry, targetConnection, transaction))
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

							using (SqliteCommand indexCommand = new SqliteCommand(Helper.SQL.CreateIndex, targetConnection))
							{
								indexCommand.ExecuteNonQuery();
							}
						}
					}
				}
			}
		}

		public const Int32 BatchSize = 1000;

		public static readonly String DateTimeFormat = "yyyy-MM-dd HH:mm:ss.FFFFFFF";

		public static class SQL
		{
			public static readonly String CreateTable = @"CREATE TABLE IF NOT EXISTS Entries (ID INTEGER PRIMARY KEY, Title TEXT NOT NULL, Description TEXT NOT NULL, Link TEXT NOT NULL, Category TEXT NOT NULL, SubCategory TEXT NOT NULL, PublishTime Text NOT NULL, UpdateTime Text NOT NULL)";
			public static readonly String SelectAll = @"SELECT * FROM verycd";
			public static readonly String InsertEntry = @"INSERT INTO Entries VALUES (NULL, @Title, @Description, @Link, @Category, @SubCategory, @PublishTime, @UpdateTime)";
			public static readonly String CreateIndex = @"CREATE INDEX TitleIndex ON Entries (Title)";
		}
	}
}