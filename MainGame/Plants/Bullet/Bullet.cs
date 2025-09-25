using Godot;
using static ResourceDB.Sounds;
using System;

public abstract partial class Bullet : Node2D
{
	[Export] public float ShadowPositionY = 0.0f; // 子弹的阴影位置

	bool _bisDisappear = false; // 这个变量用来判断子弹是否消失

    [Export] protected Sprite2D BulletSprite2D; // 子弹的主体
    [Export] protected Sprite2D Shadow; // 子弹的阴影
    [Export] protected Area2D Area2D; // 子弹的碰撞检测区域
    [Export] protected GpuParticles2D GpuParticles; // 子弹的粒子效果
	[Export] protected HurtType HurtType = HurtType.Direct; // 子弹伤害类型
    protected readonly AudioStreamPlayer BulletSplatsSound = new(); // 子弹的爆炸声音
	
	public int Damage = 20; // 子弹伤害
	public override void _Ready()
	{
        AddChild(BulletSplatsSound); // 添加子弹的爆炸声音节点
		Shadow.GlobalPosition = new Vector2(GlobalPosition.X, ShadowPositionY); // 设置子弹的阴影位置
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		if (!_bisDisappear)
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

	public void OnAreaEntered(Area2D area)
	{
		if (_bisDisappear)
        {
			GD.Print("Bullet has already disappeared!");
            return;
        }

        //GD.Print("Bullet collided with " + area.Name);
		_bisDisappear = true; // 子弹消失

		BulletSprite2D.Visible = false; // 子弹不可见
		Shadow.Visible = false; // 子弹阴影不可见
		Area2D.SetDeferred("monitoring", false); // 停止检测子弹碰撞

		// 判断子弹是否击中僵尸
		if (area.GetNode("..") is Zombie zombie) 
		{
			AttackZombie(zombie);
		}
		else
		{
			GD.Print("子弹没有击中僵尸！它可能击中了其他东西：" + area.Name);
		}
	}

	public async void AttackZombie(Zombie zombie)
	{
        GD.Print("Bullet hit zombie");
        //僵尸扣血
        zombie.Hurt(new Hurt(Damage, HurtType));
        //zombie.Die();
        //是sprite节点不可见


        GpuParticles.SetDeferred("emitting", true);
		PlaySplatSound();
        // 延迟0.4秒后销毁子弹
        await ToSignal(GetTree().CreateTimer(0.5), SceneTreeTimer.SignalName.Timeout);
        QueueFree();
    }

	public abstract void PlaySplatSound();
}
