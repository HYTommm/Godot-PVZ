using Godot;

// 伤害类型，使用二进制位来表示是否可被对应类型防具防御
public enum HurtType
{
	// 平射子弹伤害
	Direct = 0b0000_0011, // 可以被一类和二类防具防御
	// 投掷物伤害
	Thrown = 0b0001_0001, // 可以被一类防具防御
	// 爆炸伤害
	AshExplosion = 0b0010_0011, // 可以被一类和二类防具防御
	Explosion = 0b0011_0011, // 可以被一类和二类防具防御
	// 地刺类伤害
	Bomber = 0b0100_0001, // 可以被一类防具防御
	// 小推车类伤害
	LawnMower = 0b0101_0011, // 可以被一类和二类防具防御
	// 啃食伤害
	Eating = 0b0110_0011, // 可以被一类和二类防具防御
	// 其他伤害
	Other
}

public partial class Hurt(int damage, HurtType hurtType, bool bEnableTargetHitSFX = false) : Node2D
{
	public int Damage { get; set; } = damage;
	public HurtType HurtType { get; set; } = hurtType;

	// 受击方是否被允许发出音效
	public bool BEnableTargetHitSFX = true;

	//public Hurt HurtHealthEntity(HealthEntity entity)
	//{
	//    return this;
	//}

}
