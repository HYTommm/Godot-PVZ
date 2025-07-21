using Godot;
using static ResourceManager.Sounds;
using System;

public partial class Bullet : Node2D
{
	[Export]
	public float ShadowPositionY = 0.0f; // 子弹的阴影位置

	bool IsDisappear = false; // 这个变量用来判断子弹是否消失

	Sprite2D BulletSprite2D; // 子弹的主体
	Sprite2D Shadow; // 子弹的阴影
	Area2D Area2D; // 子弹的碰撞检测区域
	GpuParticles2D GPUParticles; // 子弹的粒子效果
	AudioStreamPlayer BulletSplatsSound = new AudioStreamPlayer(); // 子弹的爆炸声音
	
	public int Damage = 20; // 子弹伤害
	public override void _Ready()
	{
		BulletSprite2D = GetNode<Sprite2D>("Sprite2D"); // 获取子弹的主体节点
		Shadow = GetNode<Sprite2D>("Shadow"); // 获取子弹的阴影节点
		Area2D = GetNode<Area2D>("Area2D"); // 获取子弹的碰撞检测区域节点
		GPUParticles = GetNode<GpuParticles2D>("./Splats"); // 获取子弹的粒子效果节点
        GPUParticles.OneShot = true;
        AddChild(BulletSplatsSound); // 添加子弹的爆炸声音节点
		GetNode<Sprite2D>("Shadow").GlobalPosition = new Vector2(GlobalPosition.X, ShadowPositionY); // 设置子弹的阴影位置
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
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

		BulletSprite2D.Visible = false; // 子弹不可见
		Shadow.Visible = false; // 子弹阴影不可见
		Area2D.SetDeferred("monitoring", false); // 停止检测子弹碰撞

		// 判断子弹是否击中僵尸
		if (area.GetNode("../..") is Zombie zombie) 
		{
			
			//GD.Print("Bullet hit zombie"); 
			//僵尸扣血
			zombie.Hurt(new Hurt(Damage, HurtType.Direct));
			//zombie.Die();
			//是sprite节点不可见
			
			
			GPUParticles.SetDeferred("emitting", true);
			

			// 随机数
			uint random = GD.Randi() % 3;
			//GD.Print("Random number: " + random);
			// 根据随机数播放不同的爆炸声音
			switch (random)
			{
				case 0:
					BulletSplatsSound.Stream = Sound_Splat;
					break;
				case 1:
					BulletSplatsSound.Stream = Sound_Splat2;
					break;
				case 2:
					BulletSplatsSound.Stream = Sound_Splat3;
					break;
			}

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
