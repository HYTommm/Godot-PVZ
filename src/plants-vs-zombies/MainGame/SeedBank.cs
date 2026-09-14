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
	/// 顺序即卡槽顺序（先选的排在左边）。
	///
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
		if (plants == null || plants.Count == 0)
		{
			GD.PrintErr("[SeedBank] 选卡结果为空，卡槽保持原样");
			return;
		}
		if (plants.Count > packets.Count)
		{
			GD.PrintErr($"[SeedBank] 选了 {plants.Count} 种植物，但只有 {packets.Count} 个卡槽，超出的会被丢弃");
		}

		for (int i = 0; i < packets.Count; i++)
		{
			SeedPacketLarger packet = packets[i];
			if (i < plants.Count)
			{
				packet.SetSeedScene(PlantTypes.Instance.GetScene(plants[i]));
				packet.Visible = true;
			}
			else
			{
				// 槽位多于选卡数时留空，而不是把剩下的槽位塞上重复植物
				packet.Visible = false;
			}
		}

		GD.Print($"[SeedBank] 卡槽已按选卡结果重排：{plants.Count} / {packets.Count}");
	}
}
