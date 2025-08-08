using System;
using System.Collections.Generic;
using System.Linq;

public class ZombieWeightsAndGrades
{
    private readonly Dictionary<ZombieTypeEnum, int> _zombieWeightsDict = new (); // 僵尸权重
    private readonly Dictionary<ZombieTypeEnum, int> _zombieGradesDict = new (); // 僵尸等级
    private readonly Dictionary<ZombieTypeEnum, bool> _zombieAllowedDict = new (); // 僵尸是否允许
    private int _zombieTotalWeight = 0;

    private int _zombieCurrentRound = 0;
    private int _zombieCurrentWave = 0;

	public ZombieWeightsAndGrades()
	{
		_zombieWeightsDict.Add(ZombieTypeEnum.Normal, 4000);
		_zombieWeightsDict.Add(ZombieTypeEnum.Conehead, 2000);
		_zombieWeightsDict.Add(ZombieTypeEnum.Buckethead, 1000);
		_zombieWeightsDict.Add(ZombieTypeEnum.Screendoor, 1500);

		_zombieTotalWeight = _zombieWeightsDict.Sum(x => x.Value);

		_zombieGradesDict.Add(ZombieTypeEnum.Normal, 1);
		_zombieGradesDict.Add(ZombieTypeEnum.Conehead, 2); // 2
		_zombieGradesDict.Add(ZombieTypeEnum.Buckethead, 4); // 4
		_zombieGradesDict.Add(ZombieTypeEnum.Screendoor, 3); // 3
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
		foreach (ZombieTypeEnum key in _zombieAllowedDict.Keys.ToList())
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
	/// 随机获取一个僵尸类型
	/// </summary>
	public ZombieTypeEnum GetRandomZombieType()
	{
		int randomWeight = new Random().Next(0, _zombieTotalWeight);
		foreach (ZombieTypeEnum zombie in _zombieWeightsDict.Keys.Where(zombie => _zombieAllowedDict[zombie]))
        {
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
