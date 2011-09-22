using System;
using System.Collections.Generic;
using System.Linq;
using Ants;
using SettlersEngine;
using System.Drawing;
using System.IO;

namespace AntsBot
{
    public class Goal
    {
        public Location StartPoint { get; private set; }
        public Location CurrentPoint { get; set; }

        public Location EndPoint
        {
            get
            {
                return this.StartPath.Last().Location;
            }
        }

        public IEnumerable<Tile> StartPath { get; private set; }

        public Queue<Tile> CurrentPath;

        public Func<GameState, bool> IsTerminated { get; private set; }

        public Goal(Location startPoint, IEnumerable<Tile> path, Func<GameState, bool> terminationFunc)
        {
            this.CurrentPath = new Queue<Tile>(path);
            this.CurrentPath.Dequeue();

            this.StartPath = path;

            this.StartPoint = this.CurrentPoint = startPoint;
            this.IsTerminated = terminationFunc;
        }
    }

	public class MyBot : Bot 
    {
        private HashSet<Location> Destinations;
        private SpatialAStar<Tile, object> astar;

        private List<Goal> Goals;

        private Random rng;

        public MyBot()
        {
            this.Destinations = new HashSet<Location>(new LocationComparer());
            this.Goals = new List<AntsBot.Goal>();
            rng = new Random();
        }

        private double GetSearchRadius(GameState state)
        {
            return Math.Sqrt(state.Width * state.Height) / 15;
        }

        private Goal FindFood(AntLoc ant, GameState state)
        {
            // look for some food
            var searchingspots = state.FoodTiles.Where(f => !(Goals.Count(g => g.EndPoint == f) > 1));
            //searchingspots = searchingspots.Where(f => f.GetDistance(ant) < GetSearchRadius(state)).OrderBy(f => f.GetDistance(ant)).Take(2);
            var antpoint = ant.ToPoint();
            var foodpoints = searchingspots.Select(f => new
            {
                Tile = f,
                Point = f.ToPoint(),
                Path = astar.Search(ant.ToPoint(), f.ToPoint(), new Object()) ?? Enumerable.Empty<Tile>()
            });

            var closest = foodpoints.OrderBy(f => f.Path.Count()).FirstOrDefault();

            if (closest != null)
            {
                var goal = new Goal(closest.Tile, closest.Path, ( g => g.HasFood(closest.Tile) ));
                return goal;
            }

            return null;
        }

        private Goal FindEnemies(AntLoc ant, GameState state)
        {
            return null;
        }

        private Goal SpreadOut(AntLoc ant, GameState state)
        { 
            // try and figure out how close to the edge of vision I am
            var x = ant.Col;
            var y = ant.Row;

            var minX = state.MyAnts.Min(a => a.Col);
            var maxX = state.MyAnts.Max(a => a.Col);
            var minY = state.MyAnts.Min(a => a.Row);
            var maxY = state.MyAnts.Max(a => a.Row);

            var widthModifier = state.Width / 50;
            var heightModifier = state.Height / 50;

            Func<GameState, bool> terminationFunc = (s => true);

            // near the left (ish)
            if (x < ( minX + widthModifier))
            {
                var path = astar.Search(ant.ToPoint(), new Point(x + widthModifier, y), new object());
                if (path != null)
                {
                    return new Goal(ant, path, terminationFunc);
                }
            }
            else if (x > ( maxX - widthModifier ))
            {
                var path = astar.Search(ant.ToPoint(), new Point(x - widthModifier, y), new object());
                if (path != null)
                {
                    return new Goal(ant, path, terminationFunc);
                }
            }
            else if (y < ( maxY + heightModifier ))
            {
                var path = astar.Search(ant.ToPoint(), new Point(x, y + heightModifier), new object());
                if (path != null)
                {
                    return new Goal(ant, path, terminationFunc);
                }
            }
            else if (y > ( maxY - heightModifier ))
            {
                var path = astar.Search(ant.ToPoint(), new Point(x, y - heightModifier), new object());
                if (path != null)
                {
                   return new Goal(ant, path, terminationFunc);
                }
            }

            return null;
        }

        private Goal Panic(AntLoc ant, GameState state)
        {
            // wander aimlessly
            foreach (var d in Ants.Ants.Aim.Values.OrderBy(c => rng.Next()))
            {
                var loc = ant.GetDestination(d);
                if (loc.IsPassable())
                {
                    return new Goal(ant, astar.Search(ant.ToPoint(), loc.ToPoint(), new object()), (s => true));
                }
            }

            return null;
        }

		public override void doTurn (GameState state) 
        {
            this.Destinations.Clear();

            astar = new SpatialAStar<Tile, object>(state.Map);

            foreach (AntLoc ant in state.MyAnts)
            {
                Goal goal = this.Goals.FirstOrDefault(g => g.CurrentPoint.Col == ant.Col && g.CurrentPoint.Row == ant.Row);

                if (goal != null && goal.IsTerminated(state))
                {
                    goal = null;
                }

                if (goal == null)
                {
                    goal = FindFood(ant, state);
                }
                if (goal == null)
                {
                    goal = FindEnemies(ant, state);
                }
                if (goal == null)
                {
                    goal = SpreadOut(ant, state);
                }
                if (goal == null)
                {
                    goal = Panic(ant, state);
                }

                if (goal != null)
                {
                    var loc = goal.CurrentPath.Dequeue();
                    var directions = ant.GetDirections(loc.Location);

                    foreach (var d in directions)
                    {
                        var newLoc = ant.GetDestination(d);
                        if (state.IsUnoccupied(newLoc) && state.IsPassable(newLoc) && !Destinations.Contains(newLoc))
                        {
                            ant.Move(d);
                            Destinations.Add(newLoc);
                            break;
                        }
                    }
                }
            }
		}
		
		public static void Main (string[] args) {
			new Ants.Ants().playGame(new MyBot());
		}

	}
}