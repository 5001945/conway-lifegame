using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Conway
{
	public enum DIRECTION
	{
		Up, UpRight, Right, DownRight, Down, DownLeft, Left, UpLeft
	};

	public class GameOfLife
	{
		public GameOfLife(string filename)
		{
			// 파일에서는 순서대로 규칙, 월드 크기, 살아있는 좌표가 나온다. 또한 #으로 시작하는 주석도 나올 수 있다.

			// 먼저 파일에서 한 줄씩 읽게끔 StreamReader를 설정한다.
			StreamReader sr = new StreamReader(filename);

			// 파일에서 rule을 읽는다. B3/S23, S23/B3, 23/3 등으로 나올 수 있다.
			string rawRule = sr.ReadLine();
			string[] rules;
			/*
			첫 줄의 규칙은 다음과 같다:
			1. 숫자는 0-8까지 가능하고, 최대 9개까지 올 수 있다. (사실 겹치지 않는 숫자면 좋겠다만... 이걸 정규표현식으로 표현하는 법은 잘 모르겠다)
			2. B/S 꼴, S/B 꼴, / 꼴이 있다.
			*/
			Regex regexBS = new Regex(@"^B[0-8]{0,9}/S[0-8]{0,9}$");
			Regex regexSB = new Regex(@"^S[0-8]{0,9}/B[0-8]{0,9}$");
			Regex regexSlash = new Regex(@"^[0-8]{0,9}/[0-8]{0,9}$");
			try
			{
				if (regexBS.IsMatch(rawRule))
				{
					rules = rawRule.Split('/');
					rules[0] = rules[0].TrimStart('B');
					rules[1] = rules[1].TrimStart('S');
				}
				else if (regexSB.IsMatch(rawRule) || regexSlash.IsMatch(rawRule))
				{
					rules = rawRule.Split('/');
					string temp = rules[0].TrimStart('S');
					rules[0] = rules[1].TrimStart('B');
					rules[1] = temp;
				}
				else
					throw new Exception("주어진 첫 줄이 형식에 맞지 않습니다.");
			}
			catch(Exception e)
			{
				Console.WriteLine(e.Message);
				return;
			}
			
			// rules는 string[] 타입으로, 각 원소는 "", "12", "345678" 등과 같이 되어 있다.
			// 이를 int[] 타입으로 바꿔 주어야 한다.
			foreach (char ch in rules[0])
				birth.Add(ch - '0');
			foreach (char ch in rules[1])
				survive.Add(ch - '0');
			/*
			// 예시 : B3/S23 규칙을 만듦
			this.Birth = new List<int> {3};
			this.Survive = new List<int> {2, 3};
			*/

			// 그 다음 월드의 크기를 얻는다.
			string rawWorldSize = sr.ReadLine();
			int[] worldSizes = Str2Intarr(rawWorldSize);
			CN = new CellNetwork(worldSizes[0], worldSizes[1]);
			/*
			// 예시 : 20x20 월드를 만듦
			CN = new CellNetwork(20, 20);
			*/

			// 그 다음 초기에 살아있는 셀의 좌표를 얻는다. 주석처리도 해야 한다.
			while (sr.Peek() >= 0)
			{
				string xy = sr.ReadLine();
				if ( (xy.Length == 0) || (xy[0] == '#') )
					continue;
				int[] xys = Str2Intarr(xy);
				CN[xys[0], xys[1]].NowState = 1;
			}
			sr.Close();
			/*
			// 예시 : plus 모양 만듦
			CN[6, 12].NowState = 1;
			CN[7, 11].NowState = 1;
			CN[7, 12].NowState = 1;
			CN[7, 13].NowState = 1;
			CN[8, 12].NowState = 1;
			*/
		}

		private List<int> birth = new List<int> ();
		private List<int> survive = new List<int> ();
		public List<int> Birth => birth;
		public List<int> Survive => survive;

		private CellNetwork CN;

		private int turnCount = 0;
		public int TurnCount => turnCount;

		public void NextTurn()
		{
			this.Calculate();
			CN.UpdateStates();
			turnCount++;
		}
		public void Calculate()
		{
			CN.CalculateNextStates(this);
		}
		public void PrintBoard()
		{
			Console.Clear();
			for (int r = 0; r < CN.Height; r++)
			{
				for (int c = 0; c < CN.Width; c++)
				{
					if (CN[r, c].NowState == 1)
						Console.Write("■");
					else
						Console.Write("□");
				}
				Console.WriteLine();
			}
			Console.WriteLine($"Turn : {turnCount}");
			Console.WriteLine();
		}

		public static int[] Str2Intarr(string str)
		{
			string[] strSplit = str.Split(' ');
			int[] intarr = new int[strSplit.Length];
			try
			{
				for (int i = 0; i < strSplit.Length; i++)
					intarr[i] = int.Parse(strSplit[i]);
			}
			catch (FormatException)
			{
				Console.WriteLine($"Unable to parse string to int array.");
				return null;
			}
			return intarr;
		}

	};

	public class CellNetwork
	{
		private List<Cell> Cells;
		private Cell OutOfBound = new Cell();

		public readonly int Width;
		public readonly int Height;
		// Width와 Height는 불변량.

		public CellNetwork(int width, int height)
		{
			// CellNetwork의 Width와 Height에 값을 대입한다.
			this.Width = width;
			this.Height = height;

			// Cells에 Width*Height개만큼 기본 Cell을 넣는다.
			Cells = new List<Cell> ();
			for (int i = 0; i < Width*Height; i++)
				Cells.Add(new Cell());

			// 각 Cell에 대해 NearCell을 연결한다.
			for (int i = 0; i < Width*Height; i++)
			{
				// direction은 시계방향으로 0부터 7까지이다.
				// index는 왼쪽 위로 갈 수록 작아진다.

				// case Up:
				if (i < Width)
					Cells[i].NearCell[(int)DIRECTION.Up] = OutOfBound;
				else
					Cells[i].NearCell[(int)DIRECTION.Up] = Cells[i - Width];
				// case UpRight:
				if ( (i < Width) || (i%Width == Width-1) )
					Cells[i].NearCell[(int)DIRECTION.UpRight] = OutOfBound;
				else
					Cells[i].NearCell[(int)DIRECTION.UpRight] = Cells[i - Width + 1];
				// case Right:
				if (i%Width == Width-1)
					Cells[i].NearCell[(int)DIRECTION.Right] = OutOfBound;
				else
					Cells[i].NearCell[(int)DIRECTION.Right] = Cells[i + 1];
				// case DownRight:
				if ( (i >= Width*(Height-1)) || (i%Width == Width-1) )
					Cells[i].NearCell[(int)DIRECTION.DownRight] = OutOfBound;
				else
					Cells[i].NearCell[(int)DIRECTION.DownRight] = Cells[i + Width + 1];
				// case Down:
				if (i >= Width*(Height-1))
					Cells[i].NearCell[(int)DIRECTION.Down] = OutOfBound;
				else
					Cells[i].NearCell[(int)DIRECTION.Down] = Cells[i + Width];
				// case DownLeft:
				if ( (i >= Width*(Height-1)) || (i%Width == 0) )
					Cells[i].NearCell[(int)DIRECTION.DownLeft] = OutOfBound;
				else
					Cells[i].NearCell[(int)DIRECTION.DownLeft] = Cells[i + Width - 1];
				// case Left:
				if (i%Width == 0)
					Cells[i].NearCell[(int)DIRECTION.Left] = OutOfBound;
				else
					Cells[i].NearCell[(int)DIRECTION.Left] = Cells[i - 1];
				// case UpLeft:
				if ( (i < Width) || (i%Width == 0) )
					Cells[i].NearCell[(int)DIRECTION.UpLeft] = OutOfBound;
				else
					Cells[i].NearCell[(int)DIRECTION.UpLeft] = Cells[i - Width - 1];
			}
		}
		public Cell this[int row, int col]
		{
			/*
			CellNetwork CN(w, h); 하고서
			CN[3, 2] 하는 식으로 액세스가 가능해짐.
			*/
			get { return Cells[row * Width + col]; }
			set { Cells[row * Width + col] = value; }
		}

		public void CalculateNextStates(GameOfLife GoL)
		{
			foreach (Cell c in Cells)
			{
				// 만약 c의 livingCellCount가 birth에 있다면, c의 nextState는 1.
				if (GoL.Birth.Contains(c.LivingCellCount))
					c.NextState = 1;
				// 만약 c의 livingCellCount가 survive에 있다면, c의 nextState는 유지.
				else if (GoL.Survive.Contains(c.LivingCellCount))
					c.NextState = c.NowState;
				// 그 외의 경우는 죽는다.
				else
					c.NextState = 0;
			}
		}

		public void UpdateStates()
		{
			foreach (Cell c in Cells)
				c.UpdateState();
		}
	}

	public class Cell
	{
		public Cell() {}

		public Cell[] NearCell = new Cell[8];  // 클래스는 기본적으로 ref 형식. 처음엔 null로 맞춰있다.
		
		private int nowState = 0;
		public int nextState = 0;
		public int NowState { get => nowState; set => nowState = value; }  // 읽기 전용으로 하고 싶은 마음이 굴뚝같지만 GameOfLife 초기화할 때 딱 한 번 접근하기 때문에 그럴 수 없다...
		public int NextState { get => nextState; set => nextState = value; }  // 읽기 전용으로 하고 싶은 마음이 굴뚝같지만 CellNetwork에서 nextState 계산할 때 딱 한 번 접근하기 때문에 그럴 수 없다...
		// 0: dead, 1: alive

		// 이하는 읽기 전용.
		public int LivingCellCount
		{
			get
			{
				int sum = 0;
				foreach (Cell c in NearCell)
				{
					if (c != null)
						sum += c.nowState;
				}
				return sum;
			}
		}

		public void UpdateState()
		{
			nowState = nextState;
		}
	};

}
