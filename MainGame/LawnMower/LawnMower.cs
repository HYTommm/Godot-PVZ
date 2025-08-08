using Godot;
using System;
using static ResourceDB;

public partial class LawnMower : Node2D
{
	private bool _isMoving = false;
	private Vector2 _startPosition;
	private Vector2 _endPosition;
	private float _speed = 500.0f; // 移动速度，500是开局时移动至草坪前的速度
	private const float NormalSpeed = 350.0f; // 正常速度
	private const float SlowDownSpeed = 250.0f; // 僵尸撞击后减速速度
	private float _slowDownTime = 0f; // 僵尸撞击后减速时间
	private readonly AudioStreamPlayer _engineSound = new(); // 发动机声音播放器

	/// <summary>NormalWove动画播放器</summary>
	[Export] public AnimationPlayer AnimMoveNormal;
	/// <summary>TrickedWove动画播放器</summary>
	[Export] public AnimationPlayer AnimMoveTricked;
	
	public int Row = 0; // 所在行

	public LawnMower()
	{
		Scale = new Vector2(0.85f, 0.85f);
	}

	public override void _Ready()
	{
		_engineSound.Stream = Sounds.Sound_LawnMower;
		AddChild(_engineSound);
		_startPosition = Position;
	}

	public void MoveTo(Vector2 endPosition)
	{
		GD.Print("LawnMower: Moving to " + endPosition);
		_endPosition = endPosition;
		_isMoving = true;
		AnimMoveNormal.Play(name: "LawnMower_normal", customSpeed: 1.5f);
	}

	public void Stop()
	{
		_isMoving = false;
	}

	public override void _Process(double delta)
	{
        
		if (_isMoving)
		{
            Vector2 direction = _endPosition - _startPosition;
            direction = direction.Normalized();
			Vector2 stepDelta = direction * _speed * (float)delta;
			Position += stepDelta;
            Vector2 remainingDirection = (_endPosition - Position + stepDelta).Normalized();
            if (Position.DistanceTo(_endPosition) < 3f ||
                remainingDirection.X * direction.X < 0 ||
                remainingDirection.Y * direction.Y < 0)
            {
                GD.Print("LawnMower: Reached destination " + _endPosition);
                _isMoving = false;
                Position = _endPosition;
                AnimMoveNormal.Stop();
            }
        }
		if (_slowDownTime > 0f)
		{
			_slowDownTime -= (float)delta;
			if (_slowDownTime <= 0f)
				_speed = NormalSpeed;
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
            if (!_isMoving)
            {
                _speed = NormalSpeed;
                _engineSound.Play();
                MoveTo(new Vector2(1200, Position.Y));
            }
            else
            {
                _speed = SlowDownSpeed;
                _slowDownTime = 0.5f; 
            }
        }
    }
}
