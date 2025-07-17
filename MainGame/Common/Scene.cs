using Godot;
using static Godot.GD;

public abstract partial class Scene : Node
{
	// 草坪左上原点
	public Vector2 LawnLeftTopPos;
	// 草坪单位长度
	public int LawnUnitLength;
	// 草坪单位宽度
	public int LawnUnitWidth;
	public Vector2 LawnUnitSize;

	// 草坪长宽单位数量
	protected int LawnUnitColCount;
	protected int LawnUnitRowCount;
	public Vector2I LawnUnitCount;
	// 草坪二维数组
	public Lawn[,] LawnArray;

	// 相机中心位置
	public Vector2 CameraCenterPos;
	// 相机右边位置
	public Vector2 CameraRightPos;

	// 小推车位置常量
	public Vector2 LawnMoverPos;

	// 背景图片
	public Texture2D BackGroundTexture;

	// 选卡BGM
	public AudioStream BgmSelectSeedCard;

	// 普通BGM_前奏
	public AudioStream BgmNormalPre;
	// 普通BGM_循环
	public AudioStream BgmNormalLoop;
	// 高潮BGM_前奏
	public AudioStream BgmHighPre;
	// 高潮BGM_循环
	public AudioStream BgmHighLoop;

	// 普通BGM的前奏是否播放完毕
	public bool BgmNormalPrePlayed = false;
	// 高潮BGM的前奏是否播放完毕
	public bool BgmHighPrePlayed = false;

	// 普通BGM播放器
	public AudioStreamPlayer BgmNormalPlayer;
	// 高潮BGM播放器
	public AudioStreamPlayer BgmHighPlayer;
	

	// 将第i行权重定义为 Weight_i
	// 将第i行距离上次被选取定义为 LastPicked_i
	// 将第i行距离上上次被选取定义为 SecondLastPicked_i
	// 将第i行出怪权重在总权重的占比称之为 WeightP_i
	// 将 LastPicked_i 对结果的影响因子称为 PLast_i，
	// 再将 SecondLastPicked_i 对结果的影响因子称为 PSecondLast_i
	// 将受到 LastPicked_i 和 SecondLastPicked_i 影响后的Weighti称之为平滑权重，记为 SmoothWeight_i
	public float[] Weight;
	public float[] LastPicked;
	public float[] SecondLastPicked;
	public float[] WeightP;
	public float[] PLast;
	public float[] PSecondLast;
	public float[] SmoothWeight;

	public Scene(Node Global)
	{
		BgmNormalPlayer = Global.GetNode<AudioStreamPlayer>("BGM1");
		BgmHighPlayer = Global.GetNode<AudioStreamPlayer>("BGM2");
		BgmNormalPlayer.VolumeDb = -10;
		BgmHighPlayer.VolumeDb = -80;

		BgmNormalPlayer.Finished += OnBgmNormalPreFinished;
		BgmHighPlayer.Finished += OnBgmHighPreFinished;
	}

	public void Init()
	{
		Weight = new float[LawnUnitRowCount];
		LastPicked = new float[LawnUnitRowCount];
		SecondLastPicked = new float[LawnUnitRowCount];
		WeightP = new float[LawnUnitRowCount];
		PLast = new float[LawnUnitRowCount];
		PSecondLast = new float[LawnUnitRowCount];
		SmoothWeight = new float[LawnUnitRowCount];
		LawnArray = new Lawn[LawnUnitColCount, LawnUnitRowCount];
		for (int i = 0; i < LawnUnitColCount; i++)
		{
			for (int j = 0; j < LawnUnitRowCount; j++)
			{
				LawnArray[i, j] = new Lawn();
			}
		}

		float WeightAll = 0;
		// 初始化草坪权重
		for (int i = 0; i < LawnUnitRowCount; i++)
		{
			Weight[i] = 1;
			LastPicked[i] = 0;
			SecondLastPicked[i] = 0;
			WeightAll += Weight[i];
		}
		for (int i = 0; i < LawnUnitRowCount; i++)
		{
			Print("Weight[i] = " + Weight[i] + ", WeightAll = " + WeightAll);
			WeightP[i] += Weight[i] / WeightAll;
		}

		

	}

