using System;
using System.Collections.Generic;
using System.Linq;
using Ants;

namespace AntsBot
{
	public class MyBot : Bot 
    {
        private HashSet<Location> Destinations;
        private Random rng;

        public MyBot()
        {
            this.Destinations = new HashSet<Location>(new LocationComparer());
            rng = new Random();
        }

		public override void doTurn (GameState state) 
        {
            foreach (AntLoc ant in state.MyAnts)
            {
                Location closestFood = null;
                int closestDist = int.MaxValue;

                foreach (Location food in state.FoodTiles)
                {
                    int dist = state.GetDistance(ant, food);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closestFood = food;
                    }
                }

                IEnumerable<Direction> directions = Enumerable.Empty<Direction>();
                if (closestFood != null)
                    directions = ant.GetDirections(closestFood);
                else 
                    directions = Ants.Ants.Aim.Values.OrderBy(c => rng.Next());

                bool moved = false;
                foreach (var d in directions)
                {
                    var newLoc = ant.GetDestination(d);
                    if (state.IsUnoccupied(newLoc) && state.IsPassable(newLoc))
                    {
                        ant.Move(d);
                        moved = true;
                        break;
                    }                                
                }

                var randomdirs = Ants.Ants.Aim.Values.OrderBy(c => rng.Next());
                foreach (var dir in randomdirs)
                {
                    var newLoc = ant.GetDestination(dir);
                    if (state.IsUnoccupied(newLoc) && state.IsPassable(newLoc))
                    {
                        ant.Move(dir);
                    }
                }
            }
		}
		
		public static void Main (string[] args) {
			new Ants.Ants().playGame(new MyBot());
		}

	}
}