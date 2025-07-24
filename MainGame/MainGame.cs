using Godot;
using static Godot.GD;
using System;
using static System.Formats.Asn1.AsnWriter;
using System.Linq;
using System.Collections.Generic;
using static ResourceManager.Sounds;

public partial class MainGame : MainNode2D
{
	private readonly ZombieWeightsAndGrades _zombieWeightsAndGrades = new();
	private readonly ZombieType _zombieType = new();
	// 当前波数
	public int ZombieCurrentWave;
	// 最大波数
	public int ZombieMaxWave;
	// 当前波最大生命值
	public int ZombieCurrentWaveMaxHP;
	
	[Export] public Camera Camera;
	[Export] public AnimationPlayer Animation;
	[Export] public SeedBank SeedBank;
	[Export] public Node2D PlantsContainer;
	[Export] public Node2D ZombiesContainer;
	[Export] public Node2D SunContainer;
	private Plants _seed, _seedClone;
	private SeedPacketLarger _seedPacketNode;

	// 是否可以重叠种植
	public bool BCanOverlapPlant = false;
	

	// 阳光数量
	public int SunCount = 0;
	// 阳光刷新倒计时
	public Timer SunTimer = new();
	// 已刷新阳光数量
	public int SunRefreshedCount = 0;
	
	
	
	// 植物栈
	public int PlantStack = 0;
	// 植物数组
	public Plants[] Plants = new Plants[1000];
	// 僵尸栈
	public int ZombieStack = 0;
	// 僵尸数组
	public Zombie[] Zombies = new Zombie[1000];
	public int[] ZombiesNumOfRow = new int[10];
	// 僵尸总数
	public int ZombieNum = 0;
	//public Zombie[, ] zombiesOfRow = new Zombie[10, 1000];
	// 僵尸刷新倒计时
	public Timer ZombieTimer = new();

	// 小推车列表
	public List<LawnMower> LawnMowersList = new();

	// 场景
	public Scene GameScene;

	/// <summary> 是否正在选中种子卡 </summary>
	public bool BIsSeedCardSelected = false;
	public bool BIsGameOver = false;
	public bool BIsRefreshingZombies = false;

	private int _totalHealth = 0;

	public RandomNumberGenerator RNG = new();

	Vector2I _mouseUnitPos = new();

	public AudioStreamPlayer PutBackPlantSound = new();

	public bool BIsClimaxing = false; // 游戏是否处于高潮状态


	public void InitLawnMowers(Scene scene)
	{
		GD.Print("InitLawnMower");
		for (int i = 0; i < scene.LawnUnitCount.Y; i++)
		{
			LawnMower lawnMower = Load<PackedScene>("res://MainGame/LawnMower/LawnMower.tscn").Instantiate<LawnMower>();
			lawnMower.Position = new Vector2(-100, i * scene.LawnUnitSize.Y + scene.LawnMoverPos.Y);
			//lawnMower.Scale = new Vector2(0.85f, 0.85f);
			lawnMower.Row = i;
			LawnMowersList.Add(lawnMower);
			AddChild(lawnMower);
		}
	}

	public async void MoveLawnMowers()
	{
		for (int i = LawnMowersList.Count - 1; i >= 0; i--)
		{
			LawnMowersList[i].MoveTo(new Vector2(GameScene.LawnMoverPos.X, LawnMowersList[i].Position.Y));
			// 等待0.1秒
			await ToSignal(GetTree().CreateTimer(0.08), SceneTreeTimer.SignalName.Timeout);
		}
	}

	public void SetLawnMowersPosX(int posX)
	{
		foreach (LawnMower lawnMower in LawnMowersList)
		{
			lawnMower.Position = new Vector2(posX, lawnMower.Position.Y);
		}
	}

	public override void _Ready()
	{
		RNG.Randomize();// 随机种子
		_zombieWeightsAndGrades.SetZombieAllowed([
			ZombieTypeEnum.Normal,
			ZombieTypeEnum.Conehead,
			ZombieTypeEnum.Buckethead,
			ZombieTypeEnum.Screendoor
		]);
		GetNode<Node>("/root").PrintTreePretty();
		GameScene = new LawnDayScene(this.GetGlobalNode());// 设置场景
		//GameScene = new PoolDayScene();
		GetNode<Sprite2D>("./BackGround").Texture = GameScene.BackGroundTexture;// 设置背景
		InitLawnMowers(GameScene);// 初始化草坪机

		PutBackPlantSound.Stream = Sound_Tap2;
		AddChild(PutBackPlantSound);

		SunTimer.Timeout += RefreshSun;          SunTimer.OneShot = true;       AddChild(SunTimer);
		ZombieTimer.Timeout += RefreshZombie;    ZombieTimer.OneShot = true;    AddChild(ZombieTimer);
		
		SelectSeedCard();// 进入选卡环节

		
	}