	public void OnBgmNormalPreFinished()
	{
		//float db = BgmNormalPlayer.VolumeDb;
		if (BgmNormalPrePlayed == false)
		{
			BgmNormalPrePlayed = true;
			BgmNormalPlayer.Stream = BgmNormalLoop;
			//BgmNormalPlayer.VolumeDb = db;
			BgmNormalPlayer.Play();
		}
		BgmNormalPlayer.Play();
	}

	public void OnBgmHighPreFinished()
	{
		//float db = BgmHighPlayer.VolumeDb;
		if (BgmHighPrePlayed == false)
		{
			BgmHighPrePlayed = true;
			BgmHighPlayer.Stream = BgmHighLoop;
			//BgmHighPlayer.VolumeDb = db;
			BgmHighPlayer.Play();
		}
		BgmHighPlayer.Play();
	}

	/// <summary>
	/// 查看指定位置是否为空
	/// </summary>
	/// <param name="row"></param>
	/// <param name="col"></param>
	/// <returns></returns>
	public bool IsLawnUnitPlantEmpty(int row, int col)
	{
		return LawnArray[row, col].PlantCount == 0;
	}

	/// <summary>
	/// 放置植物
	/// </summary>
	/// <param name="row"></param>
	/// <param name="col"></param>
	public void LawnUnitPlacePlant(int row, int col)
	{
		LawnArray[row, col].PlantCount++;
	}

	/// <summary>
	/// 移除植物
	/// </summary>
	/// <param name="row"></param>
	/// <param name="col"></param>
	public void LawnUnitRemovePlant(int row, int col)
	{
		LawnArray[row, col].PlantCount--;
	}

	/// <summary>
	/// 清空指定位置
	/// </summary>
	/// <param name="row"></param>
	/// <param name="col"></param>
	public void LawnUnitClearPlant(int row, int col)
	{
		LawnArray[row, col].PlantCount = 0;
	}


	public virtual void TurnToNormalBGM()
	{

		//BgmNormalPlayer.VolumeDb = -10;
		//BgmHighPlayer.VolumeDb = -80;
		Tween tween = BgmNormalPlayer.CreateTween();
		tween
			.TweenProperty(BgmNormalPlayer, "volume_db", -10, 5f)
			.SetEase(Tween.EaseType.InOut)
			.SetTrans(Tween.TransitionType.Linear);
		tween
			.TweenProperty(BgmHighPlayer, "volume_db", -80, 5f)
			.SetEase(Tween.EaseType.InOut)
			.SetTrans(Tween.TransitionType.Linear);
	}

	public virtual void TurnToHighBGM()
	{
		//BgmNormalPlayer.VolumeDb = -80;
		//BgmHighPlayer.VolumeDb = -10;

		Tween tween = BgmNormalPlayer.CreateTween();
		tween
			.TweenProperty(BgmNormalPlayer, "volume_db", -80, 5f)
			.SetEase(Tween.EaseType.InOut)
			.SetTrans(Tween.TransitionType.Linear);
		tween
			.TweenProperty(BgmHighPlayer, "volume_db", -10, 5f)
			.SetEase(Tween.EaseType.InOut)
			.SetTrans(Tween.TransitionType.Linear);
	}
	public virtual void PlaySelectSeedCardBGM()
	{
		BgmNormalPlayer.Stream = BgmSelectSeedCard;
		BgmHighPlayer.Stream = BgmSelectSeedCard;
		PlayAllBGM();
	}

	public virtual void PlayMaineGameBGM()
	{
		BgmNormalPlayer.Stream = BgmNormalPre;
		BgmNormalPlayer.VolumeDb = -10;
		BgmHighPlayer.Stream = BgmHighPre;
		PlayAllBGM();
	}

