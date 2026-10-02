using Godot;
using System;
using StructInheritance;
using static ResourceDB.Sounds;

public partial class Pea : Bullet
{
    /// <summary>
    /// 测试用：豌豆不打溅射粒子，方便单看子弹碰撞本身的开销。
    /// 恢复正常观感就把这行删掉（基类默认是 true）。
    /// </summary>
    protected override bool BEnableSplats => false;

    public override void PlaySplatSound()
    {
        // 随机数
        uint random = GD.Randi() % 3;
        //GD.Print("Random number: " + random);
        // 根据随机数播放不同的爆炸声音
        BulletSplatsSound.Stream = random switch
        {
            0 => Sound_Splat,
            1 => Sound_Splat2,
            2 => Sound_Splat3,
            _ => BulletSplatsSound.Stream
        };

        // 播放爆炸声音
        BulletSplatsSound.PitchScale = MainGame.Instance.RNG.RandfRange(1.0f, 1.5f);
        BulletSplatsSound.Play();
    }
}