	// Called every frame. 'delta' is the elapsed time since the previous frame.


	public override void _Process(double delta)
	{
		// 如果SeedCard被选中
		if (BIsSeedCardSelected)
		{
			_seed.Position = _seedPacketNode.GetGlobalMousePosition() - new Vector2(35, 60);
			
			Vector2 mouseGlobalPos = GetGlobalMousePosition();
			if (   mouseGlobalPos.X >= GameScene.LawnLeftTopPos.X && mouseGlobalPos.X < GameScene.LawnLeftTopPos.X + GameScene.LawnUnitLength * GameScene.LawnUnitCount.X
				&& mouseGlobalPos.Y >= GameScene.LawnLeftTopPos.Y && mouseGlobalPos.Y < GameScene.LawnLeftTopPos.Y + GameScene.LawnUnitWidth * GameScene.LawnUnitCount.Y
				)
			{
				/*
				MouseUnitPos = 
					new Vector2I((int)((MouseGlobalPos.X - GameScene.LawnLeftTopPos.X) / GameScene.LawnUnitLength),
								(int)((MouseGlobalPos.Y - GameScene.LawnLeftTopPos.Y) / GameScene.LawnUnitWidth));
				*/
				_mouseUnitPos.X = (int)((mouseGlobalPos.X - GameScene.LawnLeftTopPos.X) / GameScene.LawnUnitLength);
				_mouseUnitPos.Y = (int)((mouseGlobalPos.Y - GameScene.LawnLeftTopPos.Y) / GameScene.LawnUnitWidth);
				//GD.Print(MouseUnitPos);

				if (BCanOverlapPlant || GameScene.IsLawnUnitPlantEmpty(_mouseUnitPos.X, _mouseUnitPos.Y))
				{
					_seedClone.Position = new Vector2(_mouseUnitPos.X * GameScene.LawnUnitLength + GameScene.LawnLeftTopPos.X,
													 _mouseUnitPos.Y * GameScene.LawnUnitWidth  + GameScene.LawnLeftTopPos.Y);
					_seedClone.Visible = true; 
				}
				else
				{
					_seedClone.Visible = false;
				}
			}
			else
			{
				_seedClone.Visible = false;
				_mouseUnitPos.X = -1;
				_mouseUnitPos.Y = -1;
			
			}
		}

	}


	public override void _Input(InputEvent @event)
	{
		base._Input(@event);
		if (@event.IsAction("mouse_left"))
		{
			if (!BMouse_left_down)
			{
				return;
			}
			// 如果鼠标左键按下，且正在选中种子卡，则种植植物
			if (!BIsSeedCardSelected)
			{
				return;
			}
			if (_seedClone.Visible == false)
			{
				if (_mouseUnitPos is { X: -1, Y: -1 })
				{
					GetViewport().SetInputAsHandled();
					PutBackPlant();
					_seedPacketNode.SetCDZero();
					PutBackPlantSound.Play();
				}
				return;
			}
			PlantSeed();
		}

	}
	// 选择种子卡
	public async void SelectSeedCard()
	{
		GameScene.PlaySelectSeedCardBgm();
		await ToSignal(GetTree().CreateTimer(1.3), SceneTreeTimer.SignalName.Timeout);
		Animation.Play("Label");
		Camera.Move(GameScene.CameraRightPos, 1.25);
		await ToSignal(Camera, Camera.SignalName.MoveEnd);

		await ToSignal(GetTree().CreateTimer(1.2), SceneTreeTimer.SignalName.Timeout);
		Game();
	}
	
