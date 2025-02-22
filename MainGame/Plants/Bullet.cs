using Godot;
using System;

public partial class Bullet : Node2D
{
	bool IsDisappear = false; // 这个变量用来判断子弹是否消失
	GpuParticles2D GPUParticles; // 子弹的粒子效果
	AudioStreamPlayer BulletSplatsSound = new AudioStreamPlayer(); // 子弹的爆炸声音
	public override void _Ready()
	{
		GPUParticles = GetNode<GpuParticles2D>("./Splats");
		AddChild(BulletSplatsSound);
		GD.Randomize();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		//子弹向右移动
		Position += new Vector2(5, 0);
		//判断子弹是否超出屏幕范围
		if (Position.X > 900)
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
			GetNode<Area2D>("./Sprite2D/Area2D").CallDeferred("set_monitoring", false);
			//GD.Print("Bullet hit zombie");
			//僵尸扣血
			zombie.Hurt(10);
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
