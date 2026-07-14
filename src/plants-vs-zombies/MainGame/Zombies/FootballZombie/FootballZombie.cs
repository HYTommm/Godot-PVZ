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

		_stateMachine.ForceSetState(FootballState.Walk);
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
