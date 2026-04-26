using System;
using Godot;

[GlobalClass]
public partial class HealthStage : Resource
{
    /// <summary>
    /// 触发类型
    /// </summary>
    public enum TriggerTypeEnum
    {
        /// <summary>当前血量低于阈值时触发</summary>
        Below,

        /// <summary>当前血量低于或等于阈值时触发</summary>
        BelowOrEqual,

        /// <summary>当前血量高于或等于阈值时触发</summary>
        AboveOrEqual,

        /// <summary>当前血量高于阈值时触发</summary>
        Above,

        /// <summary>血量从高于或等于阈值变为低于阈值时触发</summary>
        CrossBelow,

        /// <summary>血量从高于阈值变为低于或等于阈值时触发</summary>
        CrossBelowOrEqual,

        /// <summary>血量从低于或等于阈值变为高于阈值时触发</summary>
        CrossAbove,

        /// <summary>血量从低于阈值变为高于或等于阈值时触发</summary>
        CrossAboveOrEqual
    }

    /// <summary>触发血量阈值</summary>
    [Export] public int Threshold { get; set; } = 0;

    /// <summary>触发类型</summary>
    [Export] public TriggerTypeEnum TriggerType = TriggerTypeEnum.CrossBelow;

    /// <summary>是否只触发一次</summary>
    [Export] public bool TriggerOnce = true;

    /// <summary>是否已经触发过</summary>
    [Export] public bool HasTriggered;

    /// <summary>动作委托</summary>
    public Action<Hurt> Action;
}