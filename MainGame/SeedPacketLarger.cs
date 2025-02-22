using Godot;
using System;

public partial class SeedPacketLarger : Sprite2D
{
	MainGame main = new MainGame();
	[Export]
	public PackedScene SeedScene;
	public Plants seedShow, seed, seedClone;
	public override void _Ready()
	{
		//GD.Print("SeedPacketLarger: _Ready");
		main = GetNode<MainGame>("/root/MainGame");
		seedShow = SeedScene.Instantiate<Plants>();
		//seedShow.Scale = new Vector2(0.5f, 0.5f);
		seedShow.Position = new Vector2(7, 18);
		AddChild(seedShow, false, InternalMode.Front);
	}

	public override void _PhysicsProcess(double delta)
	{
		//seed.Position = GetViewport().GetMousePosition() - new Vector2(35, 60);
	}


	private  void OnInputEvent(Node viewport, InputEvent @event, int shape_idx)
	{
		
		if (@event.IsAction("mouse_left"))
		{
			//GD.Print("SeedPacketLarger: OnInputEvent");
			if (main.mouse_left_down)
			{
				seed = SeedScene.Instantiate<Plants>();
				seedClone = SeedScene.Instantiate<Plants>();
				//seed.Scale = new Vector2(2.0f, 2.0f);
				
				
				main.AddSeed((Node2D)this, seed, seedClone);
			}
		}
		
	}

	public void OnMouseEnter()
	{
		//GD.Print("SeedPacketLarger: OnMouseEnter");
	}
}
