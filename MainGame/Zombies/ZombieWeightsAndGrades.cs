using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;

//using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class ZombieWeightsAndGrades
{
    Godot.Collections.Dictionary<ZombieTypeEnum, int> ZombieWeightsDict = new (); // 僵尸权重
    Godot.Collections.Dictionary<ZombieTypeEnum, int> ZombieGradesDict = new (); // 僵尸等级
    Godot.Collections.Dictionary<ZombieTypeEnum, bool> ZombieAllowedDict = new (); // 僵尸是否允许
    int ZombieTotalWeight = 0;

    int ZombieCurrentRound = 0;
    int ZombieCurrentWave = 0;

    public ZombieWeightsAndGrades()
    {
        ZombieWeightsDict.Add(ZombieTypeEnum.Normal, 4000);
        ZombieWeightsDict.Add(ZombieTypeEnum.Conehead, 2000);
        ZombieWeightsDict.Add(ZombieTypeEnum.Buckethead, 1000);

        ZombieTotalWeight = ZombieWeightsDict.Sum(x => x.Value);

        ZombieGradesDict.Add(ZombieTypeEnum.Normal, 1);
        ZombieGradesDict.Add(ZombieTypeEnum.Conehead, 2);
        ZombieGradesDict.Add(ZombieTypeEnum.Buckethead, 4);
    }

    /// <summary>
    /// 更新当前轮次，并更新僵尸权重
    /// </summary>
    public void UpdateZombieRound()
    {
        ZombieCurrentRound++;
        if (ZombieCurrentRound >= 5 && ZombieCurrentRound < 25)
        {
            ZombieWeightsDict[ZombieTypeEnum.Normal] -= 180;
            ZombieWeightsDict[ZombieTypeEnum.Conehead] -= 150;
        }
    }

    /// <summary>
    /// 更新当前波次，并更新僵尸权重
    /// </summary>
    public void UpdateZombieWave()
    {
        ZombieCurrentWave++;
    }

    /// <summary>
    /// 设置僵尸允许的类型
    /// </summary>
    /// <param name="allowedZombies"></param>
    public void SetZombieAllowed(List<ZombieTypeEnum> allowedZombies)
    {
        ZombieAllowedDict.Clear();
        foreach (ZombieTypeEnum zombie in allowedZombies)
        {
            ZombieAllowedDict.Add(zombie, true);
        }
        ZombieTotalWeight = ZombieWeightsDict.Sum(x => x.Value);
    }

    /// <summary>
    /// 随机获取一个僵尸类型
    /// </summary>
    public ZombieTypeEnum GetRandomZombieType()
    {
        int randomWeight = new Random().Next(0, ZombieTotalWeight);
        foreach (ZombieTypeEnum zombie in ZombieWeightsDict.Keys)
        {
            if (!ZombieAllowedDict[zombie])
            {
                continue;
            }
            randomWeight -= ZombieWeightsDict[zombie];
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
        return ZombieGradesDict[zombieType];
    }
}