using System;
using System.Collections.Generic;
using System.Linq;
using Ants;
using SettlersEngine;
using System.Drawing;
using System.IO;

namespace AntsBot
{
    public enum Strategy
    {
        Fight = 0, // head to enemies
        Scout = 1,  // explore map
        GatherFood = 2, // head to food
        Scatter = 3, // away from teammates
        Condense = 4,  // toward teammates
        Retreat = 5,  // away from enemies
        SpreadOut = 6 // move away from barriers
    }

    public enum Preference
    {
        StronglyEncourage = 0,
        MildlyEncourage = 1,
        Neutral = 2,
        MildlyDiscourage = 3,
        StronglyDiscourage = 4
    }
    public class Goal
    {
        public Location StartPoint { get; private set; }
        public Location CurrentPoint { get; set; }
        public Strategy? CurrentStrategy { get; set; }

        public Location EndPoint
        {
            get
            {
                return this.StartPath.Last().Location;
            }
        }

        public IEnumerable<Tile> StartPath { get; private set; }

        public Queue<Tile> CurrentPath;

        public int CurrentStep;

        public Func<GameState, bool> IsTerminated { get; private set; }

        public bool AntExists;

        public Goal(Location startPoint, IEnumerable<Tile> path, Func<GameState, bool> terminationFunc, Strategy? currentStrategy)
        {
            this.CurrentStep = 0;
            this.AntExists = true;
            this.CurrentPath = new Queue<Tile>(path);
            this.CurrentPath.Dequeue();

            this.StartPath = path;

            this.StartPoint = this.CurrentPoint = startPoint;
            this.IsTerminated = terminationFunc;

            this.CurrentStrategy = currentStrategy;
        }
    }

	public class MyBot : Bot 
    {
        private HashSet<Location> Destinations;
        private SpatialAStar<Tile, object> astar;

        private List<Goal> Goals;

        private Random rng;
        int turn;

        const int STRAT_COUNT = 7;
        const int PREF_COUNT = 6;

        float[] strategies = { 
                               .0f, //fight
                               .0f, //scout
                               1f, //gather
                               .0f, //scatter
                               .0f, //condense
                               .0f, //retreat
                               .0f  // spread out
                             };
        float[] preferenceweight = { 
                                       .01f, //strongly encourage
                                       .005f, // mildly encourage
                                       0f, // neutral
                                       -.005f, //mildly discourage
                                       -.01f  // mildly encourage
                                   };

        StreamWriter log;

        public MyBot()
        {
            log = File.CreateText("bot.log");
            this.Destinations = new HashSet<Location>(new LocationComparer());
            this.Goals = new List<AntsBot.Goal>();
            rng = new Random((int)DateTime.Now.TimeOfDay.TotalMilliseconds % 265407);
            turn = 0;
        }

        private void Log(string line)
        {
            log.WriteLine(line);
            log.Flush();
        }


        private double GetSearchRadius(GameState state)
        {
            return Math.Sqrt(state.Width * state.Height) / 15;
        }

        private LinkedList<Tile> GetPath(Location loc1, Location loc2, GameState state)
        {
            if (Math.Abs(loc1.Col - loc2.Col) > state.ViewRadius2 || Math.Abs(loc1.Row - loc2.Row) > state.ViewRadius2)
            {
                Log("closest is a map wrap - use basic path function");
                return state.GetPath(loc1, loc2);

            }
            else
            {
                Log("use astar path function");
                return astar.Search(loc1.ToPoint(), loc2.ToPoint(), new Object());
            }
        }
        private Goal GatherFood(AntLoc ant, GameState state)
        {
            if (!state.FoodTiles.Any())
                return Scout(ant, state);

            Log("foodtiles: " + state.FoodTiles.Count);
            // look for some food
            var searchingspots = state.FoodTiles.Where(f => Goals.Count(g => g.EndPoint.Equals(f))==0);
            Log("searchingspots: " + searchingspots.Count());
            //searchingspots = searchingspots.Where(f => f.GetDistance(ant) < GetSearchRadius(state)).OrderBy(f => f.GetDistance(ant)).Take(2);
            var antpoint = ant.ToPoint();
            var foodpoints = searchingspots.Select(f => new
            {
                Tile = f,
                Point = f.ToPoint(),
                Distance = state.GetDistance(ant,f)//astar.Search(ant.ToPoint(), f.ToPoint(), new Object()) ?? Enumerable.Empty<Tile>()
            });

            var closest = foodpoints.OrderBy(f => f.Distance).FirstOrDefault();

            if (closest != null)
            {
                Log("closest food:" + closest.Tile);
                
                return new Goal(closest.Tile, GetPath(ant,closest.Tile,state), (g => !g.HasFood(closest.Tile)), Strategy.GatherFood);
            }

            return SpreadOut(ant, state);
        }

