using Godot;

public partial class FootballZombie : RegularZombie
{
	private enum FootballState
	{
		Walk,
		Eat,
		Dying,
		Dead
	}

	private StateMachine<FootballState> _stateMachine;
	private HurtType _pendingDeathHurtType = HurtType.Direct;

	[Export] public Sprite2D Zombie_Football;

	public override string WalkAnimationName => "Zombie_football/walk";
	public override string LawnMowerDeathAnimationName => "Zombie_football/LawnMoweredZombie"; // 命名库必须带库名前缀
	public override string EatAnimationName => "Zombie_football/eat";
	public override string DeathAnimationName => "Zombie_football/death";

	public override void _Ready()
	{
		AddChild(_stateMachine = new StateMachine<FootballState>(FootballState.Walk));
		_stateMachine.StateChanged += OnStateChanged;

		base._Ready();

		FootballHelmet footballhelmet = new(
			Zombie_Football,
			[],
			[Zombie_hair]);
		ArmorManager.AddArmor(footballhelmet);

		// 粒子播完归还 ActiveEffectsCount，否则计数不归零、僵尸不释放
		ZombieArmParticles.Finished += OnEffectsFinished;
		ZombieHeadParticles.Finished += OnEffectsFinished;

		_stateMachine.ForceSetState(FootballState.Walk);
	}

	protected override void PickRandomSpeed()
	{
		// PVZ：动画速率 = 速度系数 × (循环帧数 / 地面循环位移) × 47
		// 橄榄球速度系数 ∈ [0.66, 0.68]，本动画 29 帧 / 地面循环位移 30px
		// → 动画速率 = 速度系数 × 29/30 × 47 ≈ [29.99, 30.89] fps
		// → customSpeed = 动画速率 / 12 ≈ [2.4988, 2.5746]（平均速度 31.0–32.0 px/s）
		WalkSpeed = MainGame.Instance.RNG.RandfRange(2.4988f, 2.5746f);
	}

	/// <summary>PVZ 橄榄球死亡速率 24 fps → customSpeed 2.0</summary>
	protected override float GetDeathAnimSpeed() => 2.0f;

	/// <summary>半血：隐藏左小臂/左手（基类）+ 左上臂换断臂贴图 + 断臂粒子</summary>
	protected override void OnHealthStageHigh()
	{
		base.OnHealthStageHigh();
		Zombie_outerarm_upper.Texture = GD.Load<Texture2D>(
			"res://art/MainGame/Zombie/FootballZombie/zombie_football_leftarm_upper2.png");
		ZombieArmParticles?.SetDeferred("emitting", true);
		ActiveEffectsCount++;
	}

	/// <summary>濒死：隐藏头/下巴/头发（基类）+ 断头粒子</summary>
	protected override void OnDyingStarted()
	{
		base.OnDyingStarted();
		ZombieHeadParticles?.SetDeferred("emitting", true);
		ActiveEffectsCount++;
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);

		switch (_stateMachine.CurrentState)
		{
			case FootballState.Walk:
				if (base.UpdateWalking(delta))
					_stateMachine.ForceSetState(FootballState.Eat);
				break;

			case FootballState.Eat:
				if (!base.UpdateEating(delta))
					_stateMachine.ForceSetState(FootballState.Walk);
				break;

			case FootballState.Dying:
				base.UpdateDying(delta);
				break;

			case FootballState.Dead:
				break;
		}
	}

	private void OnStateChanged(FootballState newState)
	{
		switch (newState)
		{
			case FootballState.Walk:
				base.StartWalking();
				break;

			case FootballState.Eat:
				base.StartEating();
				break;

			case FootballState.Dying:
				base.StartDying();
				break;

			case FootballState.Dead:
				base.StartDead(_pendingDeathHurtType);
				break;
		}
	}

	public override void StartDying()
	{
		if (BIsDying || BIsDead) return;
		if (_stateMachine.CurrentState != FootballState.Dying)
		{
			_stateMachine.ForceSetState(FootballState.Dying);
			return;
		}
		base.StartDying();
	}

	public override void StartDead(HurtType hurtType)
	{
		if (BIsDead) return;
		if (_stateMachine.CurrentState != FootballState.Dead)
		{
			_pendingDeathHurtType = hurtType;
			_stateMachine.ForceSetState(FootballState.Dead);
			return;
		}
		base.StartDead(hurtType);
	}
}
