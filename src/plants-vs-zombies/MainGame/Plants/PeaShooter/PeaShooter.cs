using Godot;
using System;

public partial class PeaShooter : PeaShooterSingle
{
    private int _currentShot = 0; // 当前是第几颗豌豆（0或1）

    public PeaShooter()
    {
        SunCost = 200; // 双发射手阳光消耗
        CDtime = 1f; // 冷却时间与原版单发射手相同
        AnimRate = 3.75f; // 动画播放速率
    }

    public override void _Idle()
    {
        // 使用双发射手的动画，如果没有则使用基类动画
        AnimIdle.CallDeferred("play", "PeaShooter_idle", -1, SpeedScaleOfIdle);
        AnimHead.CallDeferred("play", "Head_Idle", -1, SpeedScaleOfIdle);
    }

    /// <summary>
    /// 重写随机射击时间，将等待时间减少0.35秒，使得射击提前0.35秒发生
    /// </summary>
    public override void RandomShootTime()
    {
        // 计算原始随机等待时间（1.36~1.5秒）
        float waitTime = MainGame.Instance.RNG.RandiRange(ShootMinInterval, ShootMaxInterval) / 100.0f;
        // 减少0.35秒，使得_canShoot提前触发
        waitTime -= 0.35f;
        // 确保等待时间不小于最小值
        if (waitTime < 0.01f)
        {
            waitTime = 0.01f;
        }
        CanShootTimer.WaitTime = waitTime;
        CanShootTimer.Start(); // 启动计时器
    }

    /// <summary>
    /// 重写射击方法，双发射手在一轮射击中调用两次Shoot()
    /// 第一次射击：播放动画，0.26秒后发射第一颗豌豆，安排0.35秒后第二次射击
    /// 第二次射击：播放动画，0.26秒后发射第二颗豌豆，完成一轮射击
    /// </summary>
    public override void Shoot()
    {
        CanShoot = false; // 禁止射击

        // 播放射击动画（使用ShootCount决定是哪个动画，_currentShot不影响动画选择）
        AnimHead.Seek(0); // 从动画开头开始播放
        AnimHead.Play("Head_Shooting", 0.10, 3.75f);

        // 0.26秒后发射子弹
        GetTree().CreateTimer(0.26f).Timeout += ShootBullet;

        // 如果是第一次射击，安排0.35秒后进行第二次射击
        if (_currentShot == 0)
        {
            _currentShot = 1; // 标记为第二次射击
            GetTree().CreateTimer(0.35f).Timeout += Shoot;
        }
        else
        {
            // 第二次射击完成，重置状态，增加射击次数
            _currentShot = 0;
            ShootCount++; // 完成一轮射击（两颗豌豆）
            RandomShootTime(); // 随机射击时间（重置下一次完整射击的计时器）
        }
    }
}
