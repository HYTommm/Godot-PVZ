using Godot;
using static Godot.GD;
using System;
using static System.Formats.Asn1.AsnWriter;
using System.Linq;
using System.Collections.Generic;
using static ResourceManager.Sounds;

public partial class MainGame : MainNode2D
{
	ZombieWeightsAndGrades ZombieWeightsAndGrades = new ZombieWeightsAndGrades();
	ZombieType ZombieType = new ZombieType();
	// 当前波数
	public int ZombieCurrentWave;
	// 最大波数
	public int ZombieMaxWave;
	// 当前波最大生命值
	public int ZombieCurrentWaveMaxHP;
	
	public Camera camera;
	public AnimationPlayer animation;
	public SeedBank seedBank;
	public Plants seed, seedClone;
	public SeedPacketLarger seedPacketNode;

	// 是否可以重叠种植
	public bool canOverlapPlant = false;
	

	// 阳光数量
	public int SunCount = 0;
	// 阳光刷新倒计时
	public Timer SunTimer = new Timer();
	// 已刷新阳光数量
	public int SunRefreshedCount = 0;
	
	
	
	// 植物栈
	public int plantStack = 0;
	// 植物数组
	public Plants[] plants = new Plants[1000];
	// 僵尸栈
	public int zombieStack = 0;
	// 僵尸数组
	public Zombie[] zombies = new Zombie[1000];
	public int[] zombiesNumOfRow = new int[10];
	// 僵尸总数
	public int ZombieNum = 0;
	//public Zombie[, ] zombiesOfRow = new Zombie[10, 1000];
	// 僵尸刷新倒计时
	public Timer ZombieTimer = new Timer();

	// 小推车列表
	public List<LawnMower> lawnMowers = new List<LawnMower>();

	// 场景
	public Scene GameScene;

	/// <summary> 是否正在选中种子卡 </summary>
	public bool isSeedCardSelected = false;
	public bool isGameOver = false;
	public bool isRefreshingZombies = false;

	public int totalHealth = 0;

	public RandomNumberGenerator RNG = new RandomNumberGenerator();

	Vector2I MouseUnitPos = new Vector2I();

	public AudioStreamPlayer PutBackPlantSound = new AudioStreamPlayer();

	public bool isClimaxing = false; // 游戏是否处于高潮状态


	public void InitLawnMowers(Scene scene)
	{
		GD.Print("InitLawnMower");
		for (int i = 0; i < scene.LawnUnitCount.Y; i++)
		{
			LawnMower lawnMower = Load<PackedScene>("res://MainGame/LawnMower/LawnMower.tscn").Instantiate<LawnMower>();
			lawnMower.Position = new Vector2(-100, i * scene.LawnUnitSize.Y + scene.LawnMoverPos.Y);
			//lawnMower.Scale = new Vector2(0.85f, 0.85f);
			lawnMower.Row = i;
			lawnMowers.Add(lawnMower);
			AddChild(lawnMower);
		}
	}

	public async void MoveLawnMowers()
	{
		for (int i = lawnMowers.Count - 1; i >= 0; i--)
		{
			lawnMowers[i].MoveTo(new Vector2(GameScene.LawnMoverPos.X, lawnMowers[i].Position.Y));
			// 等待0.1秒
			await ToSignal(GetTree().CreateTimer(0.08), SceneTreeTimer.SignalName.Timeout);
		}
	}

	public void SetLawnMowersPosX(int posX)
	{
		for (int i = 0; i < lawnMowers.Count; i++)
		{
			lawnMowers[i].Position = new Vector2(posX, lawnMowers[i].Position.Y);
		}
	}