	// 开始游戏
	public async void Game()
	{
		
		SetLawnMowersPosX(155); // 移动草坪机
		// 移动相机到中心位置
		Camera.Move(GameScene.CameraCenterPos, 1);
		await ToSignal(Camera, Camera.SignalName.MoveEnd);
		MoveLawnMowers(); // 移动草坪机
		// 显示种子卡槽
		Animation.Play("SeedBank");
		GameScene.TurnOffAllBGM_FadeOut(Animation.CurrentAnimationLength);
		await ToSignal(Animation, AnimationMixer.SignalName.AnimationFinished);
		

		// 将种子卡槽移动到节点树的外层
		Vector2 seedBankGlobalPos = SeedBank.GlobalPosition;
		SeedBank.GetParent().RemoveChild(SeedBank);
		SeedBank.GlobalPosition = seedBankGlobalPos + Camera.GlobalPosition;
		AddChild(SeedBank);

		// 将Button节点移动到节点树的外层
		GameButton button = GetNode<GameButton>("./CanvasLayer/GameButton");
		Vector2 buttonGlobalPos = button.GlobalPosition;
		button.GetParent().RemoveChild(button);
		button.GlobalPosition = buttonGlobalPos + Camera.GlobalPosition;
		AddChild(button);
		button.Pos = button.Position;
		
		// 初始化
		ZombieCurrentWave = 1; // 初始化当前波数
		ZombieMaxWave = 20; // 初始化最大波数

		SunCount = 50000; // 初始化阳光数量

		SeedBank.UpdateSunCount(); // 更新阳光数量
		//await ToSignal(GetTree().CreateTimer(2f), "timeout");

		GameScene.PlayMainGameBgm(); // 播放BGM
		GameScene.TurnToNormalBgm();
		
		RefreshSunTimer(); // 刷新阳光计时器
		RefreshZombieTimer(19); // 刷新僵尸计时器
	}

	// 选中种子
	public void AddSeed(SeedPacketLarger node, Plants seed, Plants seedClone)
	{
		// 赋值
		_seedPacketNode = node;
		_seed = seed;
		_seedClone = seedClone;


		// 初始化克隆种子
		_seedClone.Visible = false;
		//seedClone.SelfModulate = new Color(1, 1, 1, 0.6f);
		_seedClone._SetAlpha(0.6f);
		// 添加克隆植物
		PlantsContainer.AddChild(_seedClone);

		// 初始化种子
		seed.Position = _seedPacketNode.GetViewport().GetMousePosition() - new Vector2(35, 60);
		// 添加植物
		//seedNode.GetCanvasLayerNode().AddChild(seed);
		PlantsContainer.AddChild(seed);

		BIsSeedCardSelected = true;
		//GD.Print(seedNode);
	}

	// 种植植物
	public void PlantSeed()
	{
		
		_seed.Visible = false;
		
		BIsSeedCardSelected = false;

		
		int tempIndex = -1;
		GD.Print("plantStack: " + PlantStack);
		if (Plants[PlantStack] != null)
		{
			GD.Print("plants[plantStack] != null");
			tempIndex = Plants[PlantStack].Index;
			GD.Print("tempIndex: " + tempIndex);
			Plants[PlantStack]?.QueueFree();
		}

		Plants[PlantStack] = _seedClone;
		_seedClone.Index = PlantStack;

		if (tempIndex != -1)
		{
			PlantStack = tempIndex;
		}
		else
		{
			PlantStack++;
		}

		_seed.QueueFree();
		_seedClone._Plant(_mouseUnitPos.X, _mouseUnitPos.Y, _seedClone.Index);
		GameScene.LawnUnitPlacePlant(_mouseUnitPos.X, _mouseUnitPos.Y);
		
		SunCount -= _seedClone.SunCost;
		GetNode<SeedBank>("./SeedBank").UpdateSunCount();

		//seedNode.ResetCD();
		_seedPacketNode.isCDCooling = true;
		//GD.Print("MainGame: PlantSeed");

	}

	// 释放（松开、放回）植物
	public void PutBackPlant()
	{
		_seedClone.Visible = false;
		_seed.Visible = true;
		_seedClone.QueueFree();
		_seed.QueueFree();
		BIsSeedCardSelected = false;
	}


