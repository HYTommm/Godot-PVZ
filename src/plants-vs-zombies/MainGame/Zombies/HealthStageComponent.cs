using Godot;
using Godot.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static HealthStage;

[GlobalClass]
public partial class HealthStageComponent : Resource
{
    /// <summary>生命值</summary>
    [Export] public int HP;

    /// <summary>最大生命值</summary>
    [Export] public int MaxHP;

    private Array<HealthStage> _healthStages = [];

    [Export]
    public Array<HealthStage> HealthStages
    {
        get => _healthStages;
        set
        {
            _healthStages = value ?? [];
            Refresh();
        }
    }

    // 运行时动态添加的阶段（BindAction）
    private readonly System.Collections.Generic.Dictionary<int, HealthStage> _bindStages = new();

    // 用户配置的阶段（来自 HealthStages 列表）
    private readonly SortedList<int, HealthStage> _configStages = new();

    // 默认阶段（仅当 HealthStages 为空时启用）
    private DefaultStages _defaultStages;

    private bool _useDefaultStages; // true 表示用默认阶段，false 表示用用户配置

    public DefaultStages Defaults => _defaultStages;

    // 运行时数据
    //private readonly SortedList<int, HealthStage> _stageDic = new();

    private bool _initialized = false;

    public HealthStageComponent(int maxHP) : this(maxHP, maxHP)
    {
    }

#pragma warning disable HP001

    public HealthStageComponent() : this(0, 0)
#pragma warning restore HP001
    {
    }

    public HealthStageComponent(int hp, int maxHP)
    {
        HP = hp;
        MaxHP = maxHP;
        Refresh(); // 初始刷新，根据当前配置决定使用默认阶段
        ResourceLocalToScene = true;
    }

    /// <summary>
    /// 初始化阶段数据（从导出字段创建）
    /// </summary>
    private void Initialize()
    {
        if (_initialized) return;
        Refresh();
    }

    [Flags]
    public enum StageRefreshFlags
    {
        None = 0,

        /// <summary>完全重建：丢弃所有旧实例，仅使用 _healthStages 列表（忽略其他标志）</summary>
        FullRebuild = 1 << 0,

        /// <summary>保留同阈值的旧实例（若设置此项，则 KeepOldActions 和 KeepTriggeredState 无效）</summary>
        KeepOldInstances = 1 << 1,

        /// <summary>当使用新实例时，迁移旧实例的 Action</summary>
        KeepOldActions = 1 << 2,

        /// <summary>当使用新实例时，迁移旧实例的 HasTriggered 状态</summary>
        KeepTriggeredState = 1 << 3,

        /// <summary>删除所有未在新列表中出现的旧阶段（阈值不匹配的旧阶段将被彻底移除）</summary>
        KeepUnmatchedOldStages = 1 << 4,

        // 预定义常用组合
        Default = KeepOldInstances | KeepUnmatchedOldStages,                                    // 默认行为：保留旧实例

        MigrateActionsOnly = KeepOldActions,                           // 替换实例，只迁移 Action
        MigrateWithState = KeepOldActions | KeepTriggeredState,        // 替换实例，迁移 Action 和触发状态
    }

    /// <summary>
    /// 刷新内部阶段字典。当编辑器中修改 HealthStages 或需要重建时调用。
    /// </summary>
    /// <param name="flags">刷新行为标志组合</param>
    public void Refresh(StageRefreshFlags flags = StageRefreshFlags.Default)
    {
        GD.Print($"HealthStageComponent: 刷新阶段配置，Flags={flags}");
        if (_healthStages.Count > 0)
        {
            _useDefaultStages = false;
            RefreshConfigStages(flags);
        }
        else
        {
            _useDefaultStages = true;
            RefreshDefaultStages(flags);
        }
    }

