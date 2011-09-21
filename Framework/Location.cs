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
            return State.GetDistance(this, loc);
        }
    }

    public class AntLoc : Location
    {
        public int Team { get; private set; }

        public Location Target { get; set; }

        public AntLoc(GameState state, int row, int col, int team)
            : base(state, row, col)
        {
            this.Team = team;
        }

        public void Move()
        {
            if (this.Target != null)
            {
                foreach (var d in State.GetDirection(this, this.Target))
                {
                    if (this.IsValidMove(d))
                    {
                        this.Move(d);
                        break;
                    }
                }
            }
        }

        public bool IsValidMove(Direction direction)
        {
            return State.IsPassable(State.Destination(this, direction));
        }

        public void Move(Direction direction)
        {
            this.State.issueOrder(this, direction);
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

