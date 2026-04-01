using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// 泛型状态机，支持延迟切换和循环模式。
/// </summary>
/// <typeparam name="TState">状态枚举类型</typeparam>
public partial class StateMachine<TState> : Node where TState : Enum
{
    /// <summary>
    /// 状态改变事件，参数为新状态。
    /// </summary>
    public event Action<TState>? StateChanged;

    private TState _currentState;
    private TState _nextState = default!;
    private double _timeRemaining = -1;
    private bool _hasNextState = false;
    private bool _cycleMode = false;

    // 转换字典：源状态 → (目标状态, 延迟时间)
    private readonly Dictionary<TState, (TState Next, double Time)> _transitions = new();

    // 公开只读属性
    public TState CurrentState => _currentState;

    public bool HasNextState => _hasNextState;
    public double TimeRemaining => _timeRemaining;

    /// <summary>
    /// 构造函数，需要提供初始状态。
    /// </summary>
    public StateMachine(TState initialState)
    {
        _currentState = initialState;
    }

    /// <summary>
    /// 手动安排一个状态切换（立即取消当前安排）。
    /// 如果当前处于循环模式，将自动退出循环。
    /// </summary>
    /// <param name="state">目标状态</param>
    /// <param name="time">延迟秒数（非负）</param>
    public void SetNextState(TState state, double time)
    {
        // 手动安排时，退出循环模式，避免冲突
        _cycleMode = false;

        _nextState = state;
        _timeRemaining = Math.Max(0, time);
        _hasNextState = true;
    }

    /// <summary>
    /// 添加或替换一条转换规则。
    /// </summary>
    /// <param name="from">源状态</param>
    /// <param name="to">目标状态</param>
    /// <param name="time">延迟秒数（非负）</param>
    public void AddTransition(TState from, TState to, double time)
    {
        _transitions[from] = (to, Math.Max(0, time));
    }

    /// <summary>
    /// 构建一个循环：给定状态列表，依次转换，最后一个转回第一个。
    /// 如果 states 为空，则不修改现有转换字典。
    /// </summary>
    /// <param name="states">状态列表（至少一个）</param>
    /// <param name="time">每个转换的延迟秒数（非负）</param>
    public void BuildCycle(IList<TState> states, double time)
    {
        if (states == null || states.Count == 0)
            return;

        _transitions.Clear();
        double safeTime = Math.Max(0, time);
        for (int i = 0; i < states.Count; i++)
        {
            var from = states[i];
            var to = states[(i + 1) % states.Count];
            _transitions[from] = (to, safeTime);
        }
    }

    /// <summary>
    /// 启动循环模式。会从给定起始状态开始，根据转换字典自动循环。
    /// 如果起始状态没有定义转换，将不会安排任何状态切换，但循环模式仍开启。
    /// </summary>
    /// <param name="start">起始状态</param>
    public void StartCycle(TState start)
    {
        // 停止当前任何待处理的状态切换
        CancelNextState();

        _cycleMode = true;
        _currentState = start;

        if (_transitions.TryGetValue(_currentState, out var entry))
        {
            SetNextStateInternal(entry.Next, entry.Time);
        }
    }

    /// <summary>
    /// 停止循环模式，并取消任何待处理的状态切换。
    /// </summary>
    public void StopCycle()
    {
        _cycleMode = false;
        CancelNextState();
    }

    /// <summary>
    /// 取消当前待处理的状态切换（不影响循环模式标志）。
    /// </summary>
    public void CancelNextState()
    {
        _hasNextState = false;
        _timeRemaining = -1;
        _nextState = default!;
    }

    /// <summary>
    /// 立即强制切换到指定状态（不延迟，不依赖转换字典）。
    /// 会触发 StateChanged 事件，并重置当前安排。如果处于循环模式，此操作会退出循环。
    /// </summary>
    public void ForceSetState(TState state)
    {
        // 强制切换时退出循环模式，避免干扰
        _cycleMode = false;
        CancelNextState();

        _currentState = state;
        StateChanged?.Invoke(state);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (!_hasNextState) return;

        _timeRemaining -= delta;
        if (_timeRemaining > 0) return;

        // 时间到，执行状态切换
        _hasNextState = false;
        _timeRemaining = -1;

        TState newState = _nextState;

        // 触发事件（注意：事件处理中可能再次调用 SetNextState 等，需保证状态机内部状态已更新）
        StateChanged?.Invoke(newState);

        // 更新当前状态
        _currentState = newState;

        // 如果处于循环模式，尝试安排下一个状态
        if (_cycleMode)
        {
            if (_transitions.TryGetValue(_currentState, out var entry))
            {
                SetNextStateInternal(entry.Next, entry.Time);
            }
            else
            {
                // 当前状态没有定义下一个转换，自动停止循环
                _cycleMode = false;
            }
        }
    }

    /// <summary>
    /// 内部方法：安排下一个状态（不改变循环模式标志）。
    /// </summary>
    private void SetNextStateInternal(TState state, double time)
    {
        _nextState = state;
        _timeRemaining = Math.Max(0, time);
        _hasNextState = true;
    }
}