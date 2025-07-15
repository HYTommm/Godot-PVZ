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
}

public partial class LawnDayScene : Scene
{
	public LawnDayScene()
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
		
		Init();
	}

}
public partial class PoolDayScene : Scene
{
	public PoolDayScene()
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
