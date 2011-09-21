using System;
using System.Collections.Generic;
using System.Linq;
using Ants;

namespace AntsBot
{
	public class MyBot : Bot 
    {
		public override void doTurn (GameState state) 
        {
			// loop through all my ants and try to give them orders
			foreach (AntLoc ant in state.MyAnts) 
            {
                if (state.TimeRemaining < 10) break;

                if (ant.Target != null)
                {
                    ant.Move();
                }
                else 
                {
                    var min = state.FoodTiles.Select(food => new { 
                        Tile = food,
                        Distance = ant.GetDistance(food)
                    });

                    ant.Target = min.OrderBy(f => f.Distance).First().Tile;
                    ant.Move();
                }
            }
		}
		
		public static void Main (string[] args) {
			new Ants.Ants().playGame(new MyBot());
		}

	}
}