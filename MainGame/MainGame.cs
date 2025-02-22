using Godot;
using static Godot.GD;
using System;
using static System.Formats.Asn1.AsnWriter;

public partial class MainGame : MainNode2D
{
	// 当前波数
	public int Wave;
	// 最大波数
	public int MaxWave;
	// 当前波最大生命值
	public int WaveMaxHP;
	
	public Camera camera;
	AnimationPlayer animation;
	Plants seed, seedClone;
	Node2D seedNode;
	
	// 植物栈
	public int plantStack = 0;
	// 植物数组
	public Plants[] plants = new Plants[1000];
	// 僵尸栈
	public int zombieStack = 0;
	// 僵尸数组
	public Zombie[] zombies = new Zombie[1000];
	// 场景
	public Scene GameScene;

	public bool isSeedCardSelected = false;
	public bool isGameOver = false;
	public bool isRefreshingZombies = false;

	public int totalHealth = 0;

	public RandomNumberGenerator RNG = new RandomNumberGenerator();

	
	public override void _Ready()
	{
		Print("RNG.Seed: " + RNG.Seed);
		RNG.Randomize();// 随机种子
		Print("RNG.Seed: " + RNG.Seed);
		GameScene = new LawnDayScene();// 设置场景
		//GD.Print("GameScene.Weight.Length: " + GameScene.Weight.Length);
		camera = GetNode<Camera>("./Camera");
		animation = GetNode<AnimationPlayer>("./CanvasLayer/AnimationPlayer");
		SelectSeedCard();
	}


	// Called every frame. 'delta' is the elapsed time since the previous frame.


	public override void _Process(double delta)
	{
		// 如果SeedCard被选中
		if (isSeedCardSelected)
		{
			seed.Position = seedNode.GetGlobalMousePosition() - new Vector2(35, 60);
			
			Vector2 MouseGlobalPos = GetGlobalMousePosition();
			if (   MouseGlobalPos.X >= GameScene.LawnLeftTopPos.X && MouseGlobalPos.X < GameScene.LawnLeftTopPos.X + GameScene.LawnUnitLength * 9
				&& MouseGlobalPos.Y >= GameScene.LawnLeftTopPos.Y && MouseGlobalPos.Y < GameScene.LawnLeftTopPos.Y + GameScene.LawnUnitWidth * 5)
			{
				Vector2 MouseUnitPos = 
					new Vector2((int)((MouseGlobalPos.X - GameScene.LawnLeftTopPos.X) / GameScene.LawnUnitLength),
								(int)((MouseGlobalPos.Y - GameScene.LawnLeftTopPos.Y) / GameScene.LawnUnitWidth));
				//GD.Print(MouseUnitPos);
				seedClone.Position = new Vector2(MouseUnitPos.X * GameScene.LawnUnitLength + GameScene.LawnLeftTopPos.X,
												 MouseUnitPos.Y * GameScene.LawnUnitWidth  + GameScene.LawnLeftTopPos.Y);
				seedClone.Visible = true;
			}
			else
			{
				seedClone.Visible = false;
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
				return;
			}
			PlantSeed();
		}

	}
	// 选择种子卡
	async public void SelectSeedCard()
	{
		
		await ToSignal(GetTree().CreateTimer(1.3), SceneTreeTimer.SignalName.Timeout);
		camera.Move(GameScene.CameraRightPos, 1.25);
		await ToSignal(camera, Camera.SignalName.MoveEnd);

		await ToSignal(GetTree().CreateTimer(1.2), SceneTreeTimer.SignalName.Timeout);
		Game();
	}
	
	// 开始游戏
	async public void Game()
	{
		camera.Move(GameScene.CameraCenterPos, 1);
		await ToSignal(camera, Camera.SignalName.MoveEnd);
		animation.Play("SeedBank");
		Wave = 1;
		MaxWave = 20;
	}

	// 选中种子
	public void AddSeed(Node2D node, Plants seed, Plants seedclone)
	{
		// 赋值
		this.seedNode = node;
		this.seed = seed;
		this.seedClone = seedclone;

		// 初始化种子
		seed.Position = seedNode.GetViewport().GetMousePosition() - new Vector2(35, 60);
		// 添加植物
		seedNode.GetCanvasLayerNode().AddChild(seed);

		// 初始化克隆种子
		seedClone.Visible = false;
		seedClone.SelfModulate = new Color(1, 1, 1, 0.6f);
		// 添加克隆植物
		AddChild(seedClone);
		
		isSeedCardSelected = true;
		//GD.Print(seedNode);
	}

