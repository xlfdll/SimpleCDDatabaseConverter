using System;
using System.Diagnostics;

namespace VeryCDOfflineWebService
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine();
            Console.WriteLine("SimpleCD Database Converter");
            Console.WriteLine("(C) 2017 Xlfdll Workstation");
            Console.WriteLine();

            if (args.Length < 2)
            {
                Console.WriteLine("Usage: SimpleCDDatabaseConverter <source database file> <target database file>");
                Console.WriteLine();
                Console.WriteLine("<source database file>");
                Console.WriteLine("- The file name of SimpleCD Desktop SQLite database to be converted from");
                Console.WriteLine();
                Console.WriteLine("<target database file>");
                Console.WriteLine("- The file name of VeryCD Offline Web Service SQLite database to be converted to");
                Console.WriteLine();
            }
            else
            {
                Console.WriteLine("Converting SimpleCD database ...");
                Console.WriteLine($"- From: {args[0]}");
                Console.WriteLine($"- To: {args[1]}");
                Console.WriteLine();

                try
                {
                    Helper.Convert(args[0], args[1]);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR: {ex.Message}");

                    Trace.WriteLine(ex);
                }

                Console.WriteLine("Done.");
                Console.WriteLine();
            }
        }
    }
}