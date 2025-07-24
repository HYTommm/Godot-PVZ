using Godot;

public partial class SunFlower : MoneyCropsPlants
{
	[Export] public AnimationPlayer Anim_idle;
	[Export] public AnimationPlayer Anim_blink;
	[Export] public AnimationPlayer Anim_sunLight;

	public SunFlower()
	{
		
		SunCost = 50; // 设置花费的阳光数量
		CDtime = CDTime.FAST; // 设置冷却时间
		
	}

	// 实现接口函数，发光
	public override void _Light()
	{
		if (!BIsPlanted)
			return;
		Anim_sunLight.Play("SunLight");
		
	}

	// 实现接口函数，生产
	public override void _Produce()
	{
		if (!BIsPlanted)
			return;
        if (GD.Load<PackedScene>("res://MainGame/Drops/Sun.tscn").Instantiate() is Sun sun)
        {
            sun.Position = new Vector2(Position.X + 40, Position.Y + 20); // 设置太阳的位置
            sun.Scale = new Vector2(0.3f, 0.3f); // 设置太阳的大小
            MainGame.SunContainer.AddChild(sun); // 添加太阳到场景中

            sun._Drop(); // 太阳掉落
        }

        TimerProduce.WaitTime = MainGame.RNG.RandfRange(23.5f, 35.0f); // 设置生产时间
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

		ProduceTime = MainGame.RNG.RandfRange(3.0f, 12.5f); // 设置产出时间
		Anim_idle = GetNode<AnimationPlayer>("./Idle");
		Anim_blink = GetNode<AnimationPlayer>("./Blink");
		Anim_sunLight = GetNode<AnimationPlayer>("./SunLight");

		

		//((ShaderMaterial)Material).SetShaderParameter("alpha", Modulate.A);


	}

	
	public override void _Process(double delta)
	{
		
	}

	// 实现接口函数，用于播放动画 Idle
	public override void _Idle()
	{
		Anim_idle.Play("Idle");
	}

	// 实现接口函数，设置透明度
	public override void _SetAlpha(float alpha)
	{
		((ShaderMaterial)Material).SetShaderParameter("alpha", alpha);
	}
}
