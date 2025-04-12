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
		//子弹向右移动
		Position += new Vector2(10 * (float)delta * 30, 0);
		//判断子弹是否超出屏幕范围
		if (GlobalPosition.X > 1000)
		{
			QueueFree();
		}
	}

	async public void OnAreaEntered(Area2D area)
	{
		//GD.Print("Bullet collided with " + area.Name);
		if (area.GetNode("..") is Zombie zombie)
		{
			GetNode<Sprite2D>("Sprite2D").Visible = false;
			GetNode<Sprite2D>("Shadow").Visible = false;
			GetNode<Area2D>("./Sprite2D/Area2D").CallDeferred("set_monitoring", false);
			//GD.Print("Bullet hit zombie");
			//僵尸扣血
			zombie.Hurt(Damage);
			//zombie.Die();
			//是sprite节点不可见
			
			IsDisappear = true;
			GPUParticles.Emitting = true;
			GPUParticles.OneShot = true;

			//随机数
			uint random = GD.Randi() % 3;
			//GD.Print("Random number: " + random);
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

			BulletSplatsSound.VolumeDb = -5;
			
			BulletSplatsSound.Play();


			await ToSignal(GetTree().CreateTimer(0.4), SceneTreeTimer.SignalName.Timeout);
			QueueFree();
		}
	}

	public void OnAreaShapeEntered(Rid area_rid, Area2D area2D, int areaShapeIdx, int selfShapeIdx)
	{
		//GD.Print("Bullet collided with " + area2D.ShapeFindOwner(areaShapeIdx).ToString());
	}
}
