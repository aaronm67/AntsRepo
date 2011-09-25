using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

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

        public Tile[,] Map;

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

            Map = new Tile[height, width];
            for (int row = 0; row < height; row++)
            {
                for (int col = 0; col < width; col++)
                {
                    Map[row, col] = new Tile(TileType.Land, new Location(this, row, col));
                }
            }
        }

        internal void issueOrder(Location loc, Direction direction)
        {
            issueOrder(loc, direction.ToChar());
        }

        internal void issueOrder(Location loc, char direction)
        {
            var cmd = String.Format("o {0} {1} {2}", loc.Row, loc.Col, direction);
            System.Console.Out.WriteLine(cmd);
        }

        internal void startNewTurn()
        {
            // start timer
            turnStart = DateTime.Now;

            // clear ant data
            foreach (Location loc in MyAnts)
                Map[loc.Row, loc.Col] = new Tile(TileType.Land, loc);
            foreach (Location loc in EnemyAnts)
                Map[loc.Row, loc.Col] = new Tile(TileType.Land, loc);
            foreach (Location loc in DeadTiles)
                Map[loc.Row, loc.Col] = new Tile(TileType.Land, loc);

            MyAnts.Clear();
            EnemyAnts.Clear();
            DeadTiles.Clear();

            // set all known food to unseen
            foreach (Location loc in FoodTiles)
                Map[loc.Row, loc.Col] = new Tile(TileType.Land, loc);
            FoodTiles.Clear();
        }

        internal void addAnt(int row, int col, int team)
        {            
            AntLoc ant = new AntLoc(this, row, col, team);
            Map[row, col] = new Tile(TileType.Ant, ant);
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
            var loc = new Location(this, row, col);
            Map[row, col] = new Tile(TileType.Food, loc);
            FoodTiles.Add(loc);
        }

        internal void removeFood(int row, int col)
        {
            // an ant could move into a spot where a food just was
            // don't overwrite the space unless it is food
            var loc = new Location(this, row, col);
            if (Map[row, col].Type == TileType.Food)
            {
                Map[row, col] = new Tile(TileType.Land, loc);
            }
            FoodTiles.Remove(loc);
        }

        internal void addWater(int row, int col)
        {
            Map[row, col] = new Tile(TileType.Water, new Location(this, row, col));
        }

        internal void deadAnt(int row, int col)
        {
            // food could spawn on a spot where an ant just died
            // don't overwrite the space unless it is land
            var loc = new Location(this, row, col);
            if (Map[row, col].Type == TileType.Land)
            {
                Map[row, col] = new Tile(TileType.Dead, loc);
            }

            // but always add to the dead list
            DeadTiles.Add(loc);
        }

        #endregion

        public bool IsPassable(Location loc)
        {
            // true if not water
            return Map[loc.Row, loc.Col].Type != TileType.Water;
        }

        public bool IsUnoccupied(Location loc)
        {
            // true if no ants are at the location
            return IsPassable(loc) && Map[loc.Row, loc.Col].Type != TileType.Ant;
        }

        public Location GetDestination(Location loc, Direction direction)
        {
            return GetDestination(loc, direction.ToChar());
        }

        public bool HasFood(Location l)
        {
            return FoodTiles.Any(f => f.Col == l.Col && f.Row == l.Row);
        }

        public Location GetDestination(Location loc, char direction)
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

        public LinkedList<Tile> GetPath(Location loc1, Location loc2)
        {
            var curloc = loc1;
            var ret = new LinkedList<Tile>();
            var directions = GetDirection(loc1, loc2);

            foreach (var d in directions)
            {
                curloc = GetDestination(curloc,d);
                ret.AddLast(new Tile(Map[curloc.Row, curloc.Col].Type, curloc));
            }

            return ret;
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

