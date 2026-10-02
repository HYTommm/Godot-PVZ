using Godot;
using Godot.Collections;

public abstract partial class Zombie : Entity, IHealthStage, IStatusEffect, ISeedEntity
{
    /// <summary>僵尸死亡事件</summary>
    [Signal]
    public delegate void ZombieDyingEventHandler();

    // 状态标志
    /// <summary>是否正在移动</summary>
    [Export] public bool BIsMoving = true;

    /// <summary>是否濒死</summary>
    [Export] public bool BIsDying = false;

    /// <summary>是否死亡</summary>
    [Export] public bool BIsDead = false;

    /// <summary>
    /// 只用于种子栏卡槽展示，不参与出场。
    ///
    /// 僵尸场景的 _Ready 是照"已经走进草坪"写的——抽行走速度（要用 MainGame.Instance.RNG）、
    /// 取防御与攻击碰撞箱、接动画与粒子信号、实例化焦炭场景；而卡槽把它们实例出来只是显示外观。
    /// 所以各僵尸类的 _Ready 都要先看这个标志。要在 AddChild 之前设置，否则 _Ready 已经跑过了。
    /// </summary>
    public bool BIsDisplayOnly { get; set; } = false;

    // ── ISeedEntity：卡槽只在意的四个数值 ──

    /// <summary>僵尸卡免费</summary>
    public int SunCost => 0;

    /// <summary>僵尸卡不冷却：调试时连着放才顺手</summary>
    public float CDtime => 0f;

    /// <summary>跟随鼠标时，僵尸原点相对鼠标的偏移</summary>
    public Vector2 Offset => new(35, 60);

    /// <summary>整身透明度，用于手上的半透明预览</summary>
    public void _SetAlpha(float alpha) => Modulate = new Color(1, 1, 1, alpha);

    public bool IsReleaseRequested;
    public bool IsAnimationPlaying;

    // 内部计数与控制
    public int ActiveEffectsCount = 0; // 当前活跃的效果（包括动画和粒子）数量

    // 数值与游戏相关字段
    /// <summary>所处波数</summary>
    public int Wave;

    /// <summary>所处行</summary>
    public int Row = -1;

    // 命中盒/区域
    /// <summary>防御区域节点</summary>
    public IHitBox DefenseHitBox;

    /// <summary>攻击区域节点</summary>
    public IHitBox AttackHitBox;

    /// <summary>血量阶段组件</summary>
    [Export] public HealthStageComponent HealthStageComponent { get; set; } = new(270);

    public bool Alive => HealthStageComponent.HP > 0;
    public int HP => HealthStageComponent.HP;
    public int MaxHP => HealthStageComponent.MaxHP;

    /// <summary>状态效果管理器</summary>
    public StatusEffectManager StatusEffectManager { get; } = new();

    /// <summary>
    /// 由外部管理器调用，授权怪物释放自己。
    /// </summary>
    public void RequestRelease()
    {
        if (IsReleaseRequested) return;   // 已请求过，防止重复
        IsReleaseRequested = true;

        // 用 <= 0 而不是 == 0：计数一旦被某个多还的 Finished 扣成负数，== 0 就永远不成立，僵尸再也放不掉
        if (ActiveEffectsCount <= 0 && !IsAnimationPlaying) // 没有活跃的效果和动画，可以直接释放
            QueueFree();
        // 否则等待粒子结束时（在 OnParticleFinished 中处理）
    }

    public virtual void Hurt(Hurt hurt) => (this as IHealthStage).Hurt(hurt); // 调用接口的Hurt方法

    /// <summary>
    /// 刷新僵尸
    /// </summary>
    /// <param name="index"></param>
    /// <param name="scene"></param>
    /// <param name="wave"></param>
    /// <param name="row"></param>
    public void Refresh(int index, Scene scene, int wave, int row)
    {
        //HP = MaxHP;
        //Index = index;
        Wave = wave;
        // 随机行

        Row = row;
        // 设置调试信息
        GetNode<TextEdit>("./TextEdit").Text = Index.ToString();
        Position = new Vector2(1050, scene.LawnLeftTopPos.Y + Row * scene.LawnUnitWidth - 35);
    }

    public override void SetZIndex()
    {
        GetParent().MoveChild(this, Index);
        ZIndex = (Row + 1) * 10 + (int)ZIndexEnum.Zombies;
    }

    public virtual void Init()
    {
        GD.Print("Zombie Constructor called");
    }
}
