using Godot;

/// <summary>
/// 能上种子栏卡槽的实体。植物与僵尸都实现它，卡槽逻辑只认这四个成员，
/// 不关心装进来的到底是植物还是僵尸。
///
/// 刻意不做成公共基类：植物与僵尸分属两条继承链（Plants : Entity、Zombie : Entity），
/// 而卡槽要的只是"花费、冷却、鼠标偏移、预览透明度"这四个数值，与实体本身的能力无关。
/// </summary>
public interface ISeedEntity
{
	/// <summary>
	/// 只用于卡槽展示，不参与种植或出场。必须在入树（AddChild）之前置位，
	/// 否则 _Ready 已经跑完了。
	/// </summary>
	bool BIsDisplayOnly { get; set; }

	/// <summary>卡面显示的花费；小于 0 表示不显示价格（僵尸卡即免费）</summary>
	int SunCost { get; }

	/// <summary>卡面冷却时间（秒）。0 表示不冷却</summary>
	float CDtime { get; }

	/// <summary>跟随鼠标时，实体中心相对鼠标的偏移</summary>
	Vector2 Offset { get; }

	/// <summary>设整身透明度，用于手上的半透明预览与落在草坪上的克隆体</summary>
	void _SetAlpha(float alpha);
}
