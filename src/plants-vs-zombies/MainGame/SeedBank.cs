using Godot;
using static ResourceDB.Sounds;
using System;
using System.Collections.Generic;

public partial class SeedBank : Sprite2D
{
	public static SeedBank Instance => _instance;
	private static SeedBank _instance;

	//public MainGame MainGame; // 主游戏节点
	[Export] private Label _sunCountLabel;
	[Export] private AnimationPlayer _animSunCountFlashWarning;

	public AudioStreamPlayer FlashWarningSound = new();
	// 布尔值：是否禁止选卡
	public bool BIsForbiddenSelect = false;
	public override void _Ready()
	{
		//UpdateSunCount();
		//MainGame = MainGame.Instance;
		FlashWarningSound.Stream = Sound_Buzzer;
		AddChild(FlashWarningSound);

		_instance = this;
	}
	public void UpdateSunCount()
	{
		int count = MainGame.Instance.SunCount;
		_sunCountLabel.Text = count.ToString();
	}

	public void SunCountFlashWarning()
	{
		_animSunCountFlashWarning.Play("SunCountFlashWarning");
		FlashWarningSound.Play();
	}

	/// <summary>
	/// 按场景树顺序取全部卡槽。卡槽数是"本关能带几种植物"的实际依据，
	/// 也是选卡界面槽位数的来源，所以不再单独配一个数字。
	/// </summary>
	public List<SeedPacketLarger> GetSeedPackets()
	{
		List<SeedPacketLarger> packets = new();
		foreach (Node child in GetChildren())
		{
			if (child is SeedPacketLarger packet)
			{
				packets.Add(packet);
			}
		}
		return packets;
	}

	/// <summary>
	/// 按选卡结果重排卡槽：第 i 个卡槽换成 plants[i]，槽位多于选择数的隐藏。
	/// 顺序即卡槽顺序（先选的排在左边）。传空列表就是把卡槽全清空。
	///
	/// 卡槽在场景里默认是隐藏的，开局该是空种子栏，这里只负责把选中的显示出来。
	/// 只换每张卡对应的植物，不动节点结构——卡槽的位置、输入信号连接都在场景里定义好，
	/// 重建节点反而要重新接线。
	/// </summary>
	public void ApplySeedSelection(IReadOnlyList<PlantTypeEnum> plants)
	{
		List<SeedPacketLarger> packets = GetSeedPackets();
		if (packets.Count == 0)
		{
			GD.PrintErr("[SeedBank] 一个卡槽都没找到，选卡结果无法应用");
			return;
		}
		// 空列表是合法输入：表示把卡槽全清空
		if (plants == null)
		{
			GD.PrintErr("[SeedBank] 选卡结果为 null，卡槽保持原样");
			return;
		}
		if (plants.Count > packets.Count)
		{
			GD.PrintErr($"[SeedBank] 选了 {plants.Count} 种植物，但只有 {packets.Count} 个卡槽，超出的会被丢弃");
		}

		for (int i = 0; i < packets.Count; i++)
		{
			SeedPacketLarger packet = packets[i];
			packet.Visible = true; // 槽位本身始终在，区别只在里面有没有卡

			if (i < plants.Count)
			{
				packet.SetSeedScene(PlantTypes.Instance.GetScene(plants[i]));
			}
			else
			{
				// 没派上用场的槽位退回空槽，露出白色卡槽底。
				// 不能整个隐藏——那样连槽位本身都看不见了
				packet.SetEmptySlot();
			}
		}

		GD.Print($"[SeedBank] 卡槽已按选卡结果重排：{plants.Count} / {packets.Count}");
	}
}
