using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ants;
namespace AntsBot
{
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
}

