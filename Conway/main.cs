using System;
using Conway;

class MainClass
{
	public static void Main (string[] args)
	{
		string filename;

		if (args.Length == 0)
			filename = "init_highlife_replicator.txt";
		else if (args.Length == 1)
			filename = args[0];
		else
		{
			Console.WriteLine("Too many args!");
			return;
		}
		
		GameOfLife GoL = new GameOfLife(filename);

		GoL.PrintBoard();
		for (int i = 0; i < 50; i++)
		{
			Console.ReadKey();
			GoL.NextTurn();
			GoL.PrintBoard();
		}

		Console.WriteLine("That\'s a Wrap!");
	}
}
