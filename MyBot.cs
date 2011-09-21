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
                searchingspots = searchingspots.Where(f => f.GetDistance(ant) < 10);

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

                // we found no food...lets kill some dudes...but only if I have a lot of em.
                if (!directions.Any())
                {

                }


                // wander aimlessly
                if (!directions.Any())
                {
                    directions = Ants.Ants.Aim.Values.OrderBy(c => rng.Next());
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