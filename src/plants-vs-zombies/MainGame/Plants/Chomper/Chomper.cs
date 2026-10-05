using Godot;
using System;
using System.Collections.Generic;
using static ResourceDB.Sounds;
using Collision = PlantsVsZombies.Collision;

public partial class Chomper : Plants
{
	[Export] public AnimationPlayer AnimationPlayer;

	private Collision.IHitBox _detectionHitBox;
	public AudioStreamPlayer BitSound = new();
	private Zombie _targetZombie;

	/// <summary>检测规则。建一次存着，别在每帧的检查里新建</summary>
	private Collision.HitPredicate<Zombie> _canChomp;

	/// <summary>种下之后才开始检测；卡槽里的展示实例不参与</summary>
	private bool _armed;

	public bool CanEat
	{
		get;
		private set
		{ if (field = value) CheckZombieInDetectionHitBox(); }
	} = true;

	private Timer _chompTimer;
	private const float CHOMP_INTERVAL = 1.07f; // 啃咬间隔
	private const int CHOMP_DAMAGE = 40; // 每次啃咬伤害

	private enum ChomperState
	{
		Idle,
		Biting,
		Chewing,
		Swallowing,
		Digesting,
		Chomping  // 持续啃咬无法吞食的僵尸
	}

	private StateMachine<ChomperState> _stateMachine;
	//private bool _canEat = true;

	public Chomper()
	{
		SunCost = 150;
		CDtime = CDTime.FAST;
	}

	public override void _Ready()
	{
		base._Ready();

		// BitSound 是字段初始化器建出来的，必须在展示态早退**之前**挂上树：
		// 不挂它就是孤儿节点。（下面的 _chompTimer 是现 new 的，早退时压根不会建）
		AddChild(BitSound);

		if (BIsDisplayOnly)
		{
			return;
		}

		AddChild(_stateMachine = new StateMachine<ChomperState>(ChomperState.Idle));
		_stateMachine.StateChanged += OnStateChanged;

		// 只管检测箱；防御箱由基类 Plants 自己拿自己管
		_detectionHitBox = GetNode<Collision.IHitBox>("%DetectionHitBox");

		// 检测规则建一次存着。捕获 this 的写法编译期就缓存，每帧用不会分配
		_canChomp = (source, zombie, box) =>
			zombie.Row == Row
			&& !zombie.BIsDead
			&& Collision.HitMath.OverlapX(source, box) >= 0;
		
		BitSound.Stream = Bigchomp;

		// 初始化啃咬计时器
		_chompTimer = new Timer();
		_chompTimer.WaitTime = CHOMP_INTERVAL;
		_chompTimer.OneShot = true;
		_chompTimer.Timeout += OnChompTimeout;
		AddChild(_chompTimer);
	}

	public override void _Plant(int col, int row, int index)
	{
		base._Plant(col, row, index);
		_armed = true;
	}