	public override void _Ready()
	{
		RNG.Randomize();// 随机种子
		ZombieWeightsAndGrades.SetZombieAllowed(new List<ZombieTypeEnum>() { ZombieTypeEnum.Normal, ZombieTypeEnum.Conehead, ZombieTypeEnum.Buckethead, ZombieTypeEnum.Screendoor });
		GetNode<Node>("/root").PrintTreePretty();
		GameScene = new LawnDayScene(GetNode<Node>("/root/Global"));// 设置场景
		//GameScene = new PoolDayScene();
		GetNode<Sprite2D>("./BackGround").Texture = GameScene.BackGroundTexture;// 设置背景
		InitLawnMowers(GameScene);// 初始化草坪机
		//GD.Print("GameScene.Weight.Length: " + GameScene.Weight.Length);
		camera = GetNode<Camera>("./Camera"); // 设置相机
		animation = GetNode<AnimationPlayer>("./CanvasLayer/AnimationPlayer");// 设置动画播放器
		seedBank = GetNode<SeedBank>("./CanvasLayer/SeedBank");// 设置种子卡槽

		PutBackPlantSound.Stream = Sound_Tap2;
		AddChild(PutBackPlantSound);

		SunTimer.Timeout += RefreshSun;
		SunTimer.OneShot = true;
		ZombieTimer.Timeout += RefreshZombie;
		ZombieTimer.OneShot = true;
		AddChild(SunTimer);
		AddChild(ZombieTimer);
		
		SelectSeedCard();// 进入选卡环节

		
	}


	// Called every frame. 'delta' is the elapsed time since the previous frame.


	public override void _Process(double delta)
	{
		// 如果SeedCard被选中
		if (isSeedCardSelected)
		{
			seed.Position = seedPacketNode.GetGlobalMousePosition() - new Vector2(35, 60);
			
			Vector2 MouseGlobalPos = GetGlobalMousePosition();
			if (   MouseGlobalPos.X >= GameScene.LawnLeftTopPos.X && MouseGlobalPos.X < GameScene.LawnLeftTopPos.X + GameScene.LawnUnitLength * GameScene.LawnUnitCount.X
				&& MouseGlobalPos.Y >= GameScene.LawnLeftTopPos.Y && MouseGlobalPos.Y < GameScene.LawnLeftTopPos.Y + GameScene.LawnUnitWidth * GameScene.LawnUnitCount.Y
				)
			{
				/*
				MouseUnitPos = 
					new Vector2I((int)((MouseGlobalPos.X - GameScene.LawnLeftTopPos.X) / GameScene.LawnUnitLength),
								(int)((MouseGlobalPos.Y - GameScene.LawnLeftTopPos.Y) / GameScene.LawnUnitWidth));
				*/
				MouseUnitPos.X = (int)((MouseGlobalPos.X - GameScene.LawnLeftTopPos.X) / GameScene.LawnUnitLength);
				MouseUnitPos.Y = (int)((MouseGlobalPos.Y - GameScene.LawnLeftTopPos.Y) / GameScene.LawnUnitWidth);
				//GD.Print(MouseUnitPos);

				if (canOverlapPlant || GameScene.IsLawnUnitPlantEmpty(MouseUnitPos.X, MouseUnitPos.Y))
				{
					seedClone.Position = new Vector2(MouseUnitPos.X * GameScene.LawnUnitLength + GameScene.LawnLeftTopPos.X,
													 MouseUnitPos.Y * GameScene.LawnUnitWidth  + GameScene.LawnLeftTopPos.Y);
					seedClone.Visible = true; 
				}
				else
				{
					seedClone.Visible = false;
				}
			}
			else
			{
				seedClone.Visible = false;
				MouseUnitPos.X = -1;
				MouseUnitPos.Y = -1;
			
			}
		}

	}


	public override void _Input(InputEvent @event)
	{
		base._Input(@event);
		if (@event.IsAction("mouse_left"))
		{
			if (!mouse_left_down)
			{
				return;
			}
			// 如果鼠标左键按下，且正在选中种子卡，则种植植物
			if (!isSeedCardSelected)
			{
				return;
			}
			if (seedClone.Visible == false)
			{
				if (MouseUnitPos.X == -1 && MouseUnitPos.Y == -1)
				{
					GetViewport().SetInputAsHandled();
					PutBackPlant();
					seedPacketNode.SetCDZero();
					PutBackPlantSound.Play();
				}
				return;
			}
			PlantSeed();
		}

	}
	// 选择种子卡
	async public void SelectSeedCard()
	{
		GameScene.PlaySelectSeedCardBgm();
		await ToSignal(GetTree().CreateTimer(1.3), SceneTreeTimer.SignalName.Timeout);
		animation.Play("Label");
		camera.Move(GameScene.CameraRightPos, 1.25);
		await ToSignal(camera, Camera.SignalName.MoveEnd);

		await ToSignal(GetTree().CreateTimer(1.2), SceneTreeTimer.SignalName.Timeout);
		Game();
	}
	
