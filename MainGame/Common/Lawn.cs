using Godot;
using System;

public partial class Lawn : Node2D
{
    /// <summary>内置类：类型</summary>
    public class LawnType
    {
        /// <summary>草地</summary>
        public const int Grass = 0;
        /// <summary>水池</summary>
        public const int Pool = 1;
        /// <summary>屋顶</summary>
        public const int Roof = 2;
    }
    /// <summary>草地类型</summary>
    public int Type = LawnType.Grass;
    /// <summary>植物数量</summary>
    public int PlantCount = 0;

}
