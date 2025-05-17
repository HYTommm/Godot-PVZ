using Godot;
using System;

//namespace PlantsVsZombies
//{
    public static partial class GameManager
    {
        public static MainGame GetMainGame(this Node node)
        {
            return node.GetNode<MainGame>("/root/MainGame");
        }
    }
//}