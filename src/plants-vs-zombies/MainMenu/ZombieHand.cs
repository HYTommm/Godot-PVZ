using Godot;
using static ResourceDB.Sounds;

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
		// 过场动画播到一半时选关界面也能直接切场景，这时本节点已经被释放，不能再发信号
		if (BIsEnd == 2 && IsInsideTree())
		{
			EmitSignal(SignalName.AnimEnd);
		}
	}

	private async void Music()
	{
		_loseMusicSound.Play();
		await ToSignal(GetTree().CreateTimer(1.46), SceneTreeTimer.SignalName.Timeout);
		// 等待期间场景可能已经切走、本节点被释放，此时不能再碰子节点
		if (!IsInstanceValid(this) || !IsInsideTree())
		{
			return;
		}
		_evilLaughSound.Play();
		_evilLaughSound.Finished += End;
	}
}
