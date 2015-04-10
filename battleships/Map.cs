// Автор: Павел Егоров
// Дата: 28.12.2015

using System;
using System.Collections.Generic;
using System.Linq;

namespace battleships
{
    public enum CellType
    {
        Empty,
        Ship,
        DeadOrWoundedShip,
        Miss
    }

    public enum ShotEffect
    {
        Miss,
        Wound,
        Kill
    }
    
    public class Ship
    {
        public int Size { get; private set; }
        ///<summary>Направление корабля. True — горизонтальное. False — вертикальное</summary>
        public bool Direction { get; private set; }
        public HashSet<Vector> AliveCells;
        public bool Alive { get { return AliveCells.Any(); } }
        public Vector Location { get; private set; }

        public Ship(Vector location, int size, bool direction)
        {
            Location = location;
            Size = size;
            Direction = direction;
            AliveCells = new HashSet<Vector>(GetShipCells());
        }

        public List<Vector> GetShipCells()
        {
            var shipOrientation = Direction ? new Vector(1, 0) : new Vector(0, 1);
            return Enumerable
                .Range(0, Size)
                .Select(
                    index => shipOrientation
                    .Mult(index)
                    .Add(Location))
                .ToList();
        }
    }

    public class Map
    {
        private CellType[,] GameField { get; set; }
        public Ship[,] ShipsLocationMap { get; set; }
        public List<Ship> Ships { get; private set; }

        public int Width { get; private set; }
        public int Height { get; private set; }

        public Map(int width, int height)
        {
            Width = width;
            Height = height;
            GameField = new CellType[width, height];
            ShipsLocationMap = new Ship[width, height];
            Ships = new List<Ship>();
        }

        public CellType this[Vector point]
        {
            get
            {
                return PointInField(point) ? GameField[point.X, point.Y] : CellType.Empty;
            }
            private set
            {
                if (!PointInField(point))
                    throw new IndexOutOfRangeException(point + " is not in the map borders");
                GameField[point.X, point.Y] = value;
            }
}

        ///<summary>
        ///Помещает корабль длинной sizeShip в точку startCell, смотрящий в направлении shipVector.
        ///Результат true, если удалось поместить, false - если не удалось.
        ///</summary>
        public bool CanSetShip(Vector startCell, int sizeShip, bool shipVector)
        {
            var ship = new Ship(startCell, sizeShip, shipVector);
            var shipCells = ship.GetShipCells();
            if (shipCells
                .SelectMany(GetNeighbours)
                .Any(cell => this[cell] != CellType.Empty)
                ) 
                return false;

            if (!shipCells.All(PointInField)) 
                return false;

            shipCells.ForEach(cell =>
            {
                this[cell] = CellType.Ship;
                ShipsLocationMap[cell.X, cell.Y] = ship;
            });
            Ships.Add(ship);
            return true;
        }

        public ShotEffect GetShotEffect(Vector target)
        {
            bool hit = PointInField(target) && this[target] == CellType.Ship;
            if (hit)
            {
                var ship = ShipsLocationMap[target.X, target.Y];
                ship.AliveCells.Remove(target);
                this[target] = CellType.DeadOrWoundedShip;
                return ship.Alive ? ShotEffect.Wound : ShotEffect.Kill;
            }

            if (this[target] == CellType.Empty) this[target] = CellType.Miss;
                return ShotEffect.Miss;
        }

        public IEnumerable<Vector> GetNeighbours(Vector cell)
        {
            return
                from x in new[] {-1, 0, 1}
                from y in new[] {-1, 0, 1}
                let coordinatesCell = cell.Add(new Vector(x, y))
                where PointInField(coordinatesCell)
                select coordinatesCell;
        }
        public bool PointInField(Vector point)
        {
            return point.X >= 0 && point.X < Width && point.Y >= 0 && point.Y < Height;
        }

        public bool HasAliveShips()
        {
                return Ships.Any(ship => ship.Alive);
        }
    }
}