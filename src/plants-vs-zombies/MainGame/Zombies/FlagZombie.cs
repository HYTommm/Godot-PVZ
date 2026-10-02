using Godot;

/// <summary>
/// 旗帜僵尸。血量与普僵同值（Zombie 上 HealthStageComponent 默认 270），不挂任何防具。
///
/// 外观上用 Zombie_flaghand + Zombie_innerarm_screendoor 顶掉普通僵尸的三段内臂
/// （Anim_innerArm1/2/3）。这三个内臂引用来自父类 RegularZombie，不在这里重复声明。
/// </summary>
public partial class FlagZombie : TieZombie
{
    [Export] public Sprite2D Zombie_flaghand;
    [Export] public Sprite2D Zombie_innerarm_screendoor;

    public override void _Ready()
    {
        base._Ready();

        Zombie_flaghand.Visible = true;
        Zombie_innerarm_screendoor.Visible = true;

        Anim_innerArm1.Visible = false;
        Anim_innerArm2.Visible = false;
        Anim_innerArm3.Visible = false;
    }

    public override void Init()
    {
        GD.Print("FlagZombie Init");
    }
}
