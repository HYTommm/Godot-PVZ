using Godot;
using System;

public static partial class GameManager
{
    public static MainGame GetMainGame(this Node node)
    {
        return node.GetNode<MainGame>("/root/MainGame");
    }

    public static Node GetGlobalNode(this Node node)
    {
        return node.GetNode<Node>("/root/Global");
    }
}