	// 刷新僵尸
	public async void RefreshZombie()
	{
		if (BIsRefreshingZombies)
		{
			return;
		}
		if (ZombieCurrentWave >= ZombieMaxWave)
		{
			Print("Game Over");
			return;
		}
		BIsRefreshingZombies = true;
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

		zombieMaxGrade *= 20; // 20倍数
		// Print("zombieCount: " + zombieCount);

		// 预备僵尸
		Zombie[] preZombie = new Zombie[100];// 预备僵尸数组
		int zombieCount = 0; // 预备僵尸数量
		for (int zombieCurrentGrade = 0; zombieCurrentGrade < zombieMaxGrade; zombieCount++)
		{
			ZombieTypeEnum zombieType = _zombieWeightsAndGrades.GetRandomZombieType();
			int tempGrade = _zombieWeightsAndGrades.GetZombieGrade(zombieType);
			if (tempGrade + zombieCurrentGrade > zombieMaxGrade)
			{
				zombieCount--; // 减1，重新尝试
				continue;
			}
			GD.Print("zombieType: " + zombieType, "tempGrade: " + tempGrade, "zombieCurrentGrade: " + zombieCurrentGrade);
			zombieCurrentGrade += tempGrade; // 增加僵尸当前等级
			preZombie[zombieCount] = _zombieType.GetZombieScene(zombieType).Instantiate() as Zombie;
			preZombie[zombieCount].Init(zombieType);
			// 预备僵尸
			//PreZombie[i] = Load<PackedScene>("res://MainGame/Zombies/Zombie.tscn").Instantiate() as Zombie;

			// 预备僵尸初始化
			int tempIndex = -1;

			if (Zombies[ZombieStack] != null)
			{
				tempIndex = Zombies[ZombieStack].Index;
				Zombies[ZombieStack].QueueFree();
			}
			

			Zombies[ZombieStack] = preZombie[zombieCount];// 预备僵尸加入栈数组
			
			preZombie[zombieCount].Index = ZombieStack; // 预备僵尸索引
			if (tempIndex != -1)
			{
				ZombieStack = tempIndex; // 预备僵尸索引更新
			}
			else
			{
				ZombieStack++; // 僵尸栈加1
			}
			ZombieCurrentWaveMaxHP += preZombie[zombieCount].MaxHP; // 计算当前波最大生命值

			//if (zombieCurrentGrade + PreZombie[i].Grade > zombieMaxGrade)
			//{
			//	i--; // 减1，重新尝试
			//	break;
			//}
			

		}
		// 刷新僵尸
		for (int i = 0; i < zombieCount; i++)
		{
			zombie = preZombie[i]; // 取出预备僵尸
			AddZombie(zombie);

			//AddChild(zombie);

			await ToSignal(GetTree().CreateTimer(0.5), SceneTreeTimer.SignalName.Timeout); //等待0.5秒再刷新下一个僵尸
		}
		// 如果当前波数大于最大波数，则连接僵尸死亡信号到游戏结束函数
		if (ZombieCurrentWave >= ZombieMaxWave)
		{
			// Print("Wave:" + ZombieCurrentWave + " MaxWave: " + ZombieMaxWave);
			zombie!.ZombieDie += GameOver; // 连接僵尸死亡信号到游戏结束函数
			// 最后一波僵尸死亡后，游戏结束
		}
		ZombieCurrentWave++; // 波数加1
		_zombieWeightsAndGrades.UpdateZombieWave(); // 更新僵尸波数以更新僵尸权重
		BIsRefreshingZombies = false; // 刷新结束
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

		int row = GetRandomZombieRow(); // 随机僵尸所在行
		Print("Row: " + row);

		zombie.Refresh(zombie.Index, GameScene, ZombieCurrentWave, row); // 刷新僵尸
		ZombiesNumOfRow[row]++; // 该行僵尸数加1
								//zombiesOfRow[Row, ] = zombie; // 该行僵尸数组加1
		ZombiesContainer.CallDeferred("add_child", zombie); // 添加到场景树

		ZombieNum += 1; // 总僵尸数加1
		UpdateZombieNum(); // 更新僵尸数
	}

	// 移除僵尸
	public void RemoveZombie(Zombie zombie)
	{
		(zombie.Index, ZombieStack) = (ZombieStack, zombie.Index);
		ZombieNum--;
		UpdateZombieNum();
	}

	public void UpdateZombieNum()
	{
		GD.Print("ZombieNum: " + ZombieNum, "BIsClimaxing: " + BIsClimaxing);
		if (BIsClimaxing)
		{
			if (ZombieNum <= 3)
			{
				BIsClimaxing = false;
				GameScene.TurnToNormalBgm();
			}
		}
		else
		{
			if (ZombieNum >= 10)
			{
				BIsClimaxing = true;
				GD.Print("Turn to HighBGM");
				GameScene.TurnToHighBgm();
			}
		}
	}

