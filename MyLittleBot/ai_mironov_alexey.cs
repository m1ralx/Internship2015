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
        
        public Vector Mul(int k)
        {
            return new Vector(X * k, Y * k);
        }
        
        public override bool Equals(object other)
        {
            if(!(other.GetType() == typeof(Vector)))
                return false;
            var v = (Vector)other;
            return v.X == X && v.Y == Y;
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

    public class Bot
    {
        private static int CurrentTargetSize { get; set; }
        private static int Width { get; set; }
        private static int Height { get; set; }
        private static List<int> ShipsSize { get; set; }
        private static Cell[][] Map { get; set; }
        private static Vector Direction { get; set; }
        private static Dictionary<Vector, bool> UsedDirections { get; set; }
        private static List<Vector> WoundedShip { get; set; }

        static void Main()
        {
            while (true)
            {
                var line = Console.ReadLine();
                if (line == null) 
                    return;
                var command = line.Split().First();
                var arguments = line
                    .Split()
                    .Skip(1)
                    .Select(int.Parse)
                    .ToArray();

                Vector nextShot;
                switch (command)
                {
                    case "Init":
                        Init(arguments);
                        break;
                    case "Wound":
                        nextShot = OnWound(arguments);
                        MakeShot(nextShot);
                        continue;
                    case "Kill":
                        OnKill(arguments);
                        break;
                    case "Miss":
                        var missCoo = new Vector(arguments[0], arguments[1]);
                        Map[missCoo.X][missCoo.Y] = Cell.Miss;
                        var shotMade = TryMakeShot();
                        if (shotMade)
                            continue;
                        break;
                }
                CurrentTargetSize = ShipsSize.Max();
                nextShot = GetNextShot(); 
                MakeShot(nextShot);
            }
        }

        private static int GetCountEmptyCells(Vector target, Vector direction)
        {
            var emptyCells = 0;
            var currentCell = new Vector(target.X, target.Y);
            while (true)
            {
                currentCell = currentCell.Add(direction);
                if (!TargetInField(currentCell) || Map[currentCell.X][currentCell.Y] != Cell.Empty)
                    break;
                emptyCells++;
            }
            return emptyCells;
        }

        private static bool PossibleShip(Vector target, Vector direction)
        {
            if (WoundedShip.Count > 1)
                return true;
            if (WoundedShip.Count == 1)
            {
                direction = target.Mul(-1).Add(WoundedShip.First());
                target = WoundedShip.First();
            }
            var minSize = ShipsSize.Min();
            var emptyCells = GetCountEmptyCells(target, direction)
                + GetCountEmptyCells(target, direction.Mul(-1));
            return emptyCells + 1 >= minSize;
        }

        private static void Init(params int[] initArgs)
        {
            Width = initArgs[0];
            Height = initArgs[1];
            ShipsSize = initArgs.Skip(2).ToList();
            Map = new Cell[Width][];
            for (var x = 0; x < Width; x++)
                Map[x] = new Cell[Height];
            Direction = new Vector(0, 1);
            WoundedShip = new List<Vector>();
            ResetDirections();
        }

        private static Vector GetNextShot()
        {
            return Enumerable.Range(0, Height)
                .SelectMany(y => Enumerable.Range(0, Width)
                    .Select(x => new Vector(x, y)))
                .Where(point => (point.X + point.Y + 1) % CurrentTargetSize == 0)
                .First(IsCorrectShot);

        }

        private static void ResetDirections()
        {
            Direction = new Vector(0, 1);
            UsedDirections = new Dictionary<Vector, bool>();
            UsedDirections[new Vector(1, 0)] = true;
            UsedDirections[new Vector(-1, 0)] = true;
            UsedDirections[new Vector(0, 1)] = true;
            UsedDirections[new Vector(0, -1)] = true;

        }

        private static void MarkOutline()
        {
            WoundedShip
                .Select(GetNeighbours)
                .SelectMany(neighbours => neighbours)
                .Distinct()
                .ToList()
                .ForEach(cell => Map[cell.X][cell.Y] = Cell.Miss);
        }

        private static void OnKill(int[] arguments)
        {
            WoundedShip.Add(new Vector(arguments[0], arguments[1]));
            Map[arguments[0]][arguments[1]] = Cell.Ship;
            MarkOutline();
            ShipsSize.Remove(WoundedShip.Count);
            WoundedShip = new List<Vector>();
            ResetDirections();
        }

        private static bool TryMakeShot()
        {
            Vector nextShot;
            if (WoundedShip.Count > 1)
            {
                nextShot = WoundedShip.First().Add(Direction.Mul(-1));
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

        private static Vector OnWound(int[] arguments)
        {
            var woundCoo = new Vector(arguments[0], arguments[1]);
            if (WoundedShip.Any())
            {
                var allDirections = WoundedShip.Select(point => point.Mul(-1).Add(woundCoo)).ToList();
                Direction = allDirections.First(point => point.Lenght == allDirections.Min(p => p.Lenght));
            }
            WoundedShip.Add(woundCoo);
            Map[arguments[0]][arguments[1]] = Cell.Ship;
            var nextShot = woundCoo.Add(Direction);
            if (!IsCorrectShot(nextShot))
            {
                if (WoundedShip.Count > 1)
                    nextShot = WoundedShip[0].Add(WoundedShip[0].Add(WoundedShip[1].Mul(-1)));
                else
                {
                    Direction = UsedDirections
                        .First(dir => dir.Value && IsCorrectShot(dir.Key.Add(woundCoo))).Key;
                    nextShot = woundCoo.Add(Direction);
                }
            }
            return nextShot;
        }

        private static void MakeShot(Vector shot)
        {
            Console.WriteLine("{0} {1}", shot.X, shot.Y);
        }

        public static IEnumerable<Vector> GetNeighbours(Vector cell)
        {
            return
                from x in new[] { -1, 0, 1 }
                from y in new[] { -1, 0, 1 }
                let coordinatesCell = cell.Add(new Vector(x, y))
                where TargetInField(coordinatesCell)
                select coordinatesCell;
        }

        public static bool IsCorrectShot(Vector target)
        {
            return TargetInField(target) && IsEmpty(target) && 
                (PossibleShip(target, new Vector(1, 0)) || PossibleShip(target, new Vector(0, 1)));
        }

        public static bool IsEmpty(Vector point)
        {
            return Map[point.X][point.Y] == Cell.Empty;
        }
        
        public static bool TargetInField(Vector point)
        {
            return point.X >= 0 && point.X < Width && point.Y >= 0 && point.Y < Height;
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