using Godot;
using System;
using static ResourceManager;

public partial class LawnMower : Node2D
{
	private bool _is_moving = false;
	private Vector2 _start_position;
	private Vector2 _end_position;
	private float _speed = 500.0f; // 移动速度，500是开局时移动至草坪前的速度
	private float _normal_speed = 350.0f; // 正常速度
	private float _slow_down_speed = 250.0f; // 僵尸撞击后减速速度
	private float _slow_down_time = 0f; // 僵尸撞击后减速时间
	private AudioStreamPlayer EngineSound = new AudioStreamPlayer(); // 发动机声音播放器

	/// <summary>NormalWove动画播放器</summary>
	public AnimationPlayer AnimMoveNormal;
	/// <summary>TrickedWove动画播放器</summary>
	public AnimationPlayer AnimMoveTricked;
	
	public int Row = 0; // 所在行

	public LawnMower()
	{
		Scale = new Vector2(0.85f, 0.85f);
	}

	public override void _Ready()
	{
		AnimMoveNormal = GetNode<AnimationPlayer>("Anim_LawnMower_normal");
		AnimMoveTricked = GetNode<AnimationPlayer>("Anim_LawnMower_tricked");
		//EngineSound = GetNode<AudioStreamPlayer>();
		EngineSound.Stream = Sounds.Sound_LawnMower;
		AddChild(EngineSound);
		_start_position = Position;
	}

	public void MoveTo(Vector2 end_position)
	{
		_end_position = end_position;
		_is_moving = true;
		AnimMoveNormal.Play(name: "LawnMower_normal", customSpeed: 1.5f);
	}

	public void Stop()
	{
		_is_moving = false;
	}

	public override void _Process(double delta)
	{
		if (_is_moving)
		{
			Vector2 direction = _end_position - _start_position;
			direction = direction.Normalized();
			Position += direction * _speed * (float)delta;
		}
		if (_slow_down_time > 0f)
		{
			_slow_down_time -= (float)delta;
			if (_slow_down_time <= 0f)
				_speed = _normal_speed;
		}
		if (Position.DistanceTo(_end_position) < 3f)
		{
			_is_moving = false;
			AnimMoveNormal.Stop();
		}
	}

	// 当与僵尸碰撞时，触发此函数
	public void OnAreaEntered(Area2D area)
	{
		GD.Print("LawnMower: Area " + area.Name + " has entered LawnMower " + Name);
		if (area.GetNode("../..") is Zombie zombie && zombie.Row == Row)
		{
			zombie.Hurt(new Hurt(65535, HurtType.LawnMower));
			GD.Print("LawnMower: Zombie " + zombie.Name + " has collided with LawnMower " + Name);
			if (!_is_moving)
			{
				_speed = _normal_speed;
				EngineSound.Play();
				MoveTo(new Vector2(1200, Position.Y));
			}
			else
			{
				_speed = _slow_down_speed;
				_slow_down_time = 0.5f; 
			}
		}
	}
}
