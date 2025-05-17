using Godot;
using System;

/// <summary>
/// <para>植物基类，继承自Node2D。</para>
/// <para>实现了种植植物的基本功能，包括生命值、种植音效播放器、阳光消耗量、冷却时间、主游戏节点等属性。</para>
/// <para>并提供了虚函数_Idle()、_Plant()、_SetColor()、_SetAlpha()，用于实现植物的待机状态、种植植物、设置植物的颜色、设置植物的透明度。</para>
/// <para>子类需要实现_Idle()</para>
/// <para>子类可实现_Plant()、_SetColor()、_SetAlpha()。</para>
/// </summary>
public abstract partial class Plants : Node2D
{
	/// <summary>种植音效播放器</summary>
	AudioStreamPlayer PlantSound = new AudioStreamPlayer();

	/// <summary>生命值</summary>
	public int HP = 300;

	/// <summary>最大生命值</summary>
	public int MaxHP = 300;
	
	/// <summary>植物的索引/栈数</summary>
	public int Index;

	/// <summary>植物所在行</summary>
	public int Row;

	/// <summary>植物所在列</summary>
	public int Col;

	/// <summary>是否已经种植</summary>
	public bool isPlanted = false;

	/// <summary>阳光消耗量</summary>
	public int SunCost = -1;
	

	/// <summary>类：冷却时间</summary>
	public class CDTime
	{
		/// <summary>快，约7.5秒</summary>
		static public float FAST = 7.5f;
		/// <summary>慢，约15秒</summary>
		static public float SLOW = 15f;
		/// <summary>非常慢，约30秒</summary>
		static public float VERY_SLOW = 30f;
	}

	/// <summary>冷却时间</summary>
	public float CDtime = 0;// 冷却时间

	/// <summary>主游戏节点</summary>
	public MainGame mainGame;


	public override void _Ready()
	{
		mainGame = this.GetMainGame();
        AddChild(PlantSound); // 添加种植音效播放器
        GD.Print(mainGame);
	}

	/// <summary>
	/// 虚函数，用于实现植物的待机状态
	/// </summary>
	public abstract void _Idle();

	/// <summary>
	/// 虚函数，用于种植植物
	/// </summary>
	/// <param name="row">设置植物所在行</param>
	/// <param name="index">设置植物的索引/栈数</param>
	public virtual void _Plant(int row, int index)
	{
		Row = row; // 设置植物所在行
		Index = index; // 设置植物的索引/栈数
		Visible = true; // 显示
		isPlanted = true; // 设置状态为 已种植
		//SelfModulate = new Color(1, 1, 1, 1);
		_SetColor(new Color(1, 1, 1, 1)); // 设置颜色
		GetNode<Sprite2D>("./Shadow").Visible = true; // 显示阴影

		uint random = GD.Randi() % 2; // 随机播放种植音效
		switch (random)
		{
			case 0: 
				PlantSound.Stream = (AudioStream)GD.Load("res://sounds/plant.ogg");
				break;
			case 1:
				PlantSound.Stream = (AudioStream)GD.Load("res://sounds/plant2.ogg");
				break;
		}

		_Idle(); // 开始待机状态
		
		PlantSound.VolumeDb = -5; // 设置音量
		PlantSound.Play(); // 播放种植音效
		
	}

	/// <summary>
	/// 虚函数，用于对植物扣血
	/// </summary>
	/// <param name="damage">扣血量</param>
	public virtual void Hurt(int damage)
	{
		HP -= damage; // 扣血
		if (HP <= 0) // 生命值小于等于0
		{
			FreePlant(); // 释放植物
		}
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
		
		Visible = false;
	}
}
