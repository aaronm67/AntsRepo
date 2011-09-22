using System;
using System.Collections.Generic;

namespace Ants
{
    public class Direction
    {
        public int Row { get; private set; }
        public int Col { get; private set; }

        public static readonly Direction North = new Direction(-1, 0);
        public static readonly Direction South = new Direction(1, 0);
        public static readonly Direction West = new Direction(0, -1);
        public static readonly Direction East = new Direction(0, 1);

        public Direction(int row, int col)
        {
            this.Row = row;
            this.Col = col;
        }

        public char ToChar()
        {
            if (this == Direction.North)
            {
                return 'n';
            }
            if (this == Direction.South)
            {
                return 's';
            }
            if (this == Direction.East)
            {
                return 'e';
            }
            if (this == Direction.West)
            {
                return 'w';
            }

            throw new ArgumentException();
        }
    }

    public class Location
    {
        public int Row { get; private set; }
        public int Col { get; private set; }
        public GameState State { get; private set; }

        public Location(GameState state, int row, int col)
        {
            this.State = state;
            this.Row = row;
            this.Col = col;
        }

        public double GetDistance(Location loc)
        {
            return this.State.GetDistance(this, loc);
        }

        public Location GetDestination(Direction dir)
        {
            return this.State.GetDestination(this, dir);
        }

        public IEnumerable<Direction> GetDirections(Location loc)
        {
            return this.State.GetDirection(this, loc);
        }

        public bool HasFood()
        {
            return this.State.HasFood(this);
        }

        public bool IsUnoccupied()
        {
            return this.State.IsUnoccupied(this);
        }

        public System.Drawing.Point ToPoint()
        {
            return new System.Drawing.Point(this.Row, this.Col);
        }
    }

    public class AntLoc : Location
    {
        public int Team { get; private set; }
<<<<<<< HEAD
=======
        public bool Seeking { get; set; }
>>>>>>> 41b20b4f2581aeab1014090398f434c82d0aee9d

        public AntLoc(GameState state, int row, int col, int team)
            : base(state, row, col)
        {
            this.Team = team;
<<<<<<< HEAD
=======
            this.Seeking = false;
>>>>>>> 41b20b4f2581aeab1014090398f434c82d0aee9d
        }

        public bool IsValidMove(Direction direction)
        {
            return State.IsPassable(State.GetDestination(this, direction));
        }

        public bool Move(Direction direction)
        {
            if (this.IsValidMove(direction))
            {
                this.State.issueOrder(this, direction);
                return true;
            }

            return false;
        }
    }

    public class LocationComparer : IEqualityComparer<Location>
    {
        public bool Equals(Location loc1, Location loc2)
        {
            return ( loc1.Row == loc2.Row && loc1.Col == loc2.Col );
        }

        public int GetHashCode(Location loc)
        {
            return loc.Row * int.MaxValue + loc.Col;
        }
    }
}

