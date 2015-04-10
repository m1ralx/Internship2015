using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;

namespace MyLittleBot
{
    public class Vector
    {
        public int X { get; private set; }
        public int Y { get; private set; }

        public static Vector Up { get { return new Vector(0, -1); } }
        public static Vector Down { get { return new Vector(0, 1); } }
        public static Vector Right { get { return new Vector(1, 0); } }
        public static Vector Left { get { return new Vector(-1, 0); } }

        public double Length
        {
            get
            {
                return Math.Sqrt(X * X + Y * Y);
            }
        }
        
        public Vector(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static Vector operator+(Vector v1, Vector v2)
        {
            return new Vector(v1.X + v2.X, v1.Y + v2.Y);
        }

        public static Vector operator*(Vector v, int k)
        {
            return new Vector(v.X * k, v.Y * k);
        }

        public override bool Equals(object other)
        {
            Vector otherVector = other as Vector;
            return otherVector != null && otherVector.X == X && otherVector.Y == Y;
        }

        public override int GetHashCode()
        {
            return new Point(X, Y).GetHashCode();
        }
    }

    public enum CellType
    {
        Empty,
        Ship,
        Miss
    }

    public class BattleShipsAi
    {
        private int CurrentTargetSize { get; set; }
        private int Width { get; set; }
        private int Height { get; set; }
        private List<int> ShipsSizes { get; set; }
        private CellType[][] Map { get; set; }
        private Vector Direction { get; set; }
        private Dictionary<Vector, bool> UsedDirections { get; set; }
        private List<Vector> WoundedShip { get; set; }

        public BattleShipsAi(int width, int height, List<int> shipsSizes)
        {
            Width = width;
            Height = height;
            ShipsSizes = shipsSizes;
            Map = new CellType[Width][];
            for (var x = 0; x < Width; x++)
                Map[x] = new CellType[Height];
            Direction = Vector.Down;
            WoundedShip = new List<Vector>();
            ResetDirections();
            CurrentTargetSize = ShipsSizes.Max();
        }

        public void DiagonalShot()
        {
            var nextShot = Enumerable.Range(0, Height)
                .SelectMany(y => Enumerable.Range(0, Width)
                .Select(x => new Vector(x, y)))
                .Where(point => (point.X + point.Y + 1) % CurrentTargetSize == 0)
                .First(IsCorrectShot);
            MakeShot(nextShot);

        }

        private void ResetDirections()
        {
            Direction = Vector.Down;
            UsedDirections = new Dictionary<Vector, bool>();
            UsedDirections[Vector.Right] = true;
            UsedDirections[Vector.Left] = true;
            UsedDirections[Vector.Down] = true;
            UsedDirections[Vector.Up] = true;
        }

        private void MarkOutline()
        {
            WoundedShip
               .Select(GetNeighbours)
               .SelectMany(neighbours => neighbours)
               .Distinct()
               .ToList()
               .ForEach(cell => Map[cell.X][cell.Y] = CellType.Miss);
        }

        private bool TryMakeShotIfMissed()
        {
            Vector nextShot;
            if (WoundedShip.Count > 1)
            {
                nextShot = WoundedShip.First() + (Direction * (-1));
                if (IsCorrectShot(nextShot))
                {
                    MakeShot(nextShot);
                    return true;
                }
            }
            if (WoundedShip.Count == 1)
            {
                UsedDirections[Direction] = false;
                Direction = UsedDirections
                    .First(dir => dir.Value && IsCorrectShot(dir.Key + WoundedShip.First())).Key;
                nextShot = new Vector(WoundedShip.Last().X + Direction.X, WoundedShip.Last().Y + Direction.Y);
                MakeShot(nextShot);
                return true;
            }
            return false;
        }

        public bool ActionOnKill(int[] arguments)
        {
            WoundedShip.Add(new Vector(arguments[0], arguments[1]));
            Map[arguments[0]][arguments[1]] = CellType.Ship;
            MarkOutline();
            ShipsSizes.Remove(WoundedShip.Count);
            WoundedShip = new List<Vector>();
            ResetDirections();
            CurrentTargetSize = ShipsSizes.Max();
            return true;
        }

        public bool ActionOnMiss(int[] arguments)
        {
            var coordinatesOfMiss = new Vector(arguments[0], arguments[1]);
            Map[coordinatesOfMiss.X][coordinatesOfMiss.Y] = CellType.Miss;
            bool shotMade = TryMakeShotIfMissed();
            if (shotMade)
                return false;
            return true;
        }