	protected virtual void PlayAllBGM()
	{
		BgmNormalPlayer.Play();
		BgmHighPlayer.Play();
	}

	public virtual void TurnOffAllBGM_FadeOut(double duration = 2f)
	{
		TurnBGM_FadeOut(BgmNormalPlayer, duration);
		TurnBGM_FadeOut(BgmHighPlayer, duration);
	}

	protected virtual void TurnBGM_FadeOut(AudioStreamPlayer player, double duration)
	{
		Tween tween = player.CreateTween();
		tween
			.TweenProperty(player, "volume_db", -80, duration)
			.SetEase(Tween.EaseType.InOut)
			.SetTrans(Tween.TransitionType.Linear);
	}

	public virtual void StopAllBGM()
	{
		BgmNormalPlayer.Stop();
		BgmHighPlayer.Stop();
	}
}

public partial class LawnDayScene : Scene
{
	public LawnDayScene(Node Global) : base(Global)
	{
		LawnLeftTopPos = new Vector2(260, 80);
		LawnUnitLength = 80;
		LawnUnitWidth = 100;
		LawnUnitSize = new Vector2(LawnUnitLength, LawnUnitWidth);

		LawnUnitColCount = 9;
		LawnUnitRowCount = 5;
		LawnUnitCount = new Vector2I(LawnUnitColCount, LawnUnitRowCount);

		CameraCenterPos = new Vector2(220, 0);
		CameraRightPos = new Vector2(600, 0);

		LawnMoverPos = new Vector2(194.5f, 121f);

		BackGroundTexture = Load<Texture2D>("res://art/MainGame/background1.jpg");

		BgmSelectSeedCard = Load<AudioStream>("res://sounds/MainGame/select_seedcard.ogg");

		BgmNormalPre = Load<AudioStream>("res://sounds/MainGame/daylawn_part1_nodrum.ogg");
		BgmNormalLoop = Load<AudioStream>("res://sounds/MainGame/daylawn_part2_nodrum.ogg");

		BgmHighPre = Load<AudioStream>("res://sounds/MainGame/daylawn_part1_drum.ogg");
		BgmHighLoop = Load<AudioStream>("res://sounds/MainGame/daylawn_part2_drum.ogg");
		
		Init();
	}

	//public override void TurnToNormalBGM()
	//{
	//	GD.Print("LawnDayScene::TurnToNormalBGM");
	//	Tween tween = BgmHighPlayer.CreateTween();
	//	tween
	//		.TweenProperty(BgmHighPlayer, "volume_db", -80, 5f)
	//		.SetEase(Tween.EaseType.InOut)
	//		.SetTrans(Tween.TransitionType.Linear);
	//}

	public override void TurnToHighBGM()
	{
		//BgmHighPlayer.VolumeDb = -10;
		GD.Print("LawnDayScene::TurnToHighBGM");
		Tween tween = BgmHighPlayer.CreateTween();
		tween
			.TweenProperty(BgmHighPlayer, "volume_db", -10, 5f)
			.SetEase(Tween.EaseType.InOut)
			.SetTrans(Tween.TransitionType.Linear);
		// 在 Tween 完成后打印实际音量
		tween.Finished += () => GD.Print("Final volume: " + BgmHighPlayer.VolumeDb);
	}

}
public partial class PoolDayScene : Scene
{
	public PoolDayScene(Node Global) : base(Global)
	{
		LawnLeftTopPos = new Vector2(260, 80);
		LawnUnitLength = 80;
		LawnUnitWidth = 85;
		LawnUnitSize = new Vector2(LawnUnitLength, LawnUnitWidth);

		LawnUnitColCount = 9;
		LawnUnitRowCount = 6;
		LawnUnitCount = new Vector2I(LawnUnitColCount, LawnUnitRowCount);

		CameraCenterPos = new Vector2(220, 0);
		CameraRightPos = new Vector2(600, 0);
		BackGroundTexture = GD.Load<Texture2D>("res://art/MainGame/background3.jpg");
		Init();
	}

	
}