	/// <summary>
	/// 检测改成每帧看一眼，不再靠引擎的"进入"事件。
	///
	/// 引擎那个事件是状态转移，而判定要的是每帧重判：僵尸从检测框里穿过去、
	/// 目标中途死了，事件都不会再响。检查本身很便宜——不在 Idle 状态就直接返回。
	/// </summary>
	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
		CheckZombieInDetectionHitBox();
	}

	public override void _Idle()
	{
		AnimationPlayer.Play("Chomper/idle", 20);
	}

	private void CheckZombieInDetectionHitBox()
	{
		Log.Trace("Chomper: Checking detection hit box");
		if (!_armed)
		{
			return;
		}
		if (_stateMachine.CurrentState != ChomperState.Idle)
		{
			Log.Trace("_stateMachine.CurrentState != ChomperState.Idle, _stateMachine.CurrentState = ", _stateMachine.CurrentState);
			return;
		}

		if (!CanEat)
		{
			Log.Trace("CanEat == false");
			return;
		}

		Log.Trace("Chomper: Zombie detected in detection hit box");

		Zombie target = GetTargetFromDetectionHitBox();
		if (target == null) return;
		_targetZombie = target;

		Log.Trace("Chomper enters idle state");
		if (CanSwallowZombie(target))
		{
			_stateMachine.ForceSetState(ChomperState.Biting);
		}
		else
		{
			_stateMachine.ForceSetState(ChomperState.Chomping);
		}
	}

	//public override void Hurt(Hurt hurt)
	//{
	//    // 在咀嚼/消化期间，大嘴花可以被僵尸啃食
	//    if (hurt.HurtType == HurtType.Eating && !_canEat)
	//        base.Hurt(hurt);
	//    else if (hurt.HurtType != HurtType.Eating)
	//        base.Hurt(hurt);
	//}

	private void OnStateChanged(ChomperState newState)
	{
		Log.Debug($"Chomper state changed: {newState}");
		TextEdit.Text = $"State: {newState}";
		switch (newState)
		{
			case ChomperState.Idle:
				// 先清空、再放开吃。CanEat 的 setter 会当场去检测框挑目标、挑到就切进
				// Biting；清空写在它后面的话，刚挑到的目标会被这一句抹掉——表现是咬了
				// 一口、有音效、动画照播，但僵尸没事
				_targetZombie = null;
				CanEat = true;

				if (_stateMachine.CurrentState == ChomperState.Idle)
				{
					AnimationPlayer.Play("Chomper/idle");
				}
				break;

			case ChomperState.Biting:
				Log.Debug("Chomper starts biting");
				CanEat = false;
				AnimationPlayer.Play("Chomper/bite", 0.2, 2f);
				_stateMachine.SetNextState(ChomperState.Chewing, 1f);
				break;

			case ChomperState.Chewing:
				AnimationPlayer.Play("Chomper/chew", 0, 1.25f);
				_stateMachine.SetNextState(ChomperState.Swallowing, 10f);
				break;

			case ChomperState.Swallowing:
				Log.Debug("Chomper starts swallowing");
				AnimationPlayer.Play("Chomper/swallow", 0.2, 1f);
				_stateMachine.SetNextState(ChomperState.Idle, 2.25);
				break;

			case ChomperState.Chomping:
				CanEat = false;
				if (_targetZombie is { BIsDead: false })
				{
					// 立即造成一次伤害
					_targetZombie.Hurt(new Hurt(CHOMP_DAMAGE, HurtType.Eating));
					// 启动啃咬计时器
					_chompTimer.Start();
				}
				else
				{
					// 没有目标，返回空闲
					_stateMachine.ForceSetState(ChomperState.Idle);
				}
				break;
		}
	}

	private void DamageZombie()
	{
		BitSound.Play();
		if (_targetZombie is { BIsDead: false })
		{
			_targetZombie.Hurt(new Hurt(1800, HurtType.Squash));
		}
	}

	public override void FreePlant()
	{
		base.FreePlant();
		_armed = false;
	}

	private bool CanSwallowZombie(Zombie zombie)
	{
		// 暂时假设所有僵尸都可吞食（巨人僵尸和僵王博士未实现）
		return true;
	}

	/// <summary>
	/// 在检测范围内挑一只僵尸。**按栈位从小到大的顺序**——那是原版的优先级来源
	/// （所有判定都是从数组下标 0 开始、第一个满足条件的即采用），所以这里不再比
	/// Index 的大小，遍历顺序本身就是答案。
	/// </summary>
	private Zombie GetTargetFromDetectionHitBox()
	{
		if (_detectionHitBox == null || MainGame.Instance == null)
		{
			return null;
		}

		Zombie[] zombies = MainGame.Instance.Zombies;
		int index = Collision.CollisionSystem.PickFirst(zombies, _detectionHitBox, _canChomp);
		return index < 0 ? null : zombies[index];
	}

	private void OnChompTimeout()
	{
		if (_stateMachine.CurrentState != ChomperState.Chomping) return;
		if (_targetZombie == null || _targetZombie.BIsDead)
		{
			_stateMachine.ForceSetState(ChomperState.Idle);
			return;
		}

		// 造成啃咬伤害
		_targetZombie.Hurt(new Hurt(CHOMP_DAMAGE, HurtType.Eating));

		// 每次撕咬后回到Idle状态，重新检测僵尸
		//_chompTimer.Stop();
		_stateMachine.ForceSetState(ChomperState.Idle);
	}
}
