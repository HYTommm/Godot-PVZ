using System;
using System.Collections.Generic;
using System.Linq;

public class ZombieWeightsAndGrades
{
	private readonly Dictionary<ZombieTypeEnum, int> _zombieWeightsDict = new(); // 僵尸权重
	private readonly Dictionary<ZombieTypeEnum, int> _zombieGradesDict = new(); // 僵尸等级
	private readonly Dictionary<ZombieTypeEnum, bool> _zombieAllowedDict = new(); // 僵尸是否允许
	private readonly Dictionary<ZombieTypeEnum, int> _zombieFirstAllowedWaveDict = new(); // 首个允许出场的波数
	private int _zombieTotalWeight = 0;

	private int _zombieCurrentRound = 0;
	private int _zombieCurrentWave = 0;

	// 权重与等级取自原版：权重 = 抽取权重，等级 = 该僵尸占用的"点数"。
	// 注意：这些只是"全局默认值"，实际每局的僵尸池由关卡数据 LevelData.WavePool 覆盖
	//      （见 ApplyWavePool）。原版在冒险模式下不对权重做衰减，衰减只发生在生存模式。
	public ZombieWeightsAndGrades()
	{
		_zombieWeightsDict.Add(ZombieTypeEnum.Normal, 4000);
		_zombieWeightsDict.Add(ZombieTypeEnum.Conehead, 4000);
		_zombieWeightsDict.Add(ZombieTypeEnum.Buckethead, 3000);
		_zombieWeightsDict.Add(ZombieTypeEnum.Screendoor, 3500);
		_zombieWeightsDict.Add(ZombieTypeEnum.Polevaulter, 2000);
		_zombieWeightsDict.Add(ZombieTypeEnum.Newspaper, 1000);
		_zombieWeightsDict.Add(ZombieTypeEnum.Football, 2000);

		_zombieTotalWeight = _zombieWeightsDict.Sum(x => x.Value);

		_zombieGradesDict.Add(ZombieTypeEnum.Normal, 1);
		_zombieGradesDict.Add(ZombieTypeEnum.Conehead, 2);
		_zombieGradesDict.Add(ZombieTypeEnum.Buckethead, 4);
		_zombieGradesDict.Add(ZombieTypeEnum.Screendoor, 4);
		_zombieGradesDict.Add(ZombieTypeEnum.Polevaulter, 2);
		_zombieGradesDict.Add(ZombieTypeEnum.Newspaper, 2);
		_zombieGradesDict.Add(ZombieTypeEnum.Football, 7);

		// 首个允许出场的波数同样取自原版：撑杆跳 / 铁栅门 / 橄榄球 = 5，其余 = 1
		_zombieFirstAllowedWaveDict.Add(ZombieTypeEnum.Normal, 1);
		_zombieFirstAllowedWaveDict.Add(ZombieTypeEnum.Conehead, 1);
		_zombieFirstAllowedWaveDict.Add(ZombieTypeEnum.Buckethead, 1);
		_zombieFirstAllowedWaveDict.Add(ZombieTypeEnum.Screendoor, 5);
		_zombieFirstAllowedWaveDict.Add(ZombieTypeEnum.Polevaulter, 5);
		_zombieFirstAllowedWaveDict.Add(ZombieTypeEnum.Newspaper, 1);
		_zombieFirstAllowedWaveDict.Add(ZombieTypeEnum.Football, 5);

		_zombieAllowedDict.Add(ZombieTypeEnum.Normal, false);
		_zombieAllowedDict.Add(ZombieTypeEnum.Conehead, false);
		_zombieAllowedDict.Add(ZombieTypeEnum.Football, false);
		_zombieAllowedDict.Add(ZombieTypeEnum.Buckethead, false);
		_zombieAllowedDict.Add(ZombieTypeEnum.Screendoor, false);
		_zombieAllowedDict.Add(ZombieTypeEnum.Polevaulter, false);
		_zombieAllowedDict.Add(ZombieTypeEnum.Newspaper, false);
	}

	/// <summary>
	/// 更新当前轮次，并更新僵尸权重
	/// </summary>
	public void UpdateZombieRound()
	{
		_zombieCurrentRound++;
		if (_zombieCurrentRound is >= 5 and < 25)
		{
			_zombieWeightsDict[ZombieTypeEnum.Normal] -= 180;
			_zombieWeightsDict[ZombieTypeEnum.Conehead] -= 150;
		}
	}

