using Godot;
using System;

/// <summary>
/// <para>植物基类，继承自Node2D。</para>
/// <para>实现了种植植物的基本功能，包括生命值、种植音效播放器、阳光消耗量、冷却时间、主游戏节点等属性。</para>
/// <para>并提供了虚函数_Idle()、_Plant()、_SetColor()、_SetAlpha()，用于实现植物的待机状态、种植植物、设置植物的颜色、设置植物的透明度。</para>
/// <para>子类需要实现_Idle()</para>
/// <para>子类可实现_Plant()、_SetColor()、_SetAlpha()。</para>
/// </summary>
public abstract partial class Plants : HealthEntity
{
	/// <summary>种植音效播放器</summary>
	private readonly AudioStreamPlayer _plantSound = new();


	/// <summary>植物所在行</summary>
	public int Row;

	/// <summary>植物所在列</summary>
	public int Col;

	/// <summary>是否已经种植</summary>
	public bool BIsPlanted = false;

	/// <summary>阳光消耗量</summary>
	public int SunCost = -1;
	

	/// <summary>类：冷却时间</summary>
	public class CDTime
	{
		/// <summary>快，约7.5秒</summary>
		
		public const float FAST = 7.5f;
		/// <summary>慢，约15秒</summary>
		public const float SLOW = 15f;
		/// <summary>非常慢，约30秒</summary>
		public const float VERY_SLOW = 30f;
	}

	/// <summary>冷却时间</summary>
	public float CDtime = 0;// 冷却时间

	/// <summary>主游戏节点</summary>
	public MainGame MainGame;

	protected Plants()
	{
		HP = 300; // 设置生命值
		MaxHP = 300; // 设置最大生命值
		Index = -1; // 设置索引/栈数
	}

	public override void _Ready()
	{
		base._Ready();
		MainGame = this.GetMainGame();
		AddChild(_plantSound); // 添加种植音效播放器
		GD.Print(MainGame);
	}

	/// <summary>
	/// 虚函数，用于实现植物的待机状态
	/// </summary>
	public abstract void _Idle();

	/// <summary>
	/// 虚函数，用于种植植物
	/// </summary>
	/// <param name="col"></param>
	/// <param name="row">设置植物所在行</param>
	/// <param name="index">设置植物的索引/栈数</param>
	public virtual void _Plant(int col,int row, int index)
	{
		Row = row; // 设置植物所在行
		Col = col; // 设置植物所在列
		Index = index; // 设置植物的索引/栈数
		GetNode<TextEdit>("./TextEdit").Text = Index.ToString(); // 设置TextEdit显示栈数
		Visible = true; // 显示
		BIsPlanted = true; // 设置状态为 已种植
		//SelfModulate = new Color(1, 1, 1, 1);
		_SetColor(new Color(1, 1, 1, 1)); // 设置颜色
		GetNode<Sprite2D>("Shadow").Visible = true; // 显示阴影

		uint random = GD.Randi() % 2; // 随机播放种植音效
		_plantSound.Stream = random switch
		{
			0 => (AudioStream)GD.Load("res://sounds/plant.ogg"),
			1 => (AudioStream)GD.Load("res://sounds/plant2.ogg"),
			_ => _plantSound.Stream
		};

		_Idle(); // 开始待机状态
		
		_plantSound.Play(); // 播放种植音效
		
	}

	/// <summary>
	/// 虚函数，用于对植物扣血
	/// </summary>
	/// <param name="damage">扣血量</param>
	public override int Hurt(int damage)
	{
		int returnDamage = 0;
		if (HP <= damage)
			returnDamage = damage - HP;
		HP -= damage; // 扣血
		if (HP <= 0) // 生命值小于等于0
		{
			FreePlant(); // 释放植物
		}
		return returnDamage;
	}

	/// <summary>
	/// 设置植物的颜色
	/// </summary>
	/// <param name="color">颜色</param>
	public virtual void _SetColor(Color color)
	{
		SelfModulate = color; // 设置颜色
		_SetAlpha(color.A); // 设置透明度
	}

	/// <summary>
	/// 设置植物的透明度
	/// </summary>
	/// <param name="alpha">透明度</param>
	public virtual void _SetAlpha(float alpha)
	{
		// 读取当前颜色rgb的值，并设置透明度
		SelfModulate = new Color(SelfModulate.R, SelfModulate.G, SelfModulate.B, alpha); 
	}

	/// <summary>
	/// 释放植物
	/// </summary>
	public virtual void FreePlant()
	{
		if (Index >= 0)
			this.GetMainGame().RemovePlant(this);
		BIsPlanted = false; // 设置状态为 未种植
		Visible = false;
	}

	public override void SetZIndex()
	{

		ZIndex = (Row + 1) * 10 + (int)ZIndexEnum.NormalPlants;
	}
}