	// 种植植物
	public void PlantSeed()
	{
		
		seed.Visible = false;
		
		isSeedCardSelected = false;

		plants[plantStack] = seedClone;
		plantStack++;

		seed.QueueFree();
		seedClone._Plant();
		
		//GD.Print("MainGame: PlantSeed");
		
	}

	// 刷新僵尸
	public async void RefreshZombie()
	{
		// 刷新僵尸
		// 每波容量上限 = int(int(当前波数 * 0.8) / 2) + 1，10的倍数为大波，大波容量上限乘2.5
		WaveMaxHP = 0;
		GD.Print("Wave: " + Wave);
		Zombie zombie = null;
		int zombieCount = (int)((int)(Wave * 0.8) / 2.0) + 1;
		// 判断是否是大波
		if (Wave % 10 == 0)
		{
			zombieCount = (int)(zombieCount * 2.5);
		}
		GD.Print("zombieCount: " + zombieCount);

		// 预备僵尸
		Zombie[] PreZombie = new Zombie[100];// 预备僵尸数组
		for (int i = 0; i < zombieCount; i++)
		{
			PreZombie[i] = GD.Load<PackedScene>("res://MainGame/Zombies/Zombie.tscn").Instantiate() as Zombie;
			zombies[zombieStack] = PreZombie[i];
			zombieStack++;
			WaveMaxHP += PreZombie[i].MaxHP;
		}
		// 刷新僵尸
		for (int i = 0; i < zombieCount; i++)
		{
			zombie = PreZombie[i];

			int Row = GetRandomZombieRow();
			Print("Row: " + Row);

			zombie.Refresh(zombieStack, GameScene, Wave, Row);
			CallDeferred("add_child", zombie);
			
			//AddChild(zombie);
			//等待0.5秒
			await ToSignal(GetTree().CreateTimer(0.5), SceneTreeTimer.SignalName.Timeout);
		}
		// 连接僵尸死亡信号
		if (Wave >= MaxWave)
		{
			GD.Print("Wave:" + Wave + " MaxWave: " + MaxWave);
			zombie.ZombieDie += GameOver;
			// 最后一波僵尸死亡后，游戏结束
		}
		Wave++;
		isRefreshingZombies = false;
	}

	// 移除僵尸
	public void RemoveZombie(Zombie zombie)
	{
		zombies[zombie.Index] = null;
		zombieStack = zombie.Index;
	}

	// 判断全场僵尸血量在总血量的百分比
	public float GetZombieTotalHealthPercent()
	{
		totalHealth = 0;
		for (int i = 0; i < 1000; i++)
		{
			if (zombies[i] != null && zombies[i].Wave == Wave - 1)
			{
				totalHealth += zombies[i].HP;
			}
		}
		//GD.Print("totalHealth: " + totalHealth + " WaveMaxHP: " + WaveMaxHP);
		if (totalHealth == 0 || WaveMaxHP == 0)
		{
			return 0;
		}
		return (float)totalHealth / WaveMaxHP * 100;
	}

	// 随机僵尸所在行
	public int GetRandomZombieRow()
	{
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

		float SmoothWeightAll = 0;
		for (int i = 0; i < GameScene.LawnUnitCount.Y; i++)
		{
			SmoothWeightAll += GameScene.SmoothWeight[i];
		}

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
	public async void UpdateZombieHP()
	{
		//GD.Print("isRefreshingZombies: " + isRefreshingZombies);
		for (int i = 0; i < 1000; i++)
		if (isRefreshingZombies)
		{
			return;
		}
		
		//GD.Print("TotalHealthPercent: " + GetZombieTotalHealthPercent() + "totleHealth: " + totalHealth + " WaveMaxHP: " + WaveMaxHP);
		if (Wave <= MaxWave && GetZombieTotalHealthPercent() <= 60)
		{
			isRefreshingZombies = true;
			GD.Print("RefreshingZombies..." );
			await ToSignal(GetTree().CreateTimer(2), SceneTreeTimer.SignalName.Timeout);
			GD.Print("RefreshZombie");
			RefreshZombie();
		}
	}

	// 移除植物
	public void RemovePlant(Plants plant)
	{
		plants[plant.Index] = null;
		plantStack = plant.Index;
	}

	// 游戏结束
	public void GameOver()
	{
		isGameOver = true;
		GD.Print("Game Over");
	}

}