        private Goal Fight(AntLoc ant, GameState state)
        {
            if (!state.EnemyAnts.Any())
                return Scout(ant, state);

            // look for some food
            var searchingspots = state.EnemyAnts.Where(f => Goals.Count(g => g.EndPoint.Equals(f)) <= 1);
            //searchingspots = searchingspots.Where(f => f.GetDistance(ant) < GetSearchRadius(state)).OrderBy(f => f.GetDistance(ant)).Take(2);
            var antpoint = ant.ToPoint();
            var enemypoints = searchingspots.Select(f => new
            {
                Tile = f,
                Point = f.ToPoint(),
                Distance = state.GetDistance(ant,f)//astar.Search(ant.ToPoint(), f.ToPoint(), new Object()) ?? Enumerable.Empty<Tile>()
            });

            var closest = enemypoints.OrderBy(f => f.Distance).FirstOrDefault();

            if (closest != null)
            {
                var goal = new Goal(closest.Tile, GetPath(ant, closest.Tile, state), (g => g.IsUnoccupied(closest.Tile)), Strategy.Fight);
                return goal;
            }

            return SpreadOut(ant, state);
        }

        private Goal Scout(AntLoc ant, GameState state)
        {
            return SpreadOut(ant, state);
        }

        private Goal Condense(AntLoc ant, GameState state)
        {
            if (state.MyAnts.Count <= 1)
                return Scout(ant, state);

            // look for friend
            var searchingspots = state.MyAnts.Where(f => Goals.Count(g => g.EndPoint.Equals(f)) <= 1);
            //searchingspots = searchingspots.Where(f => f.GetDistance(ant) < GetSearchRadius(state)).OrderBy(f => f.GetDistance(ant)).Take(2);
            var antpoint = ant.ToPoint();
            var friendpoints = searchingspots.Select(f => new
            {
                Tile = f,
                Point = f.ToPoint(),
                Distance = state.GetDistance(ant, f)//astar.Search(ant.ToPoint(), f.ToPoint(), new Object()) ?? Enumerable.Empty<Tile>()
            });

            var closest = friendpoints.OrderBy(f => f.Distance).FirstOrDefault();

            if (closest != null)
            {
                var goal = new Goal(closest.Tile, GetPath(ant, closest.Tile, state), (g => g.IsUnoccupied(closest.Tile)), Strategy.Condense);
                return goal;
            }

            return SpreadOut(ant, state);
        }

        private Goal Retreat(AntLoc ant, GameState state)
        {
            return Condense(ant, state);
        }