	/// <summary>
	/// 更新当前波次，并更新僵尸权重
	/// </summary>
	public void UpdateZombieWave()
	{
		_zombieCurrentWave++;
	}

	/// <summary>
	/// 设置僵尸允许的类型
	/// </summary>
	/// <param name="allowedZombies"></param>
	public void SetZombieAllowed(List<ZombieTypeEnum> allowedZombies)
	{
		// ZombieAllowedDict.Clear()
		foreach (ZombieTypeEnum key in _zombieAllowedDict.Keys)
		{
			_zombieAllowedDict[key] = false;
		}
		foreach (ZombieTypeEnum zombie in allowedZombies)
		{
			_zombieAllowedDict[zombie] = true;
		}
		_zombieTotalWeight = _zombieWeightsDict
							.Where(pair =>
								_zombieAllowedDict.ContainsKey(pair.Key) && // 确保键存在
								_zombieAllowedDict[pair.Key])
							.Sum(x => x.Value);
	}

	/// <summary>
	/// 用关卡数据里的僵尸池覆盖权重和等级，并据此重设允许出场的类型。
	///
	/// 构造函数里那套权重/等级是"全局默认值"，这里只覆盖池里明确提到的类型；
	/// 池里没提到的类型一律禁止出场（否则关卡数据形同虚设）。
	/// </summary>
	/// <param name="wavePool">关卡的僵尸池；传 null 视作空池（全部禁止）</param>
	public void ApplyWavePool(Godot.Collections.Array<ZombieWaveEntry> wavePool)
	{
		List<ZombieTypeEnum> allowedZombies = new();
		if (wavePool != null)
		{
			foreach (ZombieWaveEntry entry in wavePool)
			{
				if (entry == null)
				{
					continue;
				}
				if (_zombieWeightsDict.ContainsKey(entry.Type))
				{
					_zombieWeightsDict[entry.Type] = entry.Weight;
					_zombieGradesDict[entry.Type] = entry.Grade;
					_zombieFirstAllowedWaveDict[entry.Type] = entry.FirstAllowedWave;
				}
				if (entry.Allowed)
				{
					allowedZombies.Add(entry.Type);
				}
			}
		}
		SetZombieAllowed(allowedZombies);
	}

	/// <summary>
	/// 某个僵尸类型在当前波数下是否可被抽到：
	/// 既要本关允许出场，又要已到它的"首个允许出场波"。
	/// </summary>
	private bool IsEligible(ZombieTypeEnum zombie, int wave)
	{
		if (!_zombieAllowedDict.TryGetValue(zombie, out bool allowed) || !allowed)
		{
			return false;
		}
		// 原版的判定是"波索引 + 1 >= 首个允许出场波"才可用，
		// 因为波索引 0 起算、"第 1 波"对应索引 0。
		// 所以 FirstAllowedWave = 5 表示从第 5 波起可出场，即波索引 >= 4
		return !_zombieFirstAllowedWaveDict.TryGetValue(zombie, out int firstAllowedWave)
			|| wave + 1 >= firstAllowedWave;
	}

	/// <summary>
	/// 随机获取一个僵尸类型
	/// </summary>
	/// <param name="wave">
	/// 当前波索引（0 起，与 PVZ 的波索引一致）。原版每种僵尸有独立的"首个允许出场波"，
	/// 未到该波的类型不参与本次抽取，所以权重总和也要按波数重新累加。
	/// </param>
	public ZombieTypeEnum GetRandomZombieType(int wave)
	{
		int totalWeight = 0;
		foreach (ZombieTypeEnum zombie in _zombieWeightsDict.Keys)
		{
			if (IsEligible(zombie, wave))
			{
				totalWeight += _zombieWeightsDict[zombie];
			}
		}
		if (totalWeight <= 0)
		{
			return ZombieTypeEnum.Normal;
		}

		int randomWeight = new Random().Next(0, totalWeight);
		foreach (ZombieTypeEnum zombie in _zombieWeightsDict.Keys)
		{
			if (!IsEligible(zombie, wave))
			{
				continue;
			}
			randomWeight -= _zombieWeightsDict[zombie];
			if (randomWeight < 0)
			{
				return zombie;
			}
		}
		return ZombieTypeEnum.Normal;
	}

	/// <summary>
	/// 获取僵尸的等级
	/// </summary>
	/// <param name="zombieType"></param>
	public int GetZombieGrade(ZombieTypeEnum zombieType)
	{
		return _zombieGradesDict[zombieType];
	}
}