	// 开始游戏
	async public void Game()
	{
		
		SetLawnMowersPosX(155); // 移动草坪机
		// 移动相机到中心位置
		camera.Move(GameScene.CameraCenterPos, 1);
		await ToSignal(camera, Camera.SignalName.MoveEnd);
		MoveLawnMowers(); // 移动草坪机
		// 显示种子卡槽
		animation.Play("SeedBank");
		await ToSignal(animation, "animation_finished");
		GameScene.TurnOffAllBGM_FadeOut(animation.CurrentAnimationLength);
		// 将种子卡槽移动到节点树的外层
		Vector2 SeedBankGlobalPos = seedBank.GlobalPosition;
		seedBank.GetParent().RemoveChild(seedBank);
		seedBank.GlobalPosition = SeedBankGlobalPos + camera.GlobalPosition;
		AddChild(seedBank);
		// 将Button节点移动到节点树的外层
		GameButton button = GetNode<GameButton>("./CanvasLayer/GameButton");
		Vector2 ButtonGlobalPos = button.GlobalPosition;
		button.GetParent().RemoveChild(button);
		button.GlobalPosition = ButtonGlobalPos + camera.GlobalPosition;
		AddChild(button);
		button.pos = button.Position;
		
		// 初始化
		ZombieCurrentWave = 1; // 初始化当前波数
		ZombieMaxWave = 20; // 初始化最大波数

		SunCount = 50000; // 初始化阳光数量

		seedBank.UpdateSunCount(); // 更新阳光数量
		//await ToSignal(GetTree().CreateTimer(2f), "timeout");

		GameScene.PlayMainGameBgm(); // 播放BGM
		GameScene.TurnToNormalBgm();
		
		RefreshSunTimer(); // 刷新阳光计时器
		RefreshZombieTimer(19); // 刷新僵尸计时器
	}

	// 选中种子
	public void AddSeed(SeedPacketLarger node, Plants seed, Plants seedclone)
	{
		// 赋值
		this.seedPacketNode = node;
		this.seed = seed;
		this.seedClone = seedclone;

		

		// 初始化克隆种子
		seedClone.Visible = false;
		//seedClone.SelfModulate = new Color(1, 1, 1, 0.6f);
		seedClone._SetAlpha(0.6f);
		// 添加克隆植物
		AddChild(seedClone);

		// 初始化种子
		seed.Position = seedPacketNode.GetViewport().GetMousePosition() - new Vector2(35, 60);
		// 添加植物
		//seedNode.GetCanvasLayerNode().AddChild(seed);
		AddChild(seed);

		isSeedCardSelected = true;
		//GD.Print(seedNode);
	}

	// 种植植物
	public void PlantSeed()
	{
		
		seed.Visible = false;
		
		isSeedCardSelected = false;

		
		int tempIndex = -1;
		GD.Print("plantStack: " + plantStack);
		if (plants[plantStack] != null)
		{
			GD.Print("plants[plantStack] != null");
			tempIndex = plants[plantStack].Index;
			GD.Print("tempIndex: " + tempIndex);
			plants[plantStack]?.QueueFree();
		}

		plants[plantStack] = seedClone;
		seedClone.Index = plantStack;

		if (tempIndex != -1)
		{
			plantStack = tempIndex;
		}
		else
		{
			plantStack++;
		}

		seed.QueueFree();
		seedClone._Plant(MouseUnitPos.X, MouseUnitPos.Y, seedClone.Index);
		GameScene.LawnUnitPlacePlant(MouseUnitPos.X, MouseUnitPos.Y);
		
		SunCount -= seedClone.SunCost;
		GetNode<SeedBank>("./SeedBank").UpdateSunCount();

		//seedNode.ResetCD();
		seedPacketNode.isCDCooling = true;
		//GD.Print("MainGame: PlantSeed");

	}

	// 释放（松开、放回）植物
	public void PutBackPlant()
	{
		seedClone.Visible = false;
		seed.Visible = true;
		seedClone.QueueFree();
		seed.QueueFree();
		isSeedCardSelected = false;
	}


