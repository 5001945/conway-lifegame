#include <iostream>
#include <fstream>
#include <sstream>

#include <string>
#include <vector>
#include <array>

#include <algorithm>

#include <cstdlib>

using namespace std;


vector<string> getFile(string);
vector<int> stringParser(string);

enum DIRECTION
{
	DIR_U, DIR_UR, DIR_R, DIR_DR, DIR_D, DIR_DL, DIR_L, DIR_UL
};

struct Rulebox
{
	// rawRule에서, B3/S23과 23/3은 같은 의미이다.
	string rawRule = "B3/S23";
	vector<int> birth;
	vector<int> survive;
};

class Cell
{
public:
	Cell() {};

	int livingCellCount = 0;
	int nowState = 0, nextState = 0; // 0: dead, 1: alive
	array<Cell*, 8> nearCell;
};
Cell emptyCell;

class GameOfLife
{
public:
	GameOfLife(string filename);

	Rulebox rule;
	int width, height;
	vector<Cell> cells;
	int turnCount = 0;

	void NextTurn();
	void Calculate();
	void printBoard();

private:
	void UpdateInfo();
};
GameOfLife::GameOfLife(string filename)
{
	vector<string> stringBundle = getFile(filename);
	vector<int> intBundle;
	// rule을 짓는다.
	rule.rawRule = stringBundle[0];
	// 임시
	this->rule.birth = vector<int>{3};
	this->rule.survive = vector<int>{2, 3};

	// 월드의 크기를 얻는다.
	intBundle = stringParser(stringBundle[1]);
	this->width = intBundle[0];
	this->height = intBundle[1];
	/*
	// 임시
	this->width = 20;
	this->height = 20;
	*/

	// cells에 본격적으로 Cell을 집어넣는다. 
	this->cells.assign(this->width * this->height, emptyCell);

	// 각 Cell의 8방향에 있는 Cell들을 연결시킨다.
	for (int i = 0; i < width*height; i++)
	{
		// direction은 시계방향으로 0부터 7까지이다.
		// index는 왼쪽 위로 갈 수록 작아진다.

		// case DIR_U:
		if (i < width)
			cells[i].nearCell[DIR_U] = &emptyCell;
		else
			cells[i].nearCell[DIR_U] = &cells[i - width];
		// case DIR_UR:
		if ( (i < width) || (i%width == width-1) )
			cells[i].nearCell[DIR_UR] = &emptyCell;
		else
			cells[i].nearCell[DIR_UR] = &cells[i - width + 1];
		// case DIR_R:
		if (i%width == width-1)
			cells[i].nearCell[DIR_R] = &emptyCell;
		else
			cells[i].nearCell[DIR_R] = &cells[i + 1];
		// case DIR_DR:
		if ( (i >= width*(height-1)) || (i%width == width-1) )
			cells[i].nearCell[DIR_DR] = &emptyCell;
		else
			cells[i].nearCell[DIR_DR] = &cells[i + width + 1];
		// case DIR_D:
		if (i >= width*(height-1))
			cells[i].nearCell[DIR_D] = &emptyCell;
		else
			cells[i].nearCell[DIR_D] = &cells[i + width];
		// case DIR_DL:
		if ( (i >= width*(height-1)) || (i%width == 0) )
			cells[i].nearCell[DIR_DL] = &emptyCell;
		else
			cells[i].nearCell[DIR_DL] = &cells[i + width - 1];
		// case DIR_L:
		if (i%width == 0)
			cells[i].nearCell[DIR_L] = &emptyCell;
		else
			cells[i].nearCell[DIR_L] = &cells[i - 1];
		// case DIR_UL:
		if ( (i < width) || (i%width == 0) )
			cells[i].nearCell[DIR_UL] = &emptyCell;
		else
			cells[i].nearCell[DIR_UL] = &cells[i - width - 1];
	}

	// 초기 살아있는 세포의 위치를 얻는다.
	for (int i = 2; i < stringBundle.size(); i++)
	{
		intBundle = stringParser(stringBundle[i]);
		int r = intBundle[0], c = intBundle[1];
		cells[r*width + c].nowState = 1;
	}
	/*
	// 임시
	this->cells[6*width + 12].nowState = 1;
	this->cells[7*width + 11].nowState = 1;
	this->cells[7*width + 12].nowState = 1;
	this->cells[7*width + 13].nowState = 1;
	this->cells[8*width + 12].nowState = 1;
	*/
}
void GameOfLife::NextTurn()
{
	this->Calculate();
	this->UpdateInfo();
	turnCount++;
}
void GameOfLife::Calculate()
{
	// 모든 셀에 대하여, livingCellCount를 0으로 초기화시킨다.
	for (auto &c: cells)
		c.livingCellCount = 0;

	// 모든 살아있는 셀에 대하여, 각 셀에 연결된 셀에 살아있다는 정보를 보낸다. 단, emptyCell에는 보내지 않는다.
	for (auto &c: cells)
	{
		if (c.nowState == 1)
		{
			for (auto &ncp: c.nearCell)
			{
				if (ncp != &emptyCell)
					ncp->livingCellCount++;
			}
		}
	}
	// 모든 셀에 대하여, livingCellCount를 분석해 nextState을 구한다.
	for (auto &c: cells)
	{
		// 만약 c의 livingCellCount가 birth에 있다면, c의 nextState는 1.
		if (find(this->rule.birth.begin(), this->rule.birth.end(), c.livingCellCount) != this->rule.birth.end())
			c.nextState = 1;
		// 만약 c의 livingCellCount가 survive에 있다면, c의 nextState는 유지.
		else if (find(this->rule.survive.begin(), this->rule.survive.end(), c.livingCellCount) != this->rule.survive.end())
			c.nextState = c.nowState;
		else
			c.nextState = 0;
	}
}
void GameOfLife::UpdateInfo()
{
	// 모든 셀에 대하여, nowState를 최신화시키고 nextState는 건들지 말고, livingCellCount를 0으로 초기화시킨다.
	for (auto &c: cells)
	{
		c.nowState = c.nextState;
		c.livingCellCount = 0;
	}
}
void GameOfLife::printBoard()
{
//	system("cls");  // for Windows
	system("clear");  // for Linux
	for (int r = 0; r < height; r++)
	{
		for (int c = 0; c < width; c++)
		{
			if (cells[r*width + c].nowState == 1)
				printf("■");
			else
				printf("□");
		}
		printf("\n");
	}
	printf("Turn : %d\n", turnCount);
	printf("\n");
}


