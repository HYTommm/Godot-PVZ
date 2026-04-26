using Godot;
using Godot.Collections;

public abstract partial class Zombie : Entity, IHealthStage, IStatusEffect
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

        if (ActiveEffectsCount == 0 && !IsAnimationPlaying) // 没有活跃的效果和动画，可以直接释放
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