	// 刷新僵尸
	public async void RefreshZombie()
	{
		if (isRefreshingZombies)
		{
			return;
		}
		if (ZombieCurrentWave >= ZombieMaxWave)
		{
			Print("Game Over");
			return;
		}
		isRefreshingZombies = true;
		// 每波容量上限 = int(int(当前波数 * 0.8) / 2) + 1，10的倍数为大波，大波容量上限乘2.5
		ZombieCurrentWaveMaxHP = 0;
		// Print("Wave: " + ZombieCurrentWave);
		Zombie zombie = null;
		int zombieMaxGrade = (int)((int)(ZombieCurrentWave * 0.8) / 2.0) + 1;
		// 判断是否是大波
		if (ZombieCurrentWave % 10 == 0)
		{
			zombieMaxGrade = (int)(zombieMaxGrade * 2.5);
		}

		zombieMaxGrade *= 20; // 10倍数
		// Print("zombieCount: " + zombieCount);

		// 预备僵尸
		Zombie[] PreZombie = new Zombie[100];// 预备僵尸数组
		int zombieCount = 0; // 预备僵尸数量
		for (int zombieCurrentGrade = 0; zombieCurrentGrade < zombieMaxGrade; zombieCount++)
		{
			var zombieType = ZombieWeightsAndGrades.GetRandomZombieType();
			int tempGrade = ZombieWeightsAndGrades.GetZombieGrade(zombieType);
			if (tempGrade + zombieCurrentGrade > zombieMaxGrade)
			{
				zombieCount--; // 减1，重新尝试
				continue;
			}
			GD.Print("zombieType: " + zombieType, "tempGrade: " + tempGrade, "zombieCurrentGrade: " + zombieCurrentGrade);
			zombieCurrentGrade += tempGrade; // 增加僵尸当前等级
			PreZombie[zombieCount] = ZombieType.GetZombieScene(zombieType).Instantiate() as Zombie;
			PreZombie[zombieCount].Init(zombieType);
			// 预备僵尸
			//PreZombie[i] = Load<PackedScene>("res://MainGame/Zombies/Zombie.tscn").Instantiate() as Zombie;

			// 预备僵尸初始化
			int tempIndex = -1;

			if (zombies[zombieStack] != null)
			{
				tempIndex = zombies[zombieStack].Index;
				zombies[zombieStack].QueueFree();
			}
			

			zombies[zombieStack] = PreZombie[zombieCount];// 预备僵尸加入栈数组
			
			PreZombie[zombieCount].Index = zombieStack; // 预备僵尸索引
			if (tempIndex != -1)
			{
				zombieStack = tempIndex; // 预备僵尸索引更新
			}
			else
			{
				zombieStack++; // 僵尸栈加1
			}
			ZombieCurrentWaveMaxHP += PreZombie[zombieCount].MaxHP; // 计算当前波最大生命值

			//if (zombieCurrentGrade + PreZombie[i].Grade > zombieMaxGrade)
			//{
			//	i--; // 减1，重新尝试
			//	break;
			//}
			

		}
		// 刷新僵尸
		for (int i = 0; i < zombieCount; i++)
		{
			zombie = PreZombie[i]; // 取出预备僵尸
			AddZombie(zombie);

			//AddChild(zombie);

			await ToSignal(GetTree().CreateTimer(0.5), SceneTreeTimer.SignalName.Timeout); //等待0.5秒再刷新下一个僵尸
		}
		// 如果当前波数大于最大波数，则连接僵尸死亡信号到游戏结束函数
		if (ZombieCurrentWave >= ZombieMaxWave)
		{
			// Print("Wave:" + ZombieCurrentWave + " MaxWave: " + ZombieMaxWave);
			zombie.ZombieDie += GameOver; // 连接僵尸死亡信号到游戏结束函数
			// 最后一波僵尸死亡后，游戏结束
		}
		ZombieCurrentWave++; // 波数加1
		ZombieWeightsAndGrades.UpdateZombieWave(); // 更新僵尸波数以更新僵尸权重
		isRefreshingZombies = false; // 刷新结束
		RefreshZombieTimer(); // 刷新计时器
	}

	// 刷新僵尸计时器
	public void RefreshZombieTimer()
	{
		// 在本波刷新 2500-3100cs 后生成下一波。
		ZombieTimer.Start((float)(RNG.RandiRange(2500, 3100) / 100.0));
	}
	public void RefreshZombieTimer(float time)
	{
		ZombieTimer.Start(time);
	}

