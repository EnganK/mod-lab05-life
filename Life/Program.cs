using ScottPlot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace cli_life
{
	public class Cell
	{
		public bool IsAlive;
		public readonly List<Cell> neighbors = new();
		private bool IsAliveNext;

		public void DetermineNextLiveState()
		{
			int liveNeighbors = neighbors.Count(x => x.IsAlive);
			IsAliveNext = IsAlive ? liveNeighbors == 2 || liveNeighbors == 3 : liveNeighbors == 3;
		}

		public void Advance()
		{
			IsAlive = IsAliveNext;
		}
	}

	public class Board
	{
		public readonly Cell[,] Cells;
		public readonly int CellSize;
		public int Columns => Cells.GetLength(0);
		public int Rows => Cells.GetLength(1);
		public int Width => Columns * CellSize;
		public int Height => Rows * CellSize;
		public int Step { get; private set; } = 0;

		public Board(int width, int height, int cellSize, double liveDensity = 0.1)
		{
			CellSize = cellSize;
			Cells = new Cell[width / cellSize, height / cellSize];
			for (int x = 0; x < Columns; x++)
				for (int y = 0; y < Rows; y++)
					Cells[x, y] = new Cell();
			ConnectNeighbors();
		}

		readonly Random rand = new Random();

		public void Randomize(double liveDensity)
		{
			foreach (var cell in Cells)
				cell.IsAlive = rand.NextDouble() < liveDensity;
		}

		public void Advance()
		{
			foreach (var cell in Cells)
				cell.DetermineNextLiveState();
			foreach (var cell in Cells)
				cell.Advance();
			Step++;
		}

		private void ConnectNeighbors()
		{
			for (int x = 0; x < Columns; x++)
			{
				for (int y = 0; y < Rows; y++)
				{
					int xL = (x > 0) ? x - 1 : Columns - 1;
					int xR = (x < Columns - 1) ? x + 1 : 0;
					int yT = (y > 0) ? y - 1 : Rows - 1;
					int yB = (y < Rows - 1) ? y + 1 : 0;

					Cells[x, y].neighbors.Add(Cells[xL, yT]);
					Cells[x, y].neighbors.Add(Cells[x, yT]);
					Cells[x, y].neighbors.Add(Cells[xR, yT]);
					Cells[x, y].neighbors.Add(Cells[xL, y]);
					Cells[x, y].neighbors.Add(Cells[xR, y]);
					Cells[x, y].neighbors.Add(Cells[xL, yB]);
					Cells[x, y].neighbors.Add(Cells[x, yB]);
					Cells[x, y].neighbors.Add(Cells[xR, yB]);
				}
			}
		}

		public void SaveState(string filePath)
		{
			var savedState = new SavedState(this);
			File.WriteAllText(filePath, savedState.Serialize());
		}

		public void LoadState(string filePath)
		{
			if (!File.Exists(filePath)) return;

			var savedString = File.ReadAllText(filePath);
			var savedState = SavedState.Deserialize(savedString);

			savedState?.LoadToBoard(this);
		}
	}
	public class SavedState
	{
		public bool[] state;
		public int Width;
		public int Height;

		public SavedState() { }

		public SavedState(Board board)
		{
			Width = board.Columns;
			Height = board.Rows;
			state = new bool[Width * Height];
			for (int x = 0; x < Width; x++)
				for (int y = 0; y < Height; y++)
					state[x + y * Width] = board.Cells[x, y].IsAlive;
		}

		public void LoadToBoard(Board board)
		{
			if (state.Length == board.Columns * board.Rows)
				for (int x = 0; x < board.Columns; x++)
					for (int y = 0; y < board.Rows; y++)
						board.Cells[x, y].IsAlive = state[x + y * board.Columns];
		}

		public string Serialize()
		{
			var sb = new System.Text.StringBuilder();
			sb.AppendLine($"{Width}x{Height}");
			for (int y = 0; y < Height; y++)
			{
				for (int x = 0; x < Width; x++)
					sb.Append(state[x + y * Width] ? '#' : '_');
				sb.AppendLine();
			}
			return sb.ToString().TrimEnd();
		}

		public static SavedState Deserialize(string data)
		{
			var lines = data.Split('\n');
			var dims = lines[0].Split('x');
			var w = int.Parse(dims[0]);
			var h = int.Parse(dims[1]);
			var savedState = new SavedState { Width = w, Height = h, state = new bool[w * h] };
			for (int y = 0; y < h; y++)
			{
				var row = lines[y + 1];
				for (int x = 0; x < w; x++)
					savedState.state[x + y * w] = row[x] == '#';
			}
			return savedState;
		}
	}
	public class Config
	{
		public int Width { get; set; } = 50;
		public int Height { get; set; } = 20;
		public int CellSize { get; set; } = 1;
		public double LiveDensity { get; set; } = 0.5;
		public int UpdateDelayMs { get; set; } = 1000;
		public string SaveFile { get; set; } = "life_state.txt";
		public int StepsForStability { get; set; } = 20;
		public bool ShowGraphics { get; set; } = true;
	}

	class Program
	{
		static Board board;
		static Config config;
		static int lastLiveCount = -1;
		static int stableSteps = 0;

		static void LoadConfig()
		{
			var configPath = "config.json";
			if (File.Exists(configPath))
			{
				var json = File.ReadAllText(configPath);
				config = JsonSerializer.Deserialize<Config>(json) ?? new Config();
			}
			else
			{
				config = new Config();
				File.WriteAllText(configPath, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));
			}
		}

		static void Reset()
		{
			board = new Board(config.Width, config.Height, config.CellSize, config.LiveDensity);
			lastLiveCount = -1;
			stableSteps = 0;
		}

		static void Render()
		{
			Console.Clear();
			for (int row = 0; row < board.Rows; row++)
			{
				for (int col = 0; col < board.Columns; col++)
				{
					var cell = board.Cells[col, row];
					Console.Write(cell.IsAlive ? '#' : ' ');
				}
				Console.WriteLine();
			}
		}

		static void Main(string[] args)
		{
			LoadConfig();
			Reset();
			SelectPreset();
			Render();
			Thread.Sleep(config.UpdateDelayMs);

			while (true)
			{
				board.Advance();
				var stats = BoardAnalyzer.Analize(board);
				var currentLiveCount = stats.liveCells;

				if (currentLiveCount == lastLiveCount)
				{
					stableSteps++;
				}
				else
				{
					stableSteps = 0;
					lastLiveCount = currentLiveCount;
				}

				if (config.ShowGraphics)
				{
					Render();
					board.SaveState(config.SaveFile);
					Console.WriteLine($"{board.Step}: {stats.liveCells}, {stats.clusters}");
					foreach (var kvp in stats.figures) Console.WriteLine($"{kvp.Key}: {kvp.Value}");
				}

				if (stableSteps >= config.StepsForStability)
				{
					Render();
					board.SaveState(config.SaveFile);
					Console.WriteLine($"{board.Step}: {stats.liveCells}, {stats.clusters}");
					foreach (var kvp in stats.figures) Console.WriteLine($"{kvp.Key}: {kvp.Value}");
					break;
				}

				if (config.ShowGraphics) Thread.Sleep(config.UpdateDelayMs);
			}
		}

		static void SelectPreset()
		{
			Console.WriteLine("Выберите пресет:\n0: Загрузить последнее сохранение\n1: Случайно\n2: Глайдерное ружьё\n3: Queen Bee Shuttle\n4: Жёлудь\n5: Стабильный\n6: Исследование стабильности");
			while (true)
			{
				switch (Console.ReadLine())
				{
					case "0": board.LoadState(config.SaveFile); return;
					case "1": board.Randomize(config.LiveDensity); return;
					case "2": board.LoadState("preset_gun.txt"); return;
					case "3": board.LoadState("preset_queenBeeShuttle.txt"); return;
					case "4": board.LoadState("preset_acorn.txt"); return;
					case "5": board.LoadState("preset_stable.txt"); return;
					case "6":
						RunStabilityAnalysis();
						return;
					default: Console.WriteLine("Некорректный ввод"); break;
				}
			}
		}

		static void RunStabilityAnalysis()
		{
			var results = new Dictionary<double, List<int>>();

			for(double density = 0.1; density <= 0.9; density += 0.05)
			{
				results[density] = new List<int>();
				for (int attempt = 0; attempt < 50; attempt++)
				{
					board = new Board(config.Width, config.Height, config.CellSize, density);
					board.Randomize(density);

					int steps = 0;
					int lastLiveCount = -1;
					int stableSteps = 0;

					while (stableSteps < config.StepsForStability && steps < 1000)
					{
						board.Advance();
						var currentLiveCount = board.Cells.Cast<Cell>().Count(c => c.IsAlive);

						if (currentLiveCount == lastLiveCount)
							stableSteps++;
						else
							stableSteps = 0;

						lastLiveCount = currentLiveCount;
						steps++;
					}

					results[density].Add(steps);
				}
			}

			SaveAnalysisResults(results);
			PlotResults(results);
		}

		static void SaveAnalysisResults(Dictionary<double, List<int>> results)
		{
			Directory.CreateDirectory("Data");
			var sbRaw = new StringBuilder();
			sbRaw.AppendLine("Density\tAttempt\tSteps");

			foreach (var kvp in results)
			{
				double density = kvp.Key;
				for (int i = 0; i < kvp.Value.Count; i++)
					sbRaw.AppendLine($"{density}\t{i + 1}\t{kvp.Value[i]}");
			}

			File.WriteAllText("Data/data_raw.txt", sbRaw.ToString());

			var sb = new StringBuilder();
			sb.AppendLine("Density\tAverageSteps");

			foreach (var kvp in results)
			{
				double density = kvp.Key;
				double avgSteps = kvp.Value.Average();
				sb.AppendLine($"{density}\t{avgSteps}");
			}

			File.WriteAllText("Data/data.txt", sb.ToString());
		}

		static void PlotResults(Dictionary<double, List<int>> results)
		{
			var plt = new Plot();

			var densities = results.Keys.OrderBy(x => x).ToArray();
			var avgSteps = densities.Select(d => results[d].Average()).ToArray();
			var stdDev = densities.Select(d => Math.Sqrt(results[d].Variance())).ToArray();

			plt.Add.Scatter(densities, avgSteps);
			plt.Add.ErrorBar(densities, avgSteps, stdDev);

			plt.Title("Переход в стабильную фазу");
			plt.XLabel("Начальная плотность");
			plt.YLabel("Среднее число поколений до стабильности");

			Directory.CreateDirectory("Data");
			plt.SavePng("Data/plot.png", 600, 400);
		}


	}
	public static class Extensions
	{
		public static double Variance(this IEnumerable<int> source)
		{
			if (!source.Any()) return 0;
			double avg = source.Average();
			return source.Average(x => Math.Pow(x - avg, 2));
		}
	}
	public static class BoardAnalyzer
	{
		static readonly (string name, bool[,] mask)[] patterns =
		{
			("block",   new bool[2,2]
			{
				{true,true},	//	##
				{true,true}}	//	##
			),
			("beehive", new bool[4,3]
			{
				{false,true,false},	//	 #
				{true,false,true},	//	# #
				{true,false,true},	//	# #
				{false,true,false}	//	 #
			}),
			("loaf",    new bool[4,4]
			{
				{false,true,true,false},	//	 ## 
				{true,false,false,true},	//	#  #
				{false,true,false,true},	//	 # #
				{false,false,true,false}	//	  #
			}),
			("boat",    new bool[3,3]
			{
				{true,true,false},		//	## 
				{true,false, true },	//	# #
				{false, true,false}		//	 #
			}),
			("blinker", new bool[1,3]
			{
				{true, true, true},	//	###
			}),
			("tub",    new bool[3,3]
			{
				{false,true,false},	//	 #
				{true,false,true},	//	# # 
				{false,true,false}	//	 #  
			})
		};

		public static (int liveCells, int clusters, Dictionary<string, int> figures) Analize(Board b)
		{
			bool[,] live = new bool[b.Columns, b.Rows];
			for (int x = 0; x < b.Columns; x++)
				for (int y = 0; y < b.Rows; y++)
					live[x, y] = b.Cells[x, y].IsAlive;

			int liveCells = 0, clusters = 0;
			var figures = new Dictionary<string, int>();
			foreach (var p in patterns.Select(t => t.name))
				figures[p] = 0;

			bool[,] used = new bool[b.Columns, b.Rows];

			for (int x = 0; x < b.Columns; x++)
				for (int y = 0; y < b.Rows; y++)
					if (live[x, y])
					{
						liveCells++;
						if (used[x, y]) continue;
						clusters++;
						var cc = Flood(x, y, live, used, b.Columns, b.Rows);
						foreach (var (name, mask) in patterns)
							if (Match(cc, mask, live))
							{
								figures[name]++;
								break;
							}
					}
			return (liveCells, clusters, figures);
		}

		static List<(int x, int y)> Flood(int sx, int sy, bool[,] live, bool[,] used, int w, int h)
		{
			var q = new Queue<(int, int)>();
			var cc = new List<(int, int)>();
			q.Enqueue((sx, sy));
			used[sx, sy] = true;
			while (q.Count > 0)
			{
				var (x, y) = q.Dequeue();
				cc.Add((x, y));
				foreach (var (dx, dy) in new[] { (-1, -1), (-1, 0), (-1, 1), (0, -1), (0, 1), (1, -1), (1, 0), (1, 1) })
				{
					int nx = x + dx, ny = y + dy; 
					if (nx < 0) nx = w - 1;
					if (nx >= w) nx = 0; 
					if (ny < 0) ny = h - 1;
					if (ny >= h) ny = 0;
					if (live[nx, ny] && !used[nx, ny]) 
					{
						used[nx, ny] = true;
						q.Enqueue((nx, ny));
					}
				}
			}
			return cc;
		}

		static bool Match(List<(int x, int y)> cc, bool[,] mask, bool[,] live)
		{
			int mh = mask.GetLength(0), mw = mask.GetLength(1);
			for (int rot = 0; rot < 4; rot++)
			{
				var mm = Transform(mask, rot);
				int minx = cc.Min(t => t.x), miny = cc.Min(t => t.y);
				bool ok = true;
				for (int dy = 0; dy < mm.GetLength(0) && ok; dy++)
					for (int dx = 0; dx < mm.GetLength(1); dx++)
					{
						int tx = minx + dx, ty = miny + dy;
						if (tx >= live.GetLength(0))
							tx -= live.GetLength(0);
						if (ty >= live.GetLength(1))
							ty -= live.GetLength(1);
						if (mm[dy, dx] != live[tx, ty])
						{
							ok = false;
							break;
						}
					}
				if (ok) return true;
			}
			return false;
		}

		static bool[,] Transform(bool[,] mask, int rot)
		{
			int w = mask.GetLength(0), h = mask.GetLength(1);
			bool[,] result = rot % 2 == 0 ? new bool[w, h] : new bool[h, w];
			for (int x = 0; x < w; x++)
				for (int y = 0; y < h; y++)
				{
					int nx = x, ny = y;
					switch (rot)
					{
						case 0: break;
						case 1: (nx, ny) = (y, w - 1 - x); break;
						case 2: (nx, ny) = (w - 1 - nx, h - 1 - ny); break;
						case 3: (nx, ny) = (h - 1 - y, x); break;
					}
					result[nx, ny] = mask[x, y];
				}
			return result;
		}
	}
}
