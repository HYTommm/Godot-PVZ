using Godot;
using static Godot.GD;
using System;


public abstract partial class MoneyCropsPlants : Plants
{
    public Timer TimerProduce = new(); // 计时器
    public float ProduceTime = 0.1f;
    // 抽象函数：_Produce()
    public abstract void _Produce();
    // 光照效果
    public abstract void _Light();
    public override void _Ready()
    {
        base._Ready();

        // TimerProduce 是字段初始化器建出来的，必须在展示态早退**之前**挂上树：
        // 不挂它就永远没有父节点，成了孤儿节点，引擎回收不到
        AddChild(TimerProduce);

        if (BIsDisplayOnly)
        {
            return;
        }

    }

    public override void _Plant(int col, int row, int index)
    {
        base._Plant(col, row, index);
        TimerProduce.WaitTime = ProduceTime;
        TimerProduce.OneShot = true;
        TimerProduce.Timeout += _Light;
        
        TimerProduce.Start();
    }
}

