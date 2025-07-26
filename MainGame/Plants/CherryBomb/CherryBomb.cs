using Godot;
using Godot.Collections;
using static ResourceManager.Sounds;

public partial class CherryBomb : Plants
{
	[Export]private AnimationPlayer _animExplode;
	[Export]private GpuParticles2D _particleExplode1;
	[Export]private GpuParticles2D _particleExplode2;
	[Export]private GpuParticles2D _particleExplode3;
	[Export]private int _damage = 1800;

	private AudioStreamPlayer _audioExplode = new();

	public override void _Idle()
	{
		Explode();
	}

	public override void _Ready()
	{
		base._Ready();
		AddChild(_audioExplode);
	}

	public void Explode()
	{
		_audioExplode.Stream = Sound_ReverseExplosion;
		_audioExplode.Play();
		_animExplode.Play("CherryBomb_explode", -1, 0.5f);
		_animExplode.AnimationFinished += DamageZombies;
}

	private async void DamageZombies(StringName animName)
	{
		GD.Print("CherryBomb exploded!");
		Area2D area = GetNode<Area2D>("Area2D");
		Array<Area2D> overlappingAreas = area.GetOverlappingAreas();
		foreach (Area2D overlappingArea in overlappingAreas)
		{
			GD.Print("CherryBomb overlapping area: " + overlappingArea.Name);
			if (overlappingArea.GetNode("../..") is Zombie zombie)
			{
				GD.Print("CherryBomb damaging zombie!");
				//僵尸扣血
				zombie.Hurt(new Hurt(_damage, HurtType.Explosion));
			}
		}
		GetNode<Node2D>("Node2D").Visible = false;
		GetNode<Sprite2D>("Shadow").Visible = false;

		_audioExplode.Stop();
		_audioExplode.Stream = Sound_CherryBomb;
		_audioExplode.Play();
		_particleExplode1.Emitting = true;
		_particleExplode2.Emitting = true;
		_particleExplode3.Emitting = true;
		await ToSignal(GetTree().CreateTimer(0.7f), "timeout");
		FreePlant();
	}
}
