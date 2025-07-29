using Godot;
using System;

public static class GameManager
{
	[Obsolete("直接使用MainGame.Instance")]
	public static MainGame GetMainGame(this Node node)
	{
		return node.GetNode<MainGame>("/root/MainGame");
	}
	
	[Obsolete("直接使用Global.Instance")]
	public static Node GetGlobalNode(this Node node)
	{
		return node.GetNode<Node>("/root/Global");
	}
}
