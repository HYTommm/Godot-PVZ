using Godot;
using static Godot.GD;

public class Scene
{
	// 草坪左上原点
	public Vector2 LawnLeftTopPos;
	// 草坪单位长度
	public int LawnUnitLength;
	// 草坪单位宽度
	public int LawnUnitWidth;
	public Vector2 LawnUnitSize;

	// 草坪长宽单位数量
	protected int LawnUniColCount;
	protected int LawnUniRowCount;
	public Vector2I LawnUnitCount;

	// 相机中心位置
	public Vector2 CameraCenterPos;
	// 相机右边位置
	public Vector2 CameraRightPos;
	// 背景图片
	public Texture2D BackGroundTexture;

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
	public void Init()
	{
		Weight = new float[LawnUniRowCount];
		LastPicked = new float[LawnUniRowCount];
		SecondLastPicked = new float[LawnUniRowCount];
		WeightP = new float[LawnUniRowCount];
		PLast = new float[LawnUniRowCount];
		PSecondLast = new float[LawnUniRowCount];
		SmoothWeight = new float[LawnUniRowCount];

		float WeightAll = 0;
		// 初始化草坪权重
		for (int i = 0; i < LawnUniRowCount; i++)
		{
			Weight[i] = 1;
			LastPicked[i] = 0;
			SecondLastPicked[i] = 0;
			WeightAll += Weight[i];
		}
		for (int i = 0; i < LawnUniRowCount; i++)
		{
			Print("Weight[i] = " + Weight[i] + ", WeightAll = " + WeightAll);
			WeightP[i] += Weight[i] / WeightAll;
		}
	}
}

public class LawnDayScene : Scene
{
	public LawnDayScene()
	{
		LawnLeftTopPos = new Vector2(260, 80);
		LawnUnitLength = 80;
		LawnUnitWidth = 100;
		LawnUnitSize = new Vector2(LawnUnitLength, LawnUnitWidth);

		LawnUniColCount = 9;
		LawnUniRowCount = 5;
		LawnUnitCount = new Vector2I(LawnUniColCount, LawnUniRowCount);

		CameraCenterPos = new Vector2(220, 0);
		CameraRightPos = new Vector2(600, 0);

		BackGroundTexture = GD.Load<Texture2D>("res://art/MainGame/background1.jpg");
		//Weight = new float[LawnUniRowCount];
		//LastPicked = new float[LawnUniRowCount];
		//SecondLastPicked = new float[LawnUniRowCount];
		//WeightP = new float[LawnUniRowCount];
		//PLast = new float[LawnUniRowCount];
		//PSecondLast = new float[LawnUniRowCount];
		//SmoothWeight = new float[LawnUniRowCount];

		Init();
	}

	//public void Init()
	//{
	//	float WeightAll = 0;
	//	// 初始化草坪权重
	//	for (int i = 0; i < LawnUniRowCount; i++)
	//	{
	//		Weight[i] = 1;
	//		LastPicked[i] = 0;
	//		SecondLastPicked[i] = 0;
	//		WeightAll += Weight[i];
	//	}
	//	for (int i = 0; i < LawnUniRowCount; i++)
	//	{
	//		Print("Weight[i] = " + Weight[i] + ", WeightAll = " + WeightAll);
	//		WeightP[i] += Weight[i]/WeightAll;
	//	}
	//}
}
public class PoolDayScene : Scene
{
	public PoolDayScene()
	{
		LawnLeftTopPos = new Vector2(260, 80);
		LawnUnitLength = 80;
		LawnUnitWidth = 85;
		LawnUnitSize = new Vector2(LawnUnitLength, LawnUnitWidth);

		LawnUniColCount = 9;
		LawnUniRowCount = 6;
		LawnUnitCount = new Vector2I(LawnUniColCount, LawnUniRowCount);

		CameraCenterPos = new Vector2(220, 0);
		CameraRightPos = new Vector2(600, 0);
		BackGroundTexture = GD.Load<Texture2D>("res://art/MainGame/background3.jpg");
		Init();
	}

	
}
