using Godot;
using static ResourceManager;
using static Godot.GD;
using System;

public partial class Sun : Drops
{
	[Export]
	public int GroundPosY = 0;

	public bool isFalling = true;
	public AudioStreamPlayer SunSelectSound = new();

	public MainGame MainGame;

	public override void _Process(double delta)
	{
		if (isFalling && GroundPosY > Position.Y)
		{
			Position += new Vector2(0, 60 * (float)delta);
		}
	}

	public override void _Ready()
	{
		SunSelectSound.Stream = Sounds.Sound_Points;
		AddChild(SunSelectSound);
		MainGame = this.GetMainGame();
	}
	public void SetGroundPosY(int y)
	{
		GroundPosY = y;
	}

	public void OnInputEvent(Node viewport, InputEvent inputevent, int shape_idx)
	{
		if (inputevent.IsAction("mouse_left"))
		{
			SelectSun();
		}

	}

	async public void SelectSun()
	{

		//GD.Print("Sun Selected");

		// 增加 MainGame 的 Sun 的数量
		MainGame.SunCount += 25;

		// 停止下落
		isFalling = false;
		// 隐藏 碰撞体
		GetNode<Area2D>("Area2D").Visible = false;

		SunSelectSound.PitchScale = MainGame.RNG.RandfRange(1.0f, 1.5f);
		SunSelectSound.Play();
		
		Vector2 SeedBankSunPos = GetNode<Sprite2D>("../SeedBank").Position + new Vector2(40, 35);

		// 移动 Sun 到 SeedBank 位置
		Tween tweenPos = CreateTween();
		tweenPos
			.TweenProperty(this, "position", SeedBankSunPos, 0.7f)
			.SetEase(Tween.EaseType.Out)
			.SetTrans(Tween.TransitionType.Cubic)
			.Finished += FinishSelectSun;
		await ToSignal(GetTree().CreateTimer(0.5f), "timeout");

		// 缩小 Sun 到 0.3 倍大小
		Tween tweenScale = CreateTween();
		tweenScale
			.TweenProperty(this, "scale", new Vector2(0.3f, 0.3f), 0.6f)
			.SetEase(Tween.EaseType.InOut)
			.SetTrans(Tween.TransitionType.Linear);

		// 降低 Sun 的透明度
		Tween tweenAlpha = CreateTween();
		tweenAlpha
			.TweenProperty(this, "modulate", new Color(1, 1, 1, 0.4f), 0.6f)
			.SetEase(Tween.EaseType.Out)
			.SetTrans(Tween.TransitionType.Cubic)
			.Finished += FreeSun;
		//GD.Print("finish tween");
	}

	public void FinishSelectSun()
	{
		// 通知 SeedBank 更新 Sun 数量
		GetNode<SeedBank>("../SeedBank").UpdateSunCount();
	}

	public void FreeSun()
	{
		// 销毁 Sun
		Visible = false;
		QueueFree();
	}

	public override void _Drop()
	{
		Tween tweenPos = CreateTween();
        Tween tweenScale = CreateTween();

        float rng_x = MainGame.RNG.RandfRange(-20, 20);
		float rng_y = MainGame.RNG.RandfRange( 40, 70);

		if (rng_x < 0) rng_x -= 5;
		else           rng_x += 5;

		
		tweenScale
			.TweenProperty(this, "scale", new Vector2(1, 1), 0.25f)
			.SetEase(Tween.EaseType.InOut)
			.SetTrans(Tween.TransitionType.Linear);

		tweenPos
			.TweenProperty(this, "position", new Vector2(Position.X + rng_x, Position.Y - rng_y), 0.2f)
			.SetEase(Tween.EaseType.OutIn)
			.SetTrans(Tween.TransitionType.Linear);

		tweenPos
			.TweenProperty(this, "position", new Vector2(Position.X + rng_x * 1.5f, Position.Y + 45), 0.3f)
			.SetEase(Tween.EaseType.In)
			.SetTrans(Tween.TransitionType.Linear);
	}
}