	// 判断全场僵尸血量在总血量的百分比
	public float GetZombieTotalHealthPercent()
	{
		_totalHealth = 0;
		for (int i = 0; i < 1000; i++)
		{
			if (Zombies[i] != null && Zombies[i].Wave == ZombieCurrentWave - 1)
			{
				_totalHealth += Zombies[i].HP > 0 ? Zombies[i].HP : 0;
			}
		}
		GD.Print("totalHealth: " + _totalHealth + " WaveMaxHP: " + ZombieCurrentWaveMaxHP);
		if (_totalHealth == 0 || ZombieCurrentWaveMaxHP == 0)
		{
			return 0;
		}
		return (float)_totalHealth / ZombieCurrentWaveMaxHP * 100;
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
		 *     将第i行权重定义为Weight_i
		 *     将第i行距离上次被选取定义为LastPicked_i
		 *     将第i行距离上上次被选取定义为SecondLastPicked_i
		 * LastPicked_i和SecondLastPicked_i的初始值为0。
		 * 
		 */

		/*
		 * 某一行出怪时——又称第j行插入事件：
		 * 
		 * 执行第j行插入事件时，会发生如下变化：
		 * ∀i∈[1,6]，如果Weight_i>0，则LastPicked_i和SecondLastPicked_i均增加1
		 * 将LastPicked_j的值赋给SecondLastPicked_j
		 * 将LastPicked_j设为0
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

		float smoothWeightAll = GameScene.SmoothWeight.Sum();
		
		float randomWeight = RNG.RandfRange(0, smoothWeightAll);
		int row;
		for (row = 0; row < GameScene.LawnUnitCount.Y - 1; row++)
		{
			randomWeight -= GameScene.SmoothWeight[row];
			if (randomWeight <= 0)
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
		GameScene.SecondLastPicked[row] = GameScene.LastPicked[row];
		GameScene.LastPicked[row] = 0;
		return row;
	}

	// 更新僵尸血量
	public void UpdateZombieHP()
	{
		//GD.Print("BIsRefreshingZombies: " + BIsRefreshingZombies);
		//for (int i = 0; i < 1000; i++)
		if (BIsRefreshingZombies)
		{
			return;
		}
		
		//GD.Print("TotalHealthPercent: " + GetZombieTotalHealthPercent() + "totalHealth: " + totalHealth + " WaveMaxHP: " + WaveMaxHP);
		if (ZombieCurrentWave <= ZombieMaxWave && GetZombieTotalHealthPercent() <= 60)
		{
			//BIsRefreshingZombies = true;
			Print("RefreshingZombies..." );
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
		(plant.Index, PlantStack) = (PlantStack, plant.Index);
		GameScene.LawnUnitClearPlant (plant.Col, plant.Row);
		//plants[plant.Index] = null;
		//plantStack = plant.Index;
	}

	

	// 刷新阳光
	public void RefreshSun()
	{
		GD.Print("RefreshSun");
		if (GD.Load<PackedScene>("res://MainGame/Drops/Sun.tscn").Instantiate() is Sun sun)
		{
			sun.Position = new Vector2(RNG.RandfRange(100, 700) + GameScene.CameraCenterPos.X, 90); // 设置阳光的位置
			sun.GroundPosY = RNG.RandiRange(200, 500); // 设置阳光的地面高度
			SunContainer.AddChild(sun); // 添加阳光到场景中
		}
		else
		{
			PrintErr("Sun is null");
		}

		SunRefreshedCount += 25; // 阳光掉落数加25

		
		RefreshSunTimer();
	
	}

	// 刷新阳光计时器
	public void RefreshSunTimer()
	{
		// 天降阳光的时间间隔T和本局游戏内已掉落的阳光数量有关。
		// 设已掉落的阳光数量为x，A = 10x + 425，B为0~274之间的随机数
		// 若A <= 950,则T = A + B，若A > 950，则T = 950 + B

		int sunRefreshTimeA = 10 * SunRefreshedCount + 425;
		int sunRefreshTimeB = RNG.RandiRange(0, 274);
		if (sunRefreshTimeA > 950)
		{
			sunRefreshTimeA = 950;
		}
		int sunRefreshTime = sunRefreshTimeA + sunRefreshTimeB;

		SunTimer.Start(sunRefreshTime / 100f);
		GD.Print("SunRefreshTime: " + sunRefreshTime);
	}


	// 游戏结束
	public void GameOver()
	{
		BIsGameOver = true;
		GD.Print("Game Over");
	}
}
