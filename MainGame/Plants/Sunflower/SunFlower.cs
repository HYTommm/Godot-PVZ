using Godot;
using System;
using System.Drawing;

public partial class SunFlower : MoneyCropsPlants
{
	public AnimationPlayer Anim_Idle;
	public AnimationPlayer Anim_Blink;
	public AnimationPlayer Anim_SunLight;

	public SunFlower()
	{
		
		SunCost = 50; // 设置花费的阳光数量
		CDtime = CDTime.FAST; // 设置冷却时间
		
	}

	

	/*
		var tween = create tween()
		var rng_x= Broad._rng.randf_range(-25,25)
		var rng_y = Broad._rng.randf_range(50,70)
		tween.tween_property(self,"position",Vector2(position.x + rng_x,position.y - rng_y),0.1)
		tween.tween_property(self,"position",Vector2(position.x + rng_x* 2,position.y + 25),0.2)
		tween.connect("finished",_popupover.emit)
		item_sport =0
		return
	*/

	// 实现接口函数，发光
	public override void _Light()
	{
		if (!isPlanted)
			return;
		Anim_SunLight.Play("SunLight");
		
	}

	// 实现接口函数，生产
	public override void _Produce()
	{
		if (!isPlanted)
			return;
		Sun sun = GD.Load<PackedScene>("res://MainGame/Drops/Sun.tscn").Instantiate() as Sun; // 实例化太阳
		sun.Position = new Vector2(Position.X + 40, Position.Y + 20);// 设置太阳的位置
		sun.Scale = new Vector2(0.3f, 0.3f); // 设置太阳的大小
		GetParent().AddChild(sun);// 添加太阳到场景中

		sun._Drop(); // 太阳掉落
		
		TimerProduce.WaitTime = mainGame.RNG.RandfRange(23.5f, 35.0f); // 设置生产时间
		TimerProduce.Start(); // 启动生产计时器
		GD.Print("TimerProduce : Time = " + TimerProduce.WaitTime);
	}

    public override int Hurt(int damage)
    {
		return damage;
    }
	
	public override void _Ready()
	{
		
		base._Ready();

		ProduceTime = mainGame.RNG.RandfRange(3.0f, 12.5f); // 设置产出时间
		Anim_Idle = GetNode<AnimationPlayer>("./Idle");
		Anim_Blink = GetNode<AnimationPlayer>("./Blink");
		Anim_SunLight = GetNode<AnimationPlayer>("./SunLight");

		

		//((ShaderMaterial)Material).SetShaderParameter("alpha", Modulate.A);


	}

	
	public override void _Process(double delta)
	{
		;
	}

	// 实现接口函数，用于播放动画 Idle
	public override void _Idle()
	{
		Anim_Idle.Play("Idle");
	}

	// 实现接口函数，设置透明度
	public override void _SetAlpha(float alpha)
	{
		((ShaderMaterial)Material).SetShaderParameter("alpha", alpha);
	}
}
