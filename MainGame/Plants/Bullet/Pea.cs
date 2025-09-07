using Godot;
using static ResourceDB.Sounds;

internal partial class Pea : Bullet
{
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
        BulletSplatsSound.Play();
    }
}