vector<string> getFile(string filename)
{
	ifstream readFile;
	readFile.open(filename.c_str());
	vector<string> stringBundle;

	if (readFile.is_open())
	{
		while (!readFile.eof())
		{
			string str;
			getline(readFile, str);
			if ( (str[0] != '#') && (!str.empty()) )
				stringBundle.push_back(str);
		}
		readFile.close();
	}
	return stringBundle;
}

vector<int> stringParser(string str)
{
	stringstream ss;
	vector<int> intBundle;
	/* Storing the whole string into string stream */
    ss << str; 
  
    /* Running loop till the end of the stream */
    int found;
    while (!ss.eof()) { 
	    string temp;
  
        /* extracting word by word from stream */
        ss >> temp; 
  
        /* Checking the given word is integer or not */
        if (stringstream(temp) >> found) 
			intBundle.push_back(found);
    }
	return intBundle;
}


int main(int argc, char **argv)
{
	string filename;
	if (argc == 1)
		filename = "init_LWSS.txt";
	else if (argc == 2)
		filename = argv[1];
	else
		return 0;
	GameOfLife gol(filename);
	gol.printBoard();
	/*
	gol.Calculate();
	cout << gol.cells[2*20 + 3].nextState << endl;
	gol.NextTurn();
	cout << gol.cells[2*20 + 3].nowState << endl;
	cout << gol.cells[2*20 + 3].nextState << endl;
	*/
	
	for (int i = 0; i < 30; i++)
	{
		getchar();
		gol.NextTurn();
		gol.printBoard();
	}
	
	return 0;
}
