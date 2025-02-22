using Godot;
using System;

public abstract partial class Plants : Node2D
{
	AudioStreamPlayer PlantSound = new AudioStreamPlayer();
    public int HP = 100;                                 // 生命值
    public int MaxHP = 100;                              // 最大生命值
    // 索引
    public int Index;
    // 所处行
    public int Row;
    public abstract void _Idle();
	public virtual void _Plant()
	{
		Visible = true;
		SelfModulate = new Color(1, 1, 1, 1);
		GetNode<Sprite2D>("./Shadow").Visible = true;
		uint random = GD.Randi() % 2;
		switch (random)
		{
			case 0:
				PlantSound.Stream = (AudioStream)GD.Load("res://sounds/plant.ogg");
				break;
			case 1:
				PlantSound.Stream = (AudioStream)GD.Load("res://sounds/plant2.ogg");
				break;
		}
		_Idle();
		AddChild(PlantSound);
		PlantSound.VolumeDb = -5;
		PlantSound.Play();
		
	}

}
