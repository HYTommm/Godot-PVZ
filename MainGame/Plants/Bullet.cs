using Godot;
using static ResourceManager.Sounds;
using System;

public partial class Bullet : Node2D
{
	[Export] public float ShadowPositionY = 0.0f; // 子弹的阴影位置

	bool _BisDisappear = false; // 这个变量用来判断子弹是否消失

    [Export] private Sprite2D _bulletSprite2D; // 子弹的主体
    [Export] private Sprite2D _shadow; // 子弹的阴影
    [Export] private Area2D _area2D; // 子弹的碰撞检测区域
    [Export] private GpuParticles2D _gpuParticles; // 子弹的粒子效果
    private readonly AudioStreamPlayer _bulletSplatsSound = new(); // 子弹的爆炸声音
	
	public int Damage = 20; // 子弹伤害
	public override void _Ready()
	{
        AddChild(_bulletSplatsSound); // 添加子弹的爆炸声音节点
		_shadow.GlobalPosition = new Vector2(GlobalPosition.X, ShadowPositionY); // 设置子弹的阴影位置
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		if (!_BisDisappear)
		{
			//子弹向右移动
			Position += new Vector2(333 * (float)delta, 0);
			//判断子弹是否超出屏幕范围
			if (GlobalPosition.X > 1000)
			{
				QueueFree();
			}
		}
		
	}

	public async void OnAreaEntered(Area2D area)
	{
		//GD.Print("Bullet collided with " + area.Name);
		_BisDisappear = true; // 子弹消失

		_bulletSprite2D.Visible = false; // 子弹不可见
		_shadow.Visible = false; // 子弹阴影不可见
		_area2D.SetDeferred("monitoring", false); // 停止检测子弹碰撞

		// 判断子弹是否击中僵尸
		if (area.GetNode("../..") is Zombie zombie) 
		{
			
			//GD.Print("Bullet hit zombie"); 
			//僵尸扣血
			zombie.Hurt(new Hurt(Damage, HurtType.Direct));
			//zombie.Die();
			//是sprite节点不可见
			
			
			_gpuParticles.SetDeferred("emitting", true);
			

			// 随机数
			uint random = GD.Randi() % 3;
			//GD.Print("Random number: " + random);
			// 根据随机数播放不同的爆炸声音
            _bulletSplatsSound.Stream = random switch
            {
                0 => Sound_Splat,
                1 => Sound_Splat2,
                2 => Sound_Splat3,
                _ => _bulletSplatsSound.Stream
            };

            // 播放爆炸声音
			_bulletSplatsSound.Play();

			// 延迟0.4秒后销毁子弹
			await ToSignal(GetTree().CreateTimer(0.4), SceneTreeTimer.SignalName.Timeout);
			QueueFree();
		}
		else
		{
			GD.Print("子弹没有击中僵尸！它可能击中了其他东西：" + area.Name);
		}
	}
}
