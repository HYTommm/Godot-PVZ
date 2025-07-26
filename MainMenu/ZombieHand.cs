using Godot;
using static ResourceManager.Sounds;

public partial class ZombieHand : Node2D
{
	[Signal]
	public delegate void AnimEndEventHandler();

	private readonly AudioStreamPlayer _loseMusicSound = new();
	private readonly AudioStreamPlayer _evilLaughSound = new();

	public int BIsEnd = 0;

	public override void _Ready()
	{

		_loseMusicSound.Stream = Sound_LoseMusic;
		_evilLaughSound.Stream = Sound_EvilLaugh;
		AddChild(_loseMusicSound);
		AddChild(_evilLaughSound);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void Play()
	{
		AnimationPlayer animation = GetNode<AnimationPlayer>("./AnimationPlayer");
		animation.AnimationFinished += End;
		Music();
		animation.Play("Zombie_hand");
		
	}
	public void End(StringName type)
	{
		End();
	}

	public void End()
	{
		BIsEnd++;
		if (BIsEnd == 2)
		{
			EmitSignal(SignalName.AnimEnd);
		}
	}

	private async void Music()
	{
		_loseMusicSound.Play();
		await ToSignal(GetTree().CreateTimer(1.46), SceneTreeTimer.SignalName.Timeout);
		_evilLaughSound.Play();
		_evilLaughSound.Finished += End;
	}
}
