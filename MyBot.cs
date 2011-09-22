using System;
using System.Collections.Generic;
using System.Linq;
using Ants;
using SettlersEngine;
using System.Drawing;

namespace AntsBot
{
	public class MyBot : Bot 
    {
        private HashSet<Location> FoodSpots;
        private HashSet<Location> Destinations;

        private Random rng;

        public MyBot()
        {
            this.Destinations = new HashSet<Location>(new LocationComparer());
            this.FoodSpots = new HashSet<Location>(new LocationComparer());
            rng = new Random();
        }

        private double GetSearchRadius(GameState state)
        {
            return Math.Sqrt(state.Width * state.Height) / 15;
        }

		public override void doTurn (GameState state) 
        {
            this.Destinations.Clear();
            this.FoodSpots.Clear();

            var astar = new SpatialAStar<Tile, object>(state.Map);

            foreach (AntLoc ant in state.MyAnts)
            {
                if (state.TimeRemaining < 10)
                {
                    break;
                }

                // performance checks
                var searchingspots = state.FoodTiles.Where(f => !( FoodSpots.Count(s => s == f) > 1 ));
                searchingspots = searchingspots.Where(f => f.GetDistance(ant) < GetSearchRadius(state)).OrderBy(f => f.GetDistance(ant)).Take(2);

                var directions = Enumerable.Empty<Direction>();

                var antpoint = ant.ToPoint();
                var foodpoints = searchingspots.Where(f => !(FoodSpots.Count(s => s == f) > 1)).Select(f => new {
                    Tile = f,
                    Point = f.ToPoint(), 
                    Path = astar.Search(ant.ToPoint(), f.ToPoint(), new Object()) ?? Enumerable.Empty<Tile>()
                });

                var closest = foodpoints.OrderBy(f => f.Path.Count()).Where(f => !(FoodSpots.Count(s => f.Tile == s) > 1)).FirstOrDefault();
                // found something to look for
                if (closest != null)
                {
                    var point = closest.Path.Skip(1).FirstOrDefault();
                    FoodSpots.Add(closest.Tile);

                    if (point != null)
                        directions = ant.GetDirections(point.Location);
                    else if (closest != null)
                        directions = ant.GetDirections(closest.Tile);
                }

                if (state.TimeRemaining < 10)
                {
                    break;
                }

                // we found no food...lets kill some dudes...but only if I have nothing else to do.
                //if (!directions.Any())
                //{
                //    var enemies = state.EnemyAnts;
                //    if (enemies.Any())
                //    {
                //        // lets make sure i'm not running across the map for some guys
                //        enemies = enemies.OrderBy(e => ant.GetDistance(e)).ToList();
                //        var enemynum1 = enemies.FirstOrDefault();
                //        if (enemynum1 != null)
                //        {
                //            var path = astar.Search(ant.ToPoint(), enemynum1.ToPoint(), new object()).First();
                //            if (path != null) 
                //                directions = ant.GetDirections(path.Location);

                //        }
                //    }
                //}
                
                if (!directions.Any())
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

                    // near the left (ish)
                    if (x < ( minX + widthModifier))
                    {
                        var search = astar.Search(ant.ToPoint(), new Point(x + widthModifier, y), new object());
                        if (search != null)
                        {
                            directions = ant.GetDirections(search.First().Location);
                        }
                    }
                    else if (x > ( maxX - widthModifier ))
                    {
                        if (state.TimeRemaining < 10)
                        {
                            break;
                        }

                        var search = astar.Search(ant.ToPoint(), new Point(x - widthModifier, y), new object());
                        if (search != null)
                        {
                            directions = ant.GetDirections(search.First().Location);
                        }
                    }
                    else if (y < ( maxY + heightModifier ))
                    {
                        if (state.TimeRemaining < 10)
                        {
                            break;
                        }

                        var search = astar.Search(ant.ToPoint(), new Point(x, y + heightModifier), new object());
                        if (search != null)
                        {
                            directions = ant.GetDirections(search.First().Location);
                        }
                    }
                    else if (y > ( maxY - heightModifier ))
                    {
                        if (state.TimeRemaining < 10)
                        {
                            break;
                        }

                        var search = astar.Search(ant.ToPoint(), new Point(x, y - heightModifier), new object());
                        if (search != null)
                        {
                            directions = ant.GetDirections(search.First().Location);
                        }
                    }
                }

                if (state.TimeRemaining < 10)
                {
                    break;
                }
                
				// wander aimlessly
                if (!directions.Any())
                {
                    directions = Ants.Ants.Aim.Values.OrderBy(c => rng.Next());
                }

                if (state.TimeRemaining < 10)
                {
                    break;
                }

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
		
		public static void Main (string[] args) {
			new Ants.Ants().playGame(new MyBot());
		}

	}
}