    private void RefreshConfigStages(StageRefreshFlags flags)
    {
        if (flags.HasFlag(StageRefreshFlags.FullRebuild))
        {
            _configStages.Clear();
            foreach (HealthStage stage in _healthStages)
            {
                if (stage != null)
                    _configStages[stage.Threshold] = stage;
            }
            return;
        }

        bool keepOldInstances = flags.HasFlag(StageRefreshFlags.KeepOldInstances);
        bool keepOldActions = !keepOldInstances && flags.HasFlag(StageRefreshFlags.KeepOldActions);
        bool keepTriggeredState = !keepOldInstances && flags.HasFlag(StageRefreshFlags.KeepTriggeredState);
        bool keepUnmatched = flags.HasFlag(StageRefreshFlags.KeepUnmatchedOldStages);

        System.Collections.Generic.Dictionary<int, HealthStage> prev = new(_configStages);
        _configStages.Clear();

        foreach (HealthStage stage in _healthStages)
        {
            if (stage == null) continue;
            int t = stage.Threshold;

            if (!prev.TryGetValue(t, out HealthStage old))
            {
                _configStages[t] = stage;
                continue;
            }

            if (keepOldInstances)
            {
                _configStages[t] = old;
                prev.Remove(t);
                continue;
            }

            if (!ReferenceEquals(stage, old))
            {
                if (keepOldActions) MigrateActions(old, stage);
                if (keepTriggeredState) stage.HasTriggered = old.HasTriggered;
            }
            _configStages[t] = stage;
            prev.Remove(t);
        }

        // 根据标志决定是否回填未被匹配的旧阶段
        if (keepUnmatched)
        {
            foreach ((int key, HealthStage value) in prev)
            {
                _configStages[key] = value;
            }
        }

        RebuildSortedStages();
    }

    private void RefreshDefaultStages(StageRefreshFlags flags)
    {
        bool keepOldActions = flags.HasFlag(StageRefreshFlags.KeepOldActions);
        DefaultStages oldDefaults = _defaultStages;
        _defaultStages.Refresh(MaxHP, oldDefaults, keepOldActions);
        RebuildSortedStages();
    }

    /// <summary>显式强制切换到用户配置模式（需要 _healthStages 不为空）</summary>
    public void UseConfigStages()
    {
        if (_healthStages.Count == 0)
        {
            GD.PushWarning("HealthStageComponent: 尝试使用配置阶段，但 _healthStages 为空。将保持默认阶段。");
            return;
        }
        _useDefaultStages = false;
        RefreshConfigStages(StageRefreshFlags.Default);
    }

    /// <summary>显式强制切换到默认阶段模式</summary>
    public void UseDefaultStages()
    {
        _useDefaultStages = true;
        RefreshDefaultStages(StageRefreshFlags.Default);
    }

    // ========== 动态绑定 ==========
    public void BindAction(int threshold, Action<Hurt> action)
    {
        if (!_bindStages.TryGetValue(threshold, out var stage))
        {
            stage = new HealthStage
            {
                Threshold = threshold,
                TriggerType = TriggerTypeEnum.CrossBelow,
                TriggerOnce = false,
            };
            _bindStages[threshold] = stage;
        }
        stage.Action += action;
        RebuildSortedStages();
    }

    public void UnbindAction(int threshold, Action<Hurt> action)
    {
        if (_bindStages.TryGetValue(threshold, out var stage))
        {
            stage.Action -= action;
            if (stage.Action == null)
                _bindStages.Remove(threshold);
        }
        RebuildSortedStages();
    }

    private readonly List<HealthStage> _sortedStages = [];

    private void RebuildSortedStages()
    {
        GD.Print("HealthStageComponent: 重建排序阶段列表");
        _sortedStages.Clear();

        // 获取当前配置阶段源
        IEnumerable<HealthStage> configStages = _useDefaultStages
            ? (IEnumerable<HealthStage>)_defaultStages
            : _configStages.Values;

        // 合并动态阶段
        // 使用 List.AddRange + 排序 代替 LINQ 来减少分配
        _sortedStages.AddRange(configStages);
        _sortedStages.AddRange(_bindStages.Values);
        _sortedStages.Sort((a, b) => a.Threshold.CompareTo(b.Threshold)); // 降序
    }

