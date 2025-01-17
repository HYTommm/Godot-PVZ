using Godot;
using System;

public partial class MainGame : Node2D
{
	public Vector2 CenterPos = new(220, 0);
	public Vector2 RightPos = new(600, 0);
	public Camera camera;
	AnimationPlayer animation;

	public override void _Ready()
	{
		camera = GetNode<Camera>("./Camera");
		animation = GetNode<AnimationPlayer>("./CanvasLayer/AnimationPlayer");
		SelectSeedCard();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{

	}

	async public void SelectSeedCard()
	{
		
		await ToSignal(GetTree().CreateTimer(1.3), SceneTreeTimer.SignalName.Timeout);
		camera.Move(RightPos, 1.25);
		await ToSignal(camera, Camera.SignalName.MoveEnd);

		await ToSignal(GetTree().CreateTimer(1.2), SceneTreeTimer.SignalName.Timeout);
		Game();
	}
	
	async public void Game()
	{
		camera.Move(CenterPos, 1);
		await ToSignal(camera, Camera.SignalName.MoveEnd);
		animation.Play("SeedBank");
	}
}