	// 添加僵尸
	public void AddZombie(Zombie zombie)
	{
		GD.Print("zombie: " + zombie.Name + " Index: " + zombie.Index);

		int Row = GetRandomZombieRow(); // 随机僵尸所在行
		Print("Row: " + Row);

		zombie.Refresh(zombie.Index, GameScene, ZombieCurrentWave, Row); // 刷新僵尸
		zombiesNumOfRow[Row]++; // 该行僵尸数加1
								//zombiesOfRow[Row, ] = zombie; // 该行僵尸数组加1
		CallDeferred("add_child", zombie); // 添加到场景树

		ZombieNum += 1; // 总僵尸数加1
		UpdateZombieNum(); // 更新僵尸数
	}

	// 移除僵尸
	public void RemoveZombie(Zombie zombie)
	{
		int tempIndex = zombie.Index;
		zombie.Index = zombieStack;
		//zombies[zombie.Index] = null;
		zombieStack = tempIndex;
		ZombieNum--;
		UpdateZombieNum();
	}

	public void UpdateZombieNum()
	{
		GD.Print("ZombieNum: " + ZombieNum, "isClimaxing: " + isClimaxing);
		if (isClimaxing)
		{
			if (ZombieNum <= 3)
			{
				isClimaxing = false;
				GameScene.TurnToNormalBgm();
			}
		}
		else
		{
			if (ZombieNum >= 10)
			{
				isClimaxing = true;
				GD.Print("Turn to HighBGM");
				GameScene.TurnToHighBgm();
			}
		}
	}

	// 判断全场僵尸血量在总血量的百分比
	public float GetZombieTotalHealthPercent()
	{
		totalHealth = 0;
		for (int i = 0; i < 1000; i++)
		{
			if (zombies[i] != null && zombies[i].Wave == ZombieCurrentWave - 1)
			{
				totalHealth += zombies[i].HP > 0 ? zombies[i].HP : 0;
			}
		}
		GD.Print("totalHealth: " + totalHealth + " WaveMaxHP: " + ZombieCurrentWaveMaxHP);
		if (totalHealth == 0 || ZombieCurrentWaveMaxHP == 0)
		{
			return 0;
		}
		return (float)totalHealth / ZombieCurrentWaveMaxHP * 100;
	}

	// 随机僵尸所在行
	public int GetRandomZombieRow()
	{
		// 这段代码过于复杂，请不要深挖，直接使用此函数即可
		// 此函数已被证明是正确的，请不要修改
		// 至未来的某位高手，请不要修改此函数，除非你知道你在做什么，且你有充足的理由和十足的把握

		// 如果你想了解底层算法逻辑，请参考链接：
		// "https://wiki.pvz1.com/doku.php?id=技术:出怪机制"
		
		// https://wiki.pvz1.com/doku.php?id=%E6%8A%80%E6%9C%AF:%E5%87%BA%E6%80%AA%E6%9C%BA%E5%88%B6 //同上
		// 这段代码的逻辑是：

		// 遍历每一行，计算每一行的权重
		/*
		 * 出怪行信息：
		 * 
		 * 对于第i行，有权重、上次选择、上上次选择三个信息。
		 *
		 * 权重用于计算出怪内容，之前出怪用于计算出怪概率。
		 *
		 * 作如下定义：
		 * 
		 *     将第i行权重定义为Weighti
		 *     将第i行距离上次被选取定义为LastPickedi
		 *     将第i行距离上上次被选取定义为SecondLastPickedi
		 * LastPickedi和SecondLastPickedi的初始值为0。
		 * 
		 */

		/*
		 * 某一行出怪时——又称第j行插入事件：
		 * 
		 * 执行第j行插入事件时，会发生如下变化：
		 * ∀i∈[1,6]，如果Weighti>0，则LastPickedi和SecondLastPickedi均增加1
		 * 将LastPickedj的值赋给SecondLastPickedj
		 * 将LastPickedj设为0
		 * 
		 */
		for (int i = 0; i < GameScene.LawnUnitCount.Y; i++) 
		{
			GameScene.PLast[i] =
				(GameScene.LawnUnitCount.Y * GameScene.LastPicked[i] * GameScene.WeightP[i] +
				 GameScene.LawnUnitCount.Y * GameScene.WeightP[i] - 3) / 4;
			GameScene.PSecondLast[i] = 
				(GameScene.SecondLastPicked[i] * GameScene.WeightP[i] + GameScene.WeightP[i] - 1) / 4;

			if (GameScene.WeightP[i] >= Math.Pow(10, -6))
			{
				GameScene.SmoothWeight[i] =
					GameScene.WeightP[i] * Math.Min(Math.Max(GameScene.PLast[i] + GameScene.PSecondLast[i], 0.01f), 100);
			}
			else
			{
				GameScene.SmoothWeight[i] = 0;
			}
		}

		float SmoothWeightAll = GameScene.SmoothWeight.Sum();
		
		float RandomWeight = RNG.RandfRange(0, SmoothWeightAll);
		int Row = 0;
		for (Row = 0; Row < GameScene.LawnUnitCount.Y - 1; Row++)
		{
			RandomWeight -= GameScene.SmoothWeight[Row];
			if (RandomWeight <= 0)
			{
				break;
			}
		}
		for (int i = 0; i < GameScene.LawnUnitCount.Y; i++)
		{
			if (GameScene.Weight[i] >= 0)
			{
				GameScene.LastPicked[i]++;
				GameScene.SecondLastPicked[i]++;
			}
		}
		GameScene.SecondLastPicked[Row] = GameScene.LastPicked[Row];
		GameScene.LastPicked[Row] = 0;
		return Row;
	}

