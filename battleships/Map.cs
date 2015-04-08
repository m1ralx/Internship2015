// Автор: Павел Егоров
// Дата: 28.12.2015

using System;
using System.Collections.Generic;
using System.Linq;

namespace battleships
{
    public enum CellTypes
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
                .Select(index => shipOrientation
                    .Mult(index)
                    .Add(Location)
                    ).ToList();
        }
    }

    public class Map
    {
        private static CellTypes[,] _gameField;
        public static Ship[,] ShipsLocationMap;

        public List<Ship> Ships = new List<Ship>();

        public int Width { get; private set; }
        public int Height { get; private set; }

        public Map(int width, int height)
        {
            Width = width;
            Height = height;
            _gameField = new CellTypes[width, height];
            ShipsLocationMap = new Ship[width, height];
        }

        public CellTypes this[Vector p]
        {
            get
            {
                return PointInField(p) ? _gameField[p.X, p.Y] : CellTypes.Empty;
            }
            private set
            {
                if (!PointInField(p))
                    throw new IndexOutOfRangeException(p + " is not in the map borders");
                _gameField[p.X, p.Y] = value;
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
                .Any(cell => this[cell] != CellTypes.Empty)
                ) 
                return false;

            if (!shipCells.All(PointInField)) 
                return false;

            shipCells.ForEach(cell =>
            {
                this[cell] = CellTypes.Ship;
                ShipsLocationMap[cell.X, cell.Y] = ship;
            });
            Ships.Add(ship);
            return true;
        }

        public ShotEffect GetShotEffect(Vector target)
        {
            var hit = PointInField(target) && this[target] == CellTypes.Ship;
            if (hit)
            {
                var ship = ShipsLocationMap[target.X, target.Y];
                ship.AliveCells.Remove(target);
                this[target] = CellTypes.DeadOrWoundedShip;
                return ship.Alive ? ShotEffect.Wound : ShotEffect.Kill;
            }

            if (this[target] == CellTypes.Empty) this[target] = CellTypes.Miss;
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