using Godot;
using System;

public partial class ZombieHand : Node2D
{
	[Signal]
	public delegate void AnimEndEventHandler();

	AudioStreamPlayer LoseMusicSound = new AudioStreamPlayer();
	AudioStreamPlayer EvilLaughSound = new AudioStreamPlayer();

	public int isEnd = 0;

	public override void _Ready()
	{

		LoseMusicSound.Stream = (AudioStream)GD.Load("res://sounds/losemusic.ogg");
		EvilLaughSound.Stream = (AudioStream)GD.Load("res://sounds/evillaugh.ogg");
		LoseMusicSound.VolumeDb = -5;
		EvilLaughSound.VolumeDb = -5;
		AddChild(LoseMusicSound);
		AddChild(EvilLaughSound);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void Play()
	{
		AnimationPlayer animation = GetNode<AnimationPlayer>("./AnimationPlayer");
		animation.AnimationFinished += End;
		music();
		animation.Play("Zombie_hand");
		
	}
	public void End(StringName type)
	{
		End();
	}

	public void End()
	{
		isEnd++;
		if (isEnd == 2)
		{
			EmitSignal(SignalName.AnimEnd);
		}
	}

	async private void music()
	{
		LoseMusicSound.Play();
		await ToSignal(GetTree().CreateTimer(1.46), SceneTreeTimer.SignalName.Timeout);
		EvilLaughSound.Play();
		EvilLaughSound.Finished += End;
	}
}
