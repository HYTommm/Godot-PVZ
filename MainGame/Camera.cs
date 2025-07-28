using Godot;
using System;

public partial class Camera : Camera2D
{
	[Signal]
	public delegate void MoveEndEventHandler();

	public Vector2 StartMovePos = Vector2.Zero;
	public Vector2 EndMovePos = Vector2.Zero;

	public double Time = 0;
	public bool BIsMoving = false;
	public double X = 0;
	public double A, Xh, Yh;
	public double Halfa;
	public double FPS = 60;

    // 震动参数
    [Export] public float MaxShakeIntensity = 10.0f; // 最大震动强度
    [Export] public float ShakeDuration = 0.15f;      // 单次震动持续时间
    [Export] public int ShakeSteps = 3;              // 震动次数

    private Vector2 _originalPosition;
    private bool _isShaking = false;
    private readonly Random _random = new Random();

    public override void _Ready()
    {
        _originalPosition = Position;
    }

	public override void _Process(double delta)
	{
		if (BIsMoving)
		{
			X += delta * FPS;
			double tempPosX = (-(Xh / (Halfa*Halfa)) * (X - Halfa - 0.2) * (X - Halfa - 0.2) + Xh) * delta * FPS;
			//double tempPosX = xh*delta * FPS;
			Vector2 tempPos = new((float)tempPosX, 0);
			Position += tempPos;
			if (Mathf.Abs(X - Time * FPS) <= 1)
			{
				BIsMoving = false;
				Position = EndMovePos;
				EmitSignal(SignalName.MoveEnd);
			}
		}
		
	}

	public void Move(Vector2 pos, double time)
	{
		
		BIsMoving = true;
		StartMovePos = Position;
		EndMovePos = pos;
		Time = time;
		A = time * FPS;
		Halfa = time * FPS / 2;
		X = 0;
		Xh = (EndMovePos.X - StartMovePos.X) / A*3/2;
		//xh = (EndMovePos.X - StartMovePos.X) / a;
		Yh = Mathf.Abs(StartMovePos.Y - EndMovePos.Y) / A*3/2;

	}

    // 触发相机震动
    public void Shake(float intensity = 0.5f)
    {
        if (_isShaking) return;
        _originalPosition = Position;
        _isShaking = true;
        intensity = Mathf.Clamp(intensity, 0.1f, 1.0f);

        // 创建主Tween序列
        var sequence = CreateTween();

        for (int i = 0; i < ShakeSteps; i++)
        {
            // 计算当前震动强度（随次数衰减）
            float stepIntensity = intensity * (1.0f - (float)i / ShakeSteps);

            // 生成随机偏移方向
            Vector2 shakeDirection = new Vector2(
                (float)(_random.NextDouble() * 2 - 1),
                (float)(_random.NextDouble() * 2 - 1)
            ).Normalized();

            // 计算偏移量
            Vector2 targetOffset = shakeDirection * MaxShakeIntensity * stepIntensity;

            // 添加震动步骤
            sequence.TweenProperty(this, "position", _originalPosition + targetOffset, ShakeDuration / ShakeSteps)
                .SetTrans(Tween.TransitionType.Linear)
                .SetEase(Tween.EaseType.Out);

            // 添加返回步骤
            sequence.TweenProperty(this, "position", _originalPosition, ShakeDuration / ShakeSteps)
                .SetTrans(Tween.TransitionType.Linear)
                .SetEase(Tween.EaseType.In);
        }

        // 震动结束后复位
        sequence.TweenCallback(Callable.From(() => {
            Position = _originalPosition;
            _isShaking = false;
        }));
    }
}
