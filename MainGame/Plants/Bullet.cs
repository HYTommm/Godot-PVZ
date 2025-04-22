using Godot;
using System;

public partial class Bullet : Node2D
{
	[Export]
	public float ShadowPositionY = 0.0f; // 子弹的阴影位置

	bool IsDisappear = false; // 这个变量用来判断子弹是否消失

	GpuParticles2D GPUParticles; // 子弹的粒子效果
	AudioStreamPlayer BulletSplatsSound = new AudioStreamPlayer(); // 子弹的爆炸声音
	
	public int Damage = 20; // 子弹伤害
	public override void _Ready()
	{
		GPUParticles = GetNode<GpuParticles2D>("./Splats"); // 获取子弹的粒子效果节点
		AddChild(BulletSplatsSound); // 添加子弹的爆炸声音节点
		GetNode<Sprite2D>("Shadow").GlobalPosition = new Vector2(GlobalPosition.X, ShadowPositionY); // 设置子弹的阴影位置
		GD.Randomize(); // 随机数初始化
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (!IsDisappear)
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

	async public void OnAreaEntered(Area2D area)
	{
		//GD.Print("Bullet collided with " + area.Name);
		IsDisappear = true; // 子弹消失
		GetNode<Sprite2D>("Sprite2D").Visible = false; // 子弹不可见
		GetNode<Sprite2D>("Shadow").Visible = false; // 子弹阴影不可见
		//GetNode<Area2D>("./Area2D").CallDeferred("set_monitoring", false); // 停止检测子弹碰撞
		GetNode<Area2D>("./Area2D").SetDeferred("monitoring", false); // 停止检测子弹碰撞
		// 判断子弹是否击中僵尸
		if (area.GetNode("..") is Zombie zombie) 
		{
			
			GD.Print("Bullet hit zombie"); 
			//僵尸扣血
			zombie.Hurt(Damage);
			//zombie.Die();
			//是sprite节点不可见
			
			GPUParticles.OneShot = true;
			GPUParticles.SetDeferred("emitting", true);
			

			// 随机数
			uint random = GD.Randi() % 3;
			//GD.Print("Random number: " + random);
			// 根据随机数播放不同的爆炸声音
			switch (random)
			{
				case 0:
					BulletSplatsSound.Stream = (AudioStream)GD.Load("res://sounds/splat.ogg");
					break;
				case 1:
					BulletSplatsSound.Stream = (AudioStream)GD.Load("res://sounds/splat2.ogg");
					break;
				case 2:
					BulletSplatsSound.Stream = (AudioStream)GD.Load("res://sounds/splat3.ogg");
					break;
			}

			// 设置爆炸声音的音量 
			BulletSplatsSound.VolumeDb = -5;
			// 播放爆炸声音
			BulletSplatsSound.Play();

			// 延迟0.4秒后销毁子弹
			await ToSignal(GetTree().CreateTimer(0.4), SceneTreeTimer.SignalName.Timeout);
			QueueFree();
		}
		else
		{
			GD.Print("子弹没有击中僵尸！它可能击中了其他东西：" + area.Name);
		}
	}

	public void OnAreaShapeEntered(Rid area_rid, Area2D area2D, int areaShapeIdx, int selfShapeIdx)
	{
		//GD.Print("Bullet collided with " + area2D.ShapeFindOwner(areaShapeIdx).ToString());
	}
}
