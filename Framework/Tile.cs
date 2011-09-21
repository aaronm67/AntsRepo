using System;
using SettlersEngine;

namespace Ants
{
    public enum TileType { Ant, Dead, Land, Food, Water, Unseen }

    public class Tile : IPathNode<Object>
    {
        public TileType Type { get; private set; }
        public Location Location { get; private set; }

        public Tile(TileType type, Location location)
        {
            this.Type = type;
            this.Location = location;
        }

        public bool IsWalkable(object inContext)
        {
            return (this.Type == TileType.Land || this.Type == TileType.Food) && this.Location.IsUnoccupied();
        }
    }

    
}

