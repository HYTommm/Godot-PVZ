using Godot;
using static Godot.GD;
using System;


public abstract partial class MoneyCropsPlants : Plants
{
    public Timer TimerProduce = new Timer(); // 计时器
    public float ProduceTime = 0.1f;
    // 抽象函数：_Produce()
    public abstract void _Produce();
    // 光照效果
    public abstract void _Light();
    public override void _Ready()
    {
        base._Ready();
        AddChild(TimerProduce);

    }

    public override void _Plant(int row, int index)
    {
        base._Plant(row, index);
        TimerProduce.WaitTime = ProduceTime;
        TimerProduce.OneShot = true;
        TimerProduce.Timeout += _Light;
        
        TimerProduce.Start();
    }
}

