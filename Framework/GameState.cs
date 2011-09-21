using System;
using System.Collections.Generic;
using System.Linq;

namespace Ants
{
    public class GameState
    {
        public int Width { get; private set; }
        public int Height { get; private set; }

        public int LoadTime { get; private set; }
        public int TurnTime { get; private set; }

        private DateTime turnStart;
        public int TimeRemaining
        {
            get
            {
                TimeSpan timeSpent = DateTime.Now - turnStart;
                return TurnTime - timeSpent.Milliseconds;
            }
        }

        public int ViewRadius2 { get; private set; }
        public int AttackRadius2 { get; private set; }
        public int SpawnRadius2 { get; private set; }

        public List<AntLoc> MyAnts;
        public List<AntLoc> EnemyAnts;
        public List<Location> DeadTiles;
        public List<Location> FoodTiles;

        private Tile[,] map;

        #region Keep Game State

        public GameState(int width, int height,
                          int turntime, int loadtime,
                          int viewradius2, int attackradius2, int spawnradius2)
        {

            Width = width;
            Height = height;

            LoadTime = loadtime;
            TurnTime = turntime;

            ViewRadius2 = viewradius2;
            AttackRadius2 = attackradius2;
            SpawnRadius2 = spawnradius2;

            MyAnts = new List<AntLoc>();
            EnemyAnts = new List<AntLoc>();
            DeadTiles = new List<Location>();
            FoodTiles = new List<Location>();

            map = new Tile[height, width];
            for (int row = 0; row < height; row++)
            {
                for (int col = 0; col < width; col++)
                {
                    map[row, col] = Tile.Land;
                }
            }
        }


        internal void issueOrder(Location loc, Direction direction)
        {
            issueOrder(loc, direction.ToChar());
        }

        internal void issueOrder(Location loc, char direction)
        {
            System.Console.Out.WriteLine("o {0} {1} {2}", loc.Row, loc.Col, direction);
        }

        internal void startNewTurn()
        {
            // start timer
            turnStart = DateTime.Now;

            // clear ant data
            foreach (Location loc in MyAnts)
                map[loc.Row, loc.Col] = Tile.Land;
            foreach (Location loc in EnemyAnts)
                map[loc.Row, loc.Col] = Tile.Land;
            foreach (Location loc in DeadTiles)
                map[loc.Row, loc.Col] = Tile.Land;

            MyAnts.Clear();
            EnemyAnts.Clear();
            DeadTiles.Clear();

            // set all known food to unseen
            foreach (Location loc in FoodTiles)
                map[loc.Row, loc.Col] = Tile.Land;
            FoodTiles.Clear();
        }

        internal void addAnt(int row, int col, int team)
        {
            map[row, col] = Tile.Ant;

            AntLoc ant = new AntLoc(this, row, col, team);
            if (team == 0)
            {
                MyAnts.Add(ant);
            }
            else
            {
                EnemyAnts.Add(ant);
            }
        }

        internal void addFood(int row, int col)
        {
            map[row, col] = Tile.Food;
            FoodTiles.Add(new Location(this, row, col));
        }

        internal void removeFood(int row, int col)
        {
            // an ant could move into a spot where a food just was
            // don't overwrite the space unless it is food
            if (map[row, col] == Tile.Food)
            {
                map[row, col] = Tile.Land;
            }
            FoodTiles.Remove(new Location(this, row, col));
        }

        internal void addWater(int row, int col)
        {
            map[row, col] = Tile.Water;
        }

        internal void deadAnt(int row, int col)
        {
            // food could spawn on a spot where an ant just died
            // don't overwrite the space unless it is land
            if (map[row, col] == Tile.Land)
            {
                map[row, col] = Tile.Dead;
            }

            // but always add to the dead list
            DeadTiles.Add(new Location(this, row, col));
        }

        #endregion

        public bool IsPassable(Location loc)
        {
            // true if not water
            return map[loc.Row, loc.Col] != Tile.Water;
        }

        public bool IsUnoccupied(Location loc)
        {
            // true if no ants are at the location
            return IsPassable(loc) && map[loc.Row, loc.Col] != Tile.Ant;
        }

        public Location Destination(Location loc, Direction direction)
        {
            return Destination(loc, direction.ToChar());
        }

        public Location Destination(Location loc, char direction)
        {
            // calculate a new location given the direction and wrap correctly
            Direction delta = Ants.Aim[direction];

            int row = ( loc.Row + delta.Row ) % Height;
            if (row < 0)
                row += Height; // because the modulo of a negative number is negative

            int col = ( loc.Col + delta.Col ) % Width;
            if (col < 0)
                col += Width;

            return new Location(this, row, col);
        }

        public int GetDistance(Location loc1, Location loc2)
        {
            // calculate the closest distance between two locations
            int d_row = Math.Abs(loc1.Row - loc2.Row);
            d_row = Math.Min(d_row, Height - d_row);

            int d_col = Math.Abs(loc1.Col - loc2.Col);
            d_col = Math.Min(d_col, Width - d_col);

            return d_row + d_col;
        }

        public IEnumerable<Direction> GetDirection(Location loc1, Location loc2)
        {
            // determine the 1 or 2 fastest (closest) directions to reach a location
            List<char> directions = new List<char>();

            if (loc1.Row < loc2.Row)
            {
                if (loc2.Row - loc1.Row >= Height / 2)
                    directions.Add('n');
                if (loc2.Row - loc1.Row <= Height / 2)
                    directions.Add('s');
            }
            if (loc2.Row < loc1.Row)
            {
                if (loc1.Row - loc2.Row >= Height / 2)
                    directions.Add('s');
                if (loc1.Row - loc2.Row <= Height / 2)
                    directions.Add('n');
            }

            if (loc1.Col < loc2.Col)
            {
                if (loc2.Col - loc1.Col >= Width / 2)
                    directions.Add('w');
                if (loc2.Col - loc1.Col <= Width / 2)
                    directions.Add('e');
            }
            if (loc2.Col < loc1.Col)
            {
                if (loc1.Col - loc2.Col >= Width / 2)
                    directions.Add('e');
                if (loc1.Col - loc2.Col <= Width / 2)
                    directions.Add('w');
            }

            return directions.Select(d =>
            {
                switch (d)
                {
                    case 'e':
                        return Direction.East;
                    case 'w':
                        return Direction.West;
                    case 'n':
                        return Direction.North;
                    case 's':
                        return Direction.South;
                    default:
                        throw new ArgumentException();
                }
            });
        }
    }
}

