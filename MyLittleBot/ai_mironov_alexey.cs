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

        public double Lenght
        {
            get
            {
                return Math.Sqrt(X * X + Y * Y);
            }
            private set { Lenght = value; }
        }
        
        public Vector(int x, int y)
        {
            X = x;
            Y = y;
        }
        
        public Vector Add(Vector v)
        {
            return new Vector(v.X + X, v.Y + Y);
        }
        
        public Vector Multiply(int k)
        {
            return new Vector(X * k, Y * k);
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

    public enum Cell
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
        private Cell[][] Map { get; set; }
        private Vector Direction { get; set; }
        private Dictionary<Vector, bool> UsedDirections { get; set; }
        private List<Vector> WoundedShip { get; set; }

        public BattleShipsAi(int width, int height, List<int> shipsSizes)
        {
            Width = width;
            Height = height;
            ShipsSizes = shipsSizes;
            Map = new Cell[Width][];
            for (var x = 0; x < Width; x++)
                Map[x] = new Cell[Height];
            Direction = new Vector(0, 1);
            WoundedShip = new List<Vector>();
            ResetDirections();
            CurrentTargetSize = ShipsSizes.Max();
        }

        public void MakeNextDiagonallyShot()
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
            Direction = new Vector(0, 1);
            UsedDirections = new Dictionary<Vector, bool>();
            UsedDirections[new Vector(1, 0)] = true;
            UsedDirections[new Vector(-1, 0)] = true;
            UsedDirections[new Vector(0, 1)] = true;
            UsedDirections[new Vector(0, -1)] = true;
        }

        private void MarkOutline()
        {
            WoundedShip
               .Select(GetNeighbours)
               .SelectMany(neighbours => neighbours)
               .Distinct()
               .ToList()
               .ForEach(cell => Map[cell.X][cell.Y] = Cell.Miss);
        }

        public bool OnKill(int[] arguments)
        {
            WoundedShip.Add(new Vector(arguments[0], arguments[1]));
            Map[arguments[0]][arguments[1]] = Cell.Ship;
            MarkOutline();
            ShipsSizes.Remove(WoundedShip.Count);
            WoundedShip = new List<Vector>();
            ResetDirections();
            CurrentTargetSize = ShipsSizes.Max();
            return true;
        }

        public bool TryMakeShot()
        {
            Vector nextShot;
            if (WoundedShip.Count > 1)
            {
                nextShot = WoundedShip.First().Add(Direction.Multiply(-1));
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
                    .First(dir => dir.Value && IsCorrectShot(dir.Key.Add(WoundedShip.First()))).Key;
                nextShot = new Vector(WoundedShip.Last().X + Direction.X, WoundedShip.Last().Y + Direction.Y);
                MakeShot(nextShot);
                return true;
            }
            return false;
        }

        public bool OnMiss(int[] arguments)
        {
            var coordinatesOfMiss = new Vector(arguments[0], arguments[1]);
            Map[coordinatesOfMiss.X][coordinatesOfMiss.Y] = Cell.Miss;
            bool shotMade = TryMakeShot();
            if (shotMade)
                return false;
            return true;
        }

        public bool OnWound(int[] arguments)
        {
            var coordinatesOfWound = new Vector(arguments[0], arguments[1]);
            if (WoundedShip.Any())
            {
                var allDirections = WoundedShip.Select(point => point.Multiply(-1).Add(coordinatesOfWound)).ToList();
                Direction = allDirections.First(point => point.Lenght == allDirections.Min(p => p.Lenght));
            }
            WoundedShip.Add(coordinatesOfWound);
            Map[arguments[0]][arguments[1]] = Cell.Ship;
            var nextShot = coordinatesOfWound.Add(Direction);
            if (!IsCorrectShot(nextShot))
            {
                if (WoundedShip.Count > 1)
                    nextShot = WoundedShip[0].Add(WoundedShip[0].Add(WoundedShip[1].Multiply(-1)));
                else
                {
                    Direction = UsedDirections
                        .First(dir => dir.Value && IsCorrectShot(dir.Key.Add(coordinatesOfWound))).Key;
                    nextShot = coordinatesOfWound.Add(Direction);
                }
            }
            MakeShot(nextShot);
            return false;
        }

        public void MakeShot(Vector shot)
        {
            Console.WriteLine("{0} {1}", shot.X, shot.Y);
        }

        public IEnumerable<Vector> GetNeighbours(Vector cell)
        {
            return
                from x in new[] { -1, 0, 1 }
                from y in new[] { -1, 0, 1 }
                let coordinatesCell = cell.Add(new Vector(x, y))
                where TargetInField(coordinatesCell)
                select coordinatesCell;
        }

        public bool IsCorrectShot(Vector target)
        {
            return TargetInField(target) && IsEmpty(target) &&
                   (ShipCouldBeHere(target, new Vector(1, 0)) || ShipCouldBeHere(target, new Vector(0, 1)));
        }

        public bool IsEmpty(Vector point)
        {
            return Map[point.X][point.Y] == Cell.Empty;
        }

        public bool TargetInField(Vector point)
        {
            return point.X >= 0 && point.X < Width && point.Y >= 0 && point.Y < Height;
        }

        private int GetNumberEmptyCellsInRow(Vector target, Vector direction)
        {
            var emptyCells = 0;
            var currentCell = new Vector(target.X, target.Y);
            while (true)
            {
                currentCell = currentCell.Add(direction);
                bool isBadShot = !TargetInField(currentCell) || Map[currentCell.X][currentCell.Y] != Cell.Empty;
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
                direction = target.Multiply(-1).Add(WoundedShip.First());
                target = WoundedShip.First();
            }
            var minSize = ShipsSizes.Min();
            var emptyCellsNumber = GetNumberEmptyCellsInRow(target, direction)
                             + GetNumberEmptyCellsInRow(target, direction.Multiply(-1));
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
                Ai.MakeNextDiagonallyShot();
            }
        }

        private static bool InitializeAi(params int[] initArgs)
        {
            var width = initArgs[0];
            var height = initArgs[1];
            var shipsSizes = initArgs.Skip(2).ToList();
            Ai = new BattleShipsAi(width, height, shipsSizes);
            Commands["Kill"] = Ai.OnKill;
            Commands["Wound"] = Ai.OnWound;
            Commands["Miss"] = Ai.OnMiss;
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