        private Goal SpreadOut(AntLoc ant, GameState state)
        {
            if (state.TimeRemaining < 10)
                return Scatter(ant, state);
            // try and figure out how close to the edge of vision I am
            var x = ant.Col;
            var y = ant.Row;

            var minX = state.MyAnts.Min(a => a.Col);
            var maxX = state.MyAnts.Max(a => a.Col);
            var minY = state.MyAnts.Min(a => a.Row);
            var maxY = state.MyAnts.Max(a => a.Row);

            var widthModifier = state.Width / 30;
            if (widthModifier <= 1)
                widthModifier = 2;
            var heightModifier = state.Height / 30;
            if (heightModifier <= 1)
                heightModifier = 2;

            Func<GameState, bool> terminationFunc = (s => false);

            Log("spread out: minx:" + minX + " miny:" + minY + " maxx:" + maxX + " maxy:" + maxY + " widthmod:" + widthModifier + " heightmod:" + heightModifier);
            // near the left (ish)
            if (x < ( minX + widthModifier))
            {
                var loc = new Location(state,y,x-widthModifier); //move left
                Log("left, path :" + ant + " " + loc);
                var path = GetPath(ant, loc, state);
                if (path != null)
                {
                    return new Goal(ant, path, terminationFunc, Strategy.SpreadOut);
                }
            }
            else if (x > ( maxX - widthModifier ))
            {
                var loc = new Location(state, y,x + widthModifier); //move right
                Log("right, path :" + ant + " " + loc);
                var path = GetPath(ant, loc, state);
                if (path != null)
                {
                    return new Goal(ant, path, terminationFunc, Strategy.SpreadOut);
                }
            }
            else if (y < ( maxY + heightModifier ))
            {
                var loc = new Location(state,  y - heightModifier,x); //move up
                Log("top, path :" + ant + " " + loc);
                var path = GetPath(ant, loc, state);
                if (path != null)
                {
                    return new Goal(ant, path, terminationFunc, Strategy.SpreadOut);
                }
            }
            else if (y > ( maxY - heightModifier ))
            {
                var loc = new Location(state,  y + heightModifier,x); // move down
                Log("bottom, path :" + ant + " " + loc);
                var path = GetPath(ant, loc, state);
                if (path != null)
                {
                    return new Goal(ant, path, terminationFunc, Strategy.SpreadOut);
                }
            }

            return Scatter(ant,state);
        }

        private Goal Scatter(AntLoc ant, GameState state)
        {
            // wander aimlessly
            foreach (var d in Ants.Ants.Aim.Values.OrderBy(c => rng.Next()))
            {
                var loc = ant.GetDestination(d);
                if (loc.IsPassable())
                {

                    return new Goal(ant, GetPath(ant, loc, state), (s => state.FoodTiles.Any()), Strategy.Scatter);
                }
            }

            return null;
        }

		public override void doTurn (GameState state)
        {
            try
            {
                if (state.TimeRemaining < 5)
                    return;

                if (turn == 0)
                {
                    Log("my team: player " + (state.MyAnts.First().Team + 1));
                    Log("map " + state.Width+"x"+state.Height);
                    Log("turn time " + state.TurnTime);
                }
                Log("");
                Log("Turn " + (turn++));
                Log("My Ants: " + state.MyAnts.Count);

                SetTurnStrategy(state);

                string stratstring = "";

                foreach (var v in Enum.GetValues(typeof(Strategy)))
                {
                    stratstring += Enum.GetName(typeof(Strategy), v) + ":" + strategies[(int)v] + ";";
                }

                Log("Strategy: " + stratstring);

                this.Destinations.Clear();

                astar = new SpatialAStar<Tile, object>(state.Map);

                foreach (var goal in Goals)
                    goal.AntExists = false; //need to check these each turn and clean up if ant gone

                foreach (AntLoc ant in state.MyAnts)
                {
                    Log("ant: " + ant);
                    Goal goal = this.Goals.FirstOrDefault(g => g.CurrentPoint.Equals(ant));
                    

                    if (goal != null && (goal.CurrentPath.Count==0 || goal.IsTerminated(state)))
                    {
                        if (goal.CurrentPath.Count == 0)
                            Log("ant goal complete");
                        else
                            Log("ant goal terminated");
                        Goals.Remove(goal);
                        goal = null;
                    }
                    if (goal != null)
                    {
                        goal.AntExists = true;
                        Log("ant existing goal: " + String.Join(";", goal.CurrentPath.Select(p => p.Location.ToString())) + " " + Enum.GetName(typeof(Strategy), goal.CurrentStrategy));
                    }
                    else
                    {
                        if (state.TimeRemaining < 50) // choose a fast strategy
                        {
                            Log("short on time ("+state.TimeRemaining+") - choose scatter");
                            goal = Scatter(ant, state);
                        }
                        else
                        {

                            goal = ChooseStrategy(ant, state);
                            if (!goal.CurrentPath.Any())
                            {
                                Log("bad goal/path - scatter instead");
                                goal = Scatter(ant, state);
                            }
                        }
                        Goals.Add(goal);
                        Log("new ant goal: " + String.Join(";", goal.CurrentPath.Select(p => p.Location.ToString())) + " " + Enum.GetName(typeof(Strategy), goal.CurrentStrategy));

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
                                goal.CurrentPoint = newLoc;
                                goal.CurrentStep++;
                                Destinations.Add(newLoc);
                                break;
                            }
                        }
                    }
                }
                int removed = Goals.RemoveAll(g => !g.AntExists);//clean up goals for missing ants