        public bool ActionOnWound(int[] arguments)
        {
            var coordinatesOfWound = new Vector(arguments[0], arguments[1]);
            if (WoundedShip.Any())
            {
                var allDirections = WoundedShip.Select(point => point * (-1) + coordinatesOfWound).ToList();
                Direction = allDirections.First(point => point.Length == allDirections.Min(p => p.Length));
            }
            WoundedShip.Add(coordinatesOfWound);
            Map[arguments[0]][arguments[1]] = CellType.Ship;
            var nextShot = coordinatesOfWound + Direction;
            if (!IsCorrectShot(nextShot))
            {
                if (WoundedShip.Count > 1)
                    nextShot = WoundedShip[0] * 2 + WoundedShip[1] * (-1);
                else
                {
                    Direction = UsedDirections
                        .First(dir => dir.Value && IsCorrectShot(dir.Key + coordinatesOfWound)).Key;
                    nextShot = coordinatesOfWound + Direction;
                }
            }
            MakeShot(nextShot);
            return false;
        }

        private void MakeShot(Vector shot)
        {
            Console.WriteLine("{0} {1}", shot.X, shot.Y);
        }

        private IEnumerable<Vector> GetNeighbours(Vector cell)
        {
            return
                from x in new[] { -1, 0, 1 }
                from y in new[] { -1, 0, 1 }
                let coordinatesCell = cell + new Vector(x, y)
                where TargetInField(coordinatesCell)
                select coordinatesCell;
        }

        private bool IsCorrectShot(Vector target)
        {
            return TargetInField(target) && IsEmpty(target) &&
                   (ShipCouldBeHere(target, Vector.Right) || ShipCouldBeHere(target, Vector.Up));
        }

        private bool IsEmpty(Vector point)
        {
            return Map[point.X][point.Y] == CellType.Empty;
        }

        private bool TargetInField(Vector point)
        {
            return point.X >= 0 && point.X < Width && point.Y >= 0 && point.Y < Height;
        }

        private int NumberOfEmptyCellsInRow(Vector target, Vector direction)
        {
            var emptyCells = 0;
            var currentCell = new Vector(target.X, target.Y);
            while (true)
            {
                currentCell = currentCell + direction;
                bool isBadShot = !TargetInField(currentCell) || Map[currentCell.X][currentCell.Y] != CellType.Empty;
                if (isBadShot)
                    break;
                emptyCells++;
            }
            return emptyCells;
        }

        private bool ShipCouldBeHere(Vector target, Vector direction)
        {
            if (WoundedShip.Count > 1)
                return true;
            if (WoundedShip.Count == 1)
            {
                direction = target * (-1) + WoundedShip.First();
                target = WoundedShip.First();
            }
            var minSize = ShipsSizes.Min();
            var emptyCellsNumber = NumberOfEmptyCellsInRow(target, direction)
                             + NumberOfEmptyCellsInRow(target, direction * (-1));
            return emptyCellsNumber + 1 >= minSize;
        }
    }

    public class BattleShipsBot
    {
        private static BattleShipsAi Ai { get; set; }
        private static Dictionary<string, Func<int[], bool>> Commands { get; set; }
        static void Main()
        {
            Commands = new Dictionary<string, Func<int[], bool>>
            {
                {"Init", InitializeAi}
            };
            while (true)
            {
                var inputLine = Console.ReadLine();
                if (inputLine == null) 
                    return;
                var command = inputLine.Split().First();
                var arguments = inputLine
                    .Split()
                    .Skip(1)
                    .Select(int.Parse)
                    .ToArray();
                var shotIsNotDone = Commands[command](arguments);
                if (!shotIsNotDone)
                    continue;
                Ai.DiagonalShot();
            }
        }

        private static bool InitializeAi(params int[] initArgs)
        {
            var width = initArgs[0];
            var height = initArgs[1];
            var shipsSizes = initArgs.Skip(2).ToList();
            Ai = new BattleShipsAi(width, height, shipsSizes);
            Commands["Kill"] = Ai.ActionOnKill;
            Commands["Wound"] = Ai.ActionOnWound;
            Commands["Miss"] = Ai.ActionOnMiss;
            return true;
        }
    }
}



// line имеет один из следующих форматов:
// Init <map_width> <map_height> <ship1_size> <ship2_size> ...
// Wound <last_shot_X> <last_shot_Y>
// Kill <last_shot_X> <last_shot_Y>
// Miss <last_shot_X> <last_shot_Y>
// Один экземпляр вашей программы может быть использван для проведения нескольких игр подряд.
// Сообщение Init сигнализирует о том, что началась новая игра.