    // ========== 伤害处理 ==========
    public void TakeDamage(Hurt hurt)
    {
        int previousHealth = HP;
        int damage = Math.Min(hurt.Damage, HP);
        HP -= damage;
        hurt.Damage -= damage;

        foreach (HealthStage stage in _sortedStages)
        {
            if (stage.TriggerOnce && stage.HasTriggered)
                continue;

            if (ShouldTrigger(stage, previousHealth))
            {
                if (stage.TriggerOnce) stage.HasTriggered = true;
                stage.Action?.Invoke(hurt);
            }
        }
    }

    private bool ShouldTrigger(HealthStage stage, int previousHealth)
    {
        return stage.TriggerType switch
        {
            TriggerTypeEnum.Below => HP < stage.Threshold,
            TriggerTypeEnum.BelowOrEqual => HP <= stage.Threshold,
            TriggerTypeEnum.Above => HP > stage.Threshold,
            TriggerTypeEnum.AboveOrEqual => HP >= stage.Threshold,
            TriggerTypeEnum.CrossBelow => previousHealth >= stage.Threshold && HP < stage.Threshold,
            TriggerTypeEnum.CrossBelowOrEqual => previousHealth > stage.Threshold && HP <= stage.Threshold,
            TriggerTypeEnum.CrossAbove => previousHealth <= stage.Threshold && HP > stage.Threshold,
            TriggerTypeEnum.CrossAboveOrEqual => previousHealth < stage.Threshold && HP >= stage.Threshold,
            _ => false
        };
    }

    // ========== 工具方法：迁移事件 ==========
    private static void MigrateActions(HealthStage from, HealthStage to) => to.Action += from?.Action;

    // ========== 状态重置 ==========
    public void ResetAll()
    {
        foreach (var stage in _bindStages.Values) stage.HasTriggered = false;
        foreach (var stage in _configStages.Values) stage.HasTriggered = false;
        if (_defaultStages.Enabled)
        {
            foreach (HealthStage stage in _defaultStages)
                stage.HasTriggered = false;
        }
    }

    // ========== 辅助方法：公开当前阈值列表（仅配置源） ==========
    public IEnumerable<int> GetConfigThresholds()
    {
        return _useDefaultStages
            ? [0, MaxHP / 3, 2 * MaxHP / 3] // 注意阈值可能与实际阶段实例不完全一致，若 MaxHP 变化后未刷新需注意
            : _configStages.Keys;
    }

    public struct DefaultStages : IEnumerable<HealthStage>
    {
        public HealthStage StageZero { get; private set; }  // 阈值 0
        public HealthStage StageLow { get; private set; }  // 阈值 MaxHP/3
        public HealthStage StageHigh { get; private set; }  // 阈值 2*MaxHP/3

        public bool Enabled { get; private set; }

        /// <summary>
        /// 根据当前最大生命值生成（或更新）默认阶段实例。
        /// 若传入的 keepOldActions 为 true，且已有旧实例，则尝试迁移 Action 事件。
        /// </summary>
        public void Refresh(int maxHP, DefaultStages? oldStages = null, bool keepOldActions = true)
        {
            Enabled = true;

            StageZero = CreateOrMigrate(oldStages?.StageZero, 0, TriggerTypeEnum.CrossBelowOrEqual);
            StageLow = CreateOrMigrate(oldStages?.StageLow, maxHP / 3, TriggerTypeEnum.CrossBelow);
            StageHigh = CreateOrMigrate(oldStages?.StageHigh, maxHP * 2 / 3, TriggerTypeEnum.CrossBelow);

            HealthStage CreateOrMigrate(HealthStage old, int threshold, TriggerTypeEnum type)
            {
                HealthStage stage = new()
                {
                    Threshold = threshold,
                    TriggerType = type,
                    TriggerOnce = true
                };
                if (old != null && !keepOldActions && old.Action != null)
                {
                    stage.Action = old.Action; // 迁移 Action
                }
                return stage;
            }
        }

        // 遍历支持
        public IEnumerator<HealthStage> GetEnumerator()
        {
            if (!Enabled) yield break;
            yield return StageZero;
            yield return StageLow;
            yield return StageHigh;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