                Log("ant goals(" + Goals.Count + ", "+removed+" removed): " + String.Join(";", Goals.Select(g => "["+Enum.GetName(typeof(Strategy),g.CurrentStrategy)+"]"+g.CurrentPoint.ToString()+"->"+g.EndPoint.ToString())));
                if (removed > 0) // losing fights - condense
                {
                    AlterStrategy(Strategy.Condense, Preference.StronglyEncourage);
                    AlterStrategy(Strategy.Condense, Preference.StronglyEncourage);
                    AlterStrategy(Strategy.Condense, Preference.StronglyEncourage);
                }
            }
            catch (Exception exc)
            {
                Log(exc.Message);
                Log(exc.StackTrace);
            }
		}
        private Goal ChooseStrategy(AntLoc ant, GameState state)
        {
            Strategy? strat = null;
            float rnd = rng.Next(0, (int)(strategies.Sum()*100)) * .01f;

            for (int i = 1; i < STRAT_COUNT; i++)
            {
                if (rnd < strategies.Take(i).Sum())
                {
                    strat =  (Strategy)(i - 1);
                    break;
                }
            }

            if(strat==null)
                strat = (Strategy)(STRAT_COUNT - 1);

            Goal goal = null;

            if (strat == Strategy.GatherFood)
                goal = GatherFood(ant, state);
            else if (strat == Strategy.Fight)
                goal = Fight(ant, state);
            else if (strat == Strategy.SpreadOut)
                goal = SpreadOut(ant, state);
            else if (strat == Strategy.Scatter)
                goal = Scatter(ant, state);
            else if (strat == Strategy.Scout)
                goal = Scout(ant, state);
            else if (strat == Strategy.Condense)
                goal = Condense(ant, state);
            else if (strat == Strategy.Retreat)
                goal = Retreat(ant, state);

            if (goal == null)
            {
                goal = GatherFood(ant, state);
            }
            if (goal == null)
            {
                goal = Scout(ant, state);
            }
            if (goal == null)
            {
                goal = Fight(ant, state);
            }
            if (goal == null)
            {
                goal = SpreadOut(ant, state);
            }
            if (goal == null)
            {
                goal = Scatter(ant, state);
            }
            if (goal == null)
            {
                goal = Condense(ant, state);
            }
            if (goal == null)
            {
                goal = Retreat(ant, state);
            }

            return goal;
                
        }

        private void AlterStrategy(Strategy strat, Preference pref)
        {
            var newval = strategies[(int)strat] + preferenceweight[(int)pref];
            if (newval >= 0)
            {
                strategies[(int)strat] = newval;
            }
            else
            {
                strategies[(int)strat] = 0;
            }
            /*
            var goalweight = strategies[(int)strat] + preferenceweight[(int)pref];
            if (goalweight > 0 && goalweight < 1)
            {

                float otherchange = -preferenceweight[(int)pref] / (STRAT_COUNT - 1);

                Log("strat: " + Enum.GetName(typeof(Strategy), strat) + " " + Enum.GetName(typeof(Preference), pref));


                for (int i = 0; i < STRAT_COUNT; i++)// assign the others first
                {
                    if (i != (int)strat && strategies[i] + otherchange > 0 && strategies[i] + otherchange < 1)
                    {
                        strategies[i] += otherchange;
                    }
                }

                strategies[(int)strat] += (1f - strategies.Sum());

            }
             * */
        }

        const float CLOSE_FACTOR = 2f;
        private void SetTurnStrategy(GameState state)
        {
            foreach (var a in state.MyAnts)
            {
                Log("ant loc: " + a);
            }
            Location mycenter = new Location(state,(int)Math.Round(state.MyAnts.Average(a => a.Row)), (int)Math.Round(state.MyAnts.Average(a => a.Col)));
            Log("ants center:" + mycenter);

            int totalfriends = state.MyAnts.Count;
            int closefriends = state.MyAnts.Count(a => state.GetDistance(a, mycenter) <= state.AttackRadius2 * CLOSE_FACTOR);
            Log("friends:" + totalfriends + "," + closefriends);

            int totalenemies = state.EnemyAnts.Count;
            int closeenemies = state.EnemyAnts.Count(a => state.GetDistance(a, mycenter) <= state.AttackRadius2 * CLOSE_FACTOR);
            Log("enemies:" + totalenemies + "," + closeenemies);


            int totalfood = state.FoodTiles.Count;
            int closefood = state.FoodTiles.Count(f => state.GetDistance(f, mycenter) <= state.AttackRadius2 * CLOSE_FACTOR);
            Log("food:" + totalfood + "," + closefood);


            if (closefood > 0 || totalfood > 0)
                AlterStrategy(Strategy.GatherFood, Preference.MildlyEncourage);
            else
            {
                AlterStrategy(Strategy.Scout, Preference.StronglyEncourage);
                AlterStrategy(Strategy.Scout, Preference.StronglyEncourage);
                AlterStrategy(Strategy.Scatter, Preference.StronglyEncourage);
                AlterStrategy(Strategy.SpreadOut, Preference.StronglyEncourage);
                AlterStrategy(Strategy.SpreadOut, Preference.StronglyEncourage);
            }

            if (closeenemies > closefriends)
            {
                AlterStrategy(Strategy.Fight, Preference.StronglyDiscourage);
                AlterStrategy(Strategy.Condense, Preference.MildlyEncourage);
                AlterStrategy(Strategy.Scatter, Preference.MildlyDiscourage);
                AlterStrategy(Strategy.Retreat, Preference.StronglyEncourage);
            }
            else if (closeenemies == closefriends && closeenemies > 0)
            {
                AlterStrategy(Strategy.Condense, Preference.StronglyEncourage);
                AlterStrategy(Strategy.Scatter, Preference.StronglyDiscourage);
                AlterStrategy(Strategy.Fight, Preference.MildlyEncourage);
            }
            else  //if (closeenemies < closefriends)
            {
                if (closeenemies > 0)
                {
                    AlterStrategy(Strategy.Fight, Preference.StronglyEncourage);
                    AlterStrategy(Strategy.Retreat, Preference.StronglyDiscourage);
                    AlterStrategy(Strategy.Condense, Preference.MildlyEncourage);
                    AlterStrategy(Strategy.Scatter, Preference.StronglyDiscourage);
                }
                else
                {
                    AlterStrategy(Strategy.Scout, Preference.MildlyEncourage);
                    AlterStrategy(Strategy.Condense, Preference.MildlyDiscourage);
                    AlterStrategy(Strategy.Scatter, Preference.MildlyEncourage);
                }
            }

            if (totalfriends < totalenemies && totalenemies > 0)
            {
                if (closefood > closeenemies)
                {
                    AlterStrategy(Strategy.GatherFood, Preference.StronglyEncourage);
                    AlterStrategy(Strategy.Fight, Preference.MildlyDiscourage);
                    AlterStrategy(Strategy.Retreat, Preference.MildlyDiscourage);
                    AlterStrategy(Strategy.Scatter, Preference.MildlyEncourage);
                }
                else
                {
                    AlterStrategy(Strategy.Scout, Preference.StronglyEncourage);
                    AlterStrategy(Strategy.Fight, Preference.MildlyDiscourage);
                    AlterStrategy(Strategy.Scatter, Preference.MildlyEncourage);
                }
            }
            else  //if (totalfriends > totalenemies)
            {
                if (totalenemies > 0)
                {
                    AlterStrategy(Strategy.Fight, Preference.StronglyEncourage);
                }
                AlterStrategy(Strategy.Scout, Preference.MildlyEncourage);
                AlterStrategy(Strategy.Scatter, Preference.MildlyEncourage);
            }

        }

		public static void Main (string[] args) {
			new Ants.Ants().playGame(new MyBot());
		}

	}
}