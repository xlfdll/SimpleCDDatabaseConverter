using System;

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
				Console.WriteLine("Usage: <source database file> <target database file>");
				Console.WriteLine();
			}
			else
			{
				Console.WriteLine("Converting SimpleCD database ...");
				Console.WriteLine($"- From: {args[0]}");
				Console.WriteLine($"- To: {args[1]}");
				Console.WriteLine();

				Helper.Convert(args[0], args[1]);

				Console.WriteLine("Done.");
				Console.WriteLine();
			}
		}
	}
}