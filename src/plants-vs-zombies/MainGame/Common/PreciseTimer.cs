public class PreciseTimer
{
    private double _remaining;

    public bool IsActive => _remaining > 0.0;

    public void Start(double duration)
    {
        _remaining = duration;
    }

    public void Stop()
    {
        _remaining = 0.0;
    }

    /// <summary>
    /// 每帧调用，传入 delta。
    /// </summary>
    /// <returns>true 表示计时器刚好在这一帧到期</returns>
    public bool Tick(double delta)
    {
        if (_remaining <= 0.0)
            return false;

        _remaining -= delta;
        if (_remaining <= 0.0)
        {
            _remaining = 0.0;
            return true;
        }

        return false;
    }
}
