using System;
using System.Linq;
using NLog;

namespace battleships
{
	public class ShotInfo
	{
		public ShotEffect Hit;
		public Vector Target;
	}

	public class Game
	{
		private static readonly Logger log = LogManager.GetCurrentClassLogger();
		private readonly Ai ai;

		public Game(Map map, Ai ai)
		{
			Map = map;
			this.ai = ai;
			TurnsCount = 0;
			BadShots = 0;
		}

		public Vector LastTarget { get; private set; }
		public int TurnsCount { get; private set; }
		//<summary>���������� ��������� "������" �����</summary>
		public int BadShots { get; private set; }
		public Map Map { get; private set; }
		public ShotInfo LastShotInfo { get; private set; }
		public bool AiCrashed { get; private set; }
		public Exception LastError { get; private set; }

		public bool IsOver()
		{
			return !Map.HasAliveShips() || AiCrashed;
		}

		public void MakeStep()
		{
			if (IsOver()) throw new InvalidOperationException("Game is Over");
			if (!UpdateLastTarget()) return;
			if (IsBadShot(LastTarget)) BadShots++;
			var hit = Map.GetShotEffect(LastTarget);
			LastShotInfo = new ShotInfo {Target = LastTarget, Hit = hit};
			if (hit == ShotEffect.Miss)
				TurnsCount++;
		}

		private bool UpdateLastTarget()
		{
			try
			{
				LastTarget = LastTarget == null
					? ai.Init(Map.Width, Map.Height, Map.Ships.Select(s => s.Size).ToArray())
					: ai.GetNextShot(LastShotInfo.Target, LastShotInfo.Hit);
				return true;
			}
			catch (Exception e)
			{
				AiCrashed = true;
				log.Info("Ai {0} crashed", ai.Name);
				log.Error(e);
				LastError = e;
				return false;
			}
		}

		private bool IsBadShot(Vector target)
		{
			var cellWasHitAlready = Map[target] != CellTypes.Empty && Map[target] != CellTypes.Ship;
			var cellIsNearDestroyedShip = Map.GetNeighbours(target).Any(c => Map.ShipsLocationMap[c.X, c.Y] != null && !Map.ShipsLocationMap[c.X, c.Y].Alive);
			var diagonals = new[] { new Vector(-1, -1), new Vector(-1, 1), new Vector(1, -1), new Vector(1, 1) };
			var cellHaveWoundedDiagonalNeighbour = diagonals.Any(d => Map[target.Add(d)] == CellTypes.DeadOrWoundedShip);
			return cellWasHitAlready || cellIsNearDestroyedShip || cellHaveWoundedDiagonalNeighbour;
		}
	}
}