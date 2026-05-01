using cli_life;

namespace Tests
{
	public class CellTests
	{
		[Fact]
		public void DetermineNextLiveState_LiveCellWithTwoLiveNeighbors_StaysAlive()
		{
			var cell = new Cell { IsAlive = true };
			cell.neighbors.Add(new Cell { IsAlive = true });
			cell.neighbors.Add(new Cell { IsAlive = true });
			cell.DetermineNextLiveState();
			cell.Advance();
			Assert.True(cell.IsAlive);
		}

		[Fact]
		public void DetermineNextLiveState_LiveCellWithThreeLiveNeighbors_StaysAlive()
		{
			var cell = new Cell { IsAlive = true };
			for (int i = 0; i < 3; i++) cell.neighbors.Add(new Cell { IsAlive = true });
			cell.DetermineNextLiveState();
			cell.Advance();
			Assert.True(cell.IsAlive);
		}

		[Fact]
		public void DetermineNextLiveState_LiveCellWithOneLiveNeighbor_Dies()
		{
			var cell = new Cell { IsAlive = true };
			cell.neighbors.Add(new Cell { IsAlive = true });
			cell.DetermineNextLiveState();
			cell.Advance();
			Assert.False(cell.IsAlive);
		}

		[Fact]
		public void DetermineNextLiveState_DeadCellWithThreeLiveNeighbors_BecomesAlive()
		{
			var cell = new Cell { IsAlive = false };
			for (int i = 0; i < 3; i++) cell.neighbors.Add(new Cell { IsAlive = true });
			cell.DetermineNextLiveState();
			cell.Advance();
			Assert.True(cell.IsAlive);
		}

		[Fact]
		public void DetermineNextLiveState_DeadCellWithTwoLiveNeighbors_StaysDead()
		{
			var cell = new Cell { IsAlive = false };
			for (int i = 0; i < 2; i++) cell.neighbors.Add(new Cell { IsAlive = true });
			cell.DetermineNextLiveState();
			cell.Advance();
			Assert.False(cell.IsAlive);
		}
	}

	public class BoardTests
	{
		[Fact]
		public void Board_CorrectDimensions()
		{
			var board = new Board(100, 100, 10);
			Assert.Equal(10, board.Columns);
			Assert.Equal(10, board.Rows);
		}

		[Fact]
		public void Board_ConnectNeighbors_AllCellsHaveEightNeighbors()
		{
			var board = new Board(100, 100, 10);
			foreach (var cell in board.Cells)
				Assert.Equal(8, cell.neighbors.Count);
		}

		[Fact]
		public void Board_Advance_IncrementsStep()
		{
			var board = new Board(100, 100, 10);
			var initialStep = board.Step;
			board.Advance();
			Assert.Equal(initialStep + 1, board.Step);
		}

		[Fact]
		public void Board_SaveAndLoadState_RestoresCorrectState()
		{
			var board1 = new Board(100, 100, 10);
			board1.Randomize(0.5);
			board1.SaveState("test_state.txt");
			var board2 = new Board(100, 100, 10);
			board2.LoadState("test_state.txt");
			for (int x = 0; x < board1.Columns; x++)
				for (int y = 0; y < board1.Rows; y++)
					Assert.Equal(board1.Cells[x, y].IsAlive, board2.Cells[x, y].IsAlive);
		}
	}

	public class SavedStateTests
	{
		[Fact]
		public void Serialize_Deserialize_RoundTrip()
		{
			var original = new SavedState
			{
				Width = 5,
				Height = 5,
				state = new bool[] { true, false, true, false, true, false, true, false, true, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false }
			};
			var serialized = original.Serialize();
			var deserialized = SavedState.Deserialize(serialized);
			Assert.Equal(original.Width, deserialized.Width);
			Assert.Equal(original.Height, deserialized.Height);
			Assert.Equal(original.state, deserialized.state);
		}
	}

	public class BoardAnalyzerTests
	{
		[Fact]
		public void Analyze_EmptyBoard_ReturnsZero()
		{
			var board = new Board(100, 100, 10);
			var result = BoardAnalyzer.Analize(board);
			Assert.Equal(0, result.liveCells);
			Assert.Equal(0, result.clusters);
		}

		[Fact]
		public void Analyze_SingleLiveCell_ReturnsOneLiveCell()
		{
			var board = new Board(100, 100, 10);
			board.Cells[0, 0].IsAlive = true;
			var result = BoardAnalyzer.Analize(board);
			Assert.Equal(1, result.liveCells);
			Assert.Equal(1, result.clusters);
		}

		[Fact]
		public void Analyze_BlockPattern_DetectsBlock()
		{
			var board = new Board(100, 100, 10);
			board.Cells[0, 0].IsAlive = true;
			board.Cells[1, 0].IsAlive = true;
			board.Cells[0, 1].IsAlive = true;
			board.Cells[1, 1].IsAlive = true;
			var result = BoardAnalyzer.Analize(board);
			Assert.Equal(1, result.figures["block"]);
		}
	}

	public class ExtensionsTests
	{
		[Fact]
		public void Variance_EmptyList_ReturnsZero()
		{
			var list = new List<int>();
			Assert.Equal(0, list.Variance());
		}

		[Fact]
		public void Variance_SingleElement_ReturnsZero()
		{
			var list = new List<int> { 5 };
			Assert.Equal(0, list.Variance());
		}

		[Fact]
		public void Variance_TwoElements_CorrectCalculation()
		{
			var list = new List<int> { 1, 3 };
			Assert.Equal(1, list.Variance());
		}
	}
}
