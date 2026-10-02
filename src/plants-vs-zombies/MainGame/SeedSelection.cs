using System.Collections.Generic;

/// <summary>
/// 一局开打前的选卡状态：从可选池里挑出要带上场的若干张卡。
///
/// 池子里装的是 SeedType——正式关只有植物，调试图关植物与僵尸混排，
/// 本类不区分两者，规则完全一样。
///
/// 刻意不做成 Node / Resource，也刻意不碰任何 UI：
///   - 可选池由调用方传入（正式关是全部植物，调试图关再加全部僵尸）。
///     将来接存档的"已解锁植物"时，只换这一个入参，本类不用动
///   - 槽位数由调用方传入（当前 = 种子栏里实际有几个卡槽）
/// 这样"选了几张、还能不能选"这套规则可以脱离界面单独推演。
/// </summary>
public class SeedSelection
{
	private readonly List<SeedType> _available = new();
	private readonly List<SeedType> _selected = new();

	/// <param name="available">可选池；空或 null 表示一种都选不了</param>
	/// <param name="maxSlots">卡槽数量，即最多能带几张卡</param>
	public SeedSelection(IEnumerable<SeedType> available, int maxSlots)
	{
		if (available != null)
		{
			foreach (SeedType type in available)
			{
				if (!_available.Contains(type))
				{
					_available.Add(type);
				}
			}
		}
		MaxSlots = maxSlots < 1 ? 1 : maxSlots;
	}

	/// <summary>最多能带几张卡</summary>
	public int MaxSlots { get; }

	/// <summary>可选池</summary>
	public IReadOnlyList<SeedType> Available => _available;

	/// <summary>已选的卡，顺序即卡槽顺序</summary>
	public IReadOnlyList<SeedType> Selected => _selected;

	/// <summary>已选数量</summary>
	public int SelectedCount => _selected.Count;

	/// <summary>是否已选满</summary>
	public bool IsFull => _selected.Count >= MaxSlots;

	/// <summary>是否可以开局。原版是选满才能开始</summary>
	public bool CanStart => _selected.Count >= MaxSlots;

	/// <summary>该卡是否已被选中</summary>
	public bool IsSelected(SeedType type) => _selected.Contains(type);

	/// <summary>该卡是否在可选池里</summary>
	public bool IsAvailable(SeedType type) => _available.Contains(type);

	/// <summary>
	/// 该卡在卡槽里的序号（1 起）；未选中返回 0。
	/// 序号 = 点选顺序，先点的排在前面。
	/// </summary>
	public int SlotNumber(SeedType type)
	{
		int index = _selected.IndexOf(type);
		return index < 0 ? 0 : index + 1;
	}

	/// <summary>
	/// 点一张卡：已选中的取消选择，未选中的在没满时选上。
	/// </summary>
	/// <returns>状态是否发生变化；false 表示这次点击无效（不在池里，或已经选满）</returns>
	public bool Toggle(SeedType type)
	{
		if (_selected.Remove(type))
		{
			return true;
		}
		if (!IsAvailable(type) || IsFull)
		{
			return false;
		}
		_selected.Add(type);
		return true;
	}

	/// <summary>清空已选</summary>
	public void Clear()
	{
		_selected.Clear();
	}
}