	// 更新僵尸血量
	public void UpdateZombieHP()
	{
		//GD.Print("isRefreshingZombies: " + isRefreshingZombies);
		//for (int i = 0; i < 1000; i++)
		if (isRefreshingZombies)
		{
			return;
		}
		
		//GD.Print("TotalHealthPercent: " + GetZombieTotalHealthPercent() + "totleHealth: " + totalHealth + " WaveMaxHP: " + WaveMaxHP);
		if (ZombieCurrentWave <= ZombieMaxWave && GetZombieTotalHealthPercent() <= 60)
		{
			//isRefreshingZombies = true;
			GD.Print("RefreshingZombies..." );
			// await ToSignal(GetTree().CreateTimer(2), SceneTreeTimer.SignalName.Timeout);
			if (ZombieTimer.TimeLeft > 2)
			{
				ZombieTimer.Stop();
				ZombieTimer.Start(2);
			}
		}
	}

	// 移除植物
	public void RemovePlant(Plants plant)
	{
		int tempIndex = plant.Index;
		plant.Index = plantStack;
		plantStack = tempIndex;
		GameScene.LawnUnitClearPlant (plant.Col, plant.Row);
		//plants[plant.Index] = null;
		//plantStack = plant.Index;
	}

	

	// 刷新阳光
	public void RefreshSun()
	{
		GD.Print("RefreshSun");
		Sun sun = GD.Load<PackedScene>("res://MainGame/Drops/Sun.tscn").Instantiate() as Sun; // 实例化阳光
		sun.Position = new Vector2(RNG.RandfRange(100, 700) + GameScene.CameraCenterPos.X, 90); // 设置阳光的位置
		sun.GroundPosY = RNG.RandiRange(200, 500); // 设置阳光的地面高度
		AddChild(sun); // 添加阳光到场景中
		SunRefreshedCount += 25; // 阳光掉落数加25

		
		RefreshSunTimer();
	
	}

	// 刷新阳光计时器
	public void RefreshSunTimer()
	{
		// 天降阳光的时间间隔T和本局游戏内已掉落的阳光数量有关。
		// 设已掉落的阳光数量为x，A = 10x + 425，B为0~274之间的随机数
		// 若A <= 950,则T = A + B，若A > 950，则T = 950 + B

		int SunRefreshTimeA = 10 * SunRefreshedCount + 425;
		int SunRefreshTimeB = RNG.RandiRange(0, 274);
		if (SunRefreshTimeA > 950)
		{
			SunRefreshTimeA = 950;
		}
		int SunRefreshTime = SunRefreshTimeA + SunRefreshTimeB;

		SunTimer.Start(SunRefreshTime / 100f);
		GD.Print("SunRefreshTime: " + SunRefreshTime);
	}


	// 游戏结束
	public void GameOver()
	{
		isGameOver = true;
		GD.Print("Game Over");
	}
}
