using Godot;

/// <summary>
/// 旗帜僵尸。血量与普僵同值（Zombie 上 HealthStageComponent 默认 270），不挂任何防具。
///
/// 外观上用 Zombie_flaghand + Zombie_innerarm_screendoor 顶掉普通僵尸的三段内臂
/// （Anim_innerArm1/2/3）。这三个内臂引用来自父类 RegularZombie，不在这里重复声明。
///
/// 旗子是个独立场景，挂在 Zombie 节点下（不是挂在手上，理由见 _PhysicsProcess）。
/// </summary>
public partial class FlagZombie : TieZombie
{
    [Export] public Sprite2D Zombie_flaghand;
    [Export] public Sprite2D Zombie_innerarm_screendoor;
    [Export] public Node2D Flagpole;
    [Export] public AnimationPlayer FlagpoleAnim;

    /// <summary>旗子轨道名。转换器对只有一个动画段的文件给的就是这个名字</summary>
    private const string FlagpoleAnimName = "ALL_ANIMS";

    /// <summary>旗子飘动速率。原版 15.0，本项目的 .tres 基准是 12fps，所以 15 ÷ 12</summary>
    private const float FlagpoleSpeed = 15f / 12f;

    /// <summary>手在基准帧（第 0 帧）变换的逆矩阵，供每帧算额外变换用</summary>
    private Transform2D _handBaseInverse;

    public override void _Ready()
    {
        // 这一句必须在 base._Ready() 之前：父类会在那里把走路动画播起来，
        // 之后再读 Zombie_flaghand，拿到的就是被动画改过的值，不再是基准帧的了。
        _handBaseInverse = Zombie_flaghand.Transform.AffineInverse();

        base._Ready();

        Zombie_flaghand.Visible = true;
        Zombie_innerarm_screendoor.Visible = true;

        Anim_innerArm1.Visible = false;
        Anim_innerArm2.Visible = false;
        Anim_innerArm3.Visible = false;

        StartFlagpoleAnim();
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        // 旗子只跟「手相对基准帧的额外变换」走。语义是把旗子上的点从基准坐标系搬到当前
        // 坐标系，即「先按基准的逆把点拉回手的基准系，再按手的当前变换放出去」，
        // 也就是 T ∘ B⁻¹。Godot 的 A * B 是「先 B 后 A」，所以当前变换写在左边。
        //
        // 不能图省事把旗子挂成 Zombie_flaghand 的子节点：旗子的关键帧坐标是绝对坐标，
        // 基准状态下手的位置已经烘进那套坐标里了，再叠一层手的完整变换等于把这份基准
        // 偏移算两遍，错的量正好是手在第 0 帧的坐标。
        Flagpole.Transform = Zombie_flaghand.Transform * _handBaseInverse;
    }

    /// <summary>
    /// 让旗子飘起来。转换产物里的动画默认不循环，而原版那条旗子轨道是循环播放的，
    /// 所以这里补上循环模式，再按原版速率折算后播放。
    /// </summary>
    private void StartFlagpoleAnim()
    {
        // 注意：Godot.Animation 要写全限定名。父类 RegularZombie 有个字段就叫 Animation
        // （类型是 AnimationPlayer），不限定的话这个名字会被它遮蔽。
        Godot.Animation anim = FlagpoleAnim.GetAnimation(FlagpoleAnimName);
        if (anim != null)
            anim.LoopMode = Godot.Animation.LoopModeEnum.Linear;

        FlagpoleAnim.SpeedScale = FlagpoleSpeed;
        FlagpoleAnim.Play(FlagpoleAnimName);
    }

    /// <summary>
    /// 旗帜僵尸的移速是**固定值**，不参与随机（原版与舞王/伴舞/跳跳同组）。
    ///
    /// 平均速度 = 47 × 0.45 = 21.15 px/s。本体用的是普通僵尸的走路动画，
    /// 该动画 S = 1 时是 12.99 px/s（49.8px ÷ 3.83333s），所以 customSpeed = 21.15 ÷ 12.99。
    /// </summary>
    protected override void PickRandomSpeed()
    {
        WalkSpeed = 47f * 0.45f / 12.9913f;
    }

    public override void Init()
    {
        GD.Print("FlagZombie Init");
    }
}
