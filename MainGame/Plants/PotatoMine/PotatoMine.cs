using Godot;
using static ResourceDB.Sounds;
using System;
using Godot.Collections;

public partial class PotatoMine : Plants
{
	[Export] public AnimationPlayer Anim_armed;
	[Export] public AnimationPlayer Anim_idle;
	[Export] public AnimationPlayer Anim_blink;
	[Export] public AnimationPlayer Anim_rise;
	[Export] public AnimationPlayer Anim_mash;
	[Export] private GpuParticles2D _particleExplode1;
	[Export] private GpuParticles2D _particleExplode2;
	[Export] private GpuParticles2D _particleExplode3;
	[Export] private GpuParticles2D _particleExplode4;
	[Export] private GpuParticles2D _particleExplode5;

	[Export] private Area2D _attackArea;
	[Export] private Area2D _detectionArea;
	[Export] private Node2D _bodyNodeTree;
	[Export] private int _damage = 1800;
	[Export] public float TimeToRise = 15f;
	public Timer TimerToRise = new();
	private AudioStreamPlayer _audioExplode = new();
	//public override Vector2 Offset { get => base.Offset - TempOffset; set => base.Offset = value; }
	public override void _Ready()
	{
		base._Ready();

		AddChild(TimerToRise);
		TimerToRise.WaitTime = TimeToRise;
		TimerToRise.OneShot = true;
		TimerToRise.Timeout += Rise;

		_detectionArea.Monitoring = false;

		AddChild(_audioExplode);
		//Position += new Vector2(30, 20);
	}

	public override void _Plant(int col, int row, int index)
	{
		base._Plant(col, row, index);
		
		TimerToRise.Start();
	}

	public override void _Idle()
	{
		Anim_idle.Play("PotatoMine_idle");
	}

	public void Rise()
	{
		Anim_rise.AnimationFinished += _Armed;
		Anim_rise.Play("PotatoMine_rise");
	}

	public void _Armed(StringName anim)
	{
		_detectionArea.Monitoring = true;
		Anim_armed.Play("PotatoMine_armed");
		//Explode();
	}

	private void DamageZombies(Area2D area)
	{
		bool bHasZombie = false;
		Array<Area2D> overlappingAreas = _attackArea.GetOverlappingAreas();
		foreach (Area2D overlappingArea in overlappingAreas)
		{
			GD.Print("PotatoMine overlapping area: " + overlappingArea.Name);
			if (overlappingArea.GetNode("../..") is Zombie zombie && zombie.Row == Row)
			{

				GD.Print("PotatoMine damaging zombie! Row: ", Row, "Zombie Row: ", zombie.Row);
				bHasZombie = true;
				//僵尸扣血
				zombie.Hurt(new Hurt(_damage, HurtType.Explosion));
			}
		}
		if (bHasZombie)
			Explode();
	}


	private async void Explode()
	{
		GD.Print("PotatoMine exploded!");
		
		_bodyNodeTree.Visible = false;
		Shadow.Visible = false;

		_audioExplode.Stop();
		_audioExplode.Stream = Sound_PotatoMine;
		_audioExplode.Play();
		_particleExplode1.Emitting = true;
		_particleExplode2.Emitting = true;
		_particleExplode3.Emitting = true;
		_particleExplode4.Emitting = true;
		_particleExplode5.Emitting = true;
		//Anim_mash.Play("PotatoMine_mashed");
		MainGame.Camera.Shake(0.3f, 1);
		await ToSignal(GetTree().CreateTimer(1.5f), "timeout");
		FreePlant();
	}
}
