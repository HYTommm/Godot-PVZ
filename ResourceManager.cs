using Godot;
using static Godot.GD;

public static class ResourceManager
{
	public static class Sounds
	{
		public static readonly AudioStream Sound_Bleep       = Load<AudioStream>("res://sounds/bleep.ogg");
		public static readonly AudioStream Sound_Tap         = Load<AudioStream>("res://sounds/tap.ogg");
		public static readonly AudioStream Sound_Tap2        = Load<AudioStream>("res://sounds/tap2.ogg");
		public static readonly AudioStream Sound_GraveButton = Load<AudioStream>("res://sounds/gravebutton.ogg");
		public static readonly AudioStream Sound_Points      = Load<AudioStream>("res://sounds/points.ogg"); 

		public static readonly AudioStream Sound_LoseMusic   = Load<AudioStream>("res://sounds/losemusic.ogg");
		public static readonly AudioStream Sound_EvilLaugh   = Load<AudioStream>("res://sounds/evillaugh.ogg");

		public static readonly AudioStream Sound_Splat       = Load<AudioStream>("res://sounds/splat.ogg");
		public static readonly AudioStream Sound_Splat2      = Load<AudioStream>("res://sounds/splat2.ogg");
		public static readonly AudioStream Sound_Splat3      = Load<AudioStream>("res://sounds/splat3.ogg");

		public static readonly AudioStream Sound_Chomp       = Load<AudioStream>("res://sounds/chomp.ogg");
		public static readonly AudioStream Sound_Chomp2      = Load<AudioStream>("res://sounds/chomp2.ogg");
		public static readonly AudioStream Sound_ChompSoft   = Load<AudioStream>("res://sounds/chompsoft.ogg");

		public static readonly AudioStream Sound_PlasticHit  = Load<AudioStream>("res://sounds/plastichit.ogg");
		public static readonly AudioStream Sound_PlasticHit2 = Load<AudioStream>("res://sounds/plastichit2.ogg");

		public static readonly AudioStream Sound_ShieldHit   = Load<AudioStream>("res://sounds/shieldhit.ogg");
		public static readonly AudioStream Sound_ShieldHit2  = Load<AudioStream>("res://sounds/shieldhit2.ogg");

		public static readonly AudioStream Sound_LawnMower   = Load<AudioStream>("res://sounds/lawnmower.ogg");

		public static class Bgm
		{
			public static readonly AudioStream Bgm_SelectSeedCard = Load<AudioStream>("res://sounds/MainGame/select_seedcard.ogg");

			public static readonly AudioStream Bgm_DayLawnPart1_NoDrum = Load<AudioStream>("res://sounds/MainGame/daylawn_part1_nodrum.ogg");
			public static readonly AudioStream Bgm_DayLawnPart2_NoDrum = Load<AudioStream>("res://sounds/MainGame/daylawn_part2_nodrum.ogg");

			public static readonly AudioStream Bgm_DayLawnPart1_Drum = Load<AudioStream>("res://sounds/MainGame/daylawn_part1_drum.ogg");
			public static readonly AudioStream Bgm_DayLawnPart2_Drum = Load<AudioStream>("res://sounds/MainGame/daylawn_part2_drum.ogg");
		}

	}

	
	
	public static class Images
	{
		public static class Plants
		{

		}

		public static class Zombies
		{
			public static readonly Texture2D ImageZombie_OuterarmUpper  = Load<Texture2D>("uid://dqukg0fsdw4tq"); // Load<Texture2D>("res://art/MainGame/Zombie/Zombie_outerarm_upper.png");
			public static readonly Texture2D ImageZombie_OuterarmUpper2 = Load<Texture2D>("uid://dxpyd5i0pexiw"); // Load<Texture2D>("res://art/MainGame/Zombie/Zombie_outerarm_upper2.png");
			
			public static class Armors
			{
				public static readonly Texture2D ImageZombieArmor_Cone1           = Load<Texture2D>("uid://c14f2wbgkjgnx"); // Load<Texture2D>("res://art/MainGame/Zombie/Zombie_cone1.png");
				public static readonly Texture2D ImageZombieArmor_Cone2           = Load<Texture2D>("uid://drpa3c3koywbt"); // Load<Texture2D>("res://art/MainGame/Zombie/Zombie_cone2.png");
				public static readonly Texture2D ImageZombieArmor_Cone3           = Load<Texture2D>("uid://cmyr866hx6nrq"); // Load<Texture2D>("res://art/MainGame/Zombie/Zombie_cone3.png");

				public static readonly Texture2D ImageZombieArmor_Bucket1         = Load<Texture2D>("uid://dfa4gh7yk78ip"); // Load<Texture2D>("res://art/MainGame/Zombie/Zombie_bucket1.png");
				public static readonly Texture2D ImageZombieArmor_Bucket2         = Load<Texture2D>("uid://bfm0gsckssceo"); // Load<Texture2D>("res://art/MainGame/Zombie/Zombie_bucket2.png");
				public static readonly Texture2D ImageZombieArmor_Bucket3         = Load<Texture2D>("uid://7poa7xmnf1th");  // Load<Texture2D>("res://art/MainGame/Zombie/Zombie_bucket3.png");

				public static readonly Texture2D ImageZombieArmor_ScreenDoor1     = Load<Texture2D>("uid://4fql6ea70hn");   // Load<Texture2D>("res://art/MainGame/Zombie/Zombie_screendoor1.png");
				public static readonly Texture2D ImageZombieArmor_ScreenDoor2     = Load<Texture2D>("uid://bbpc132w5u5he"); // Load<Texture2D>("res://art/MainGame/Zombie/Zombie_screendoor2.png");
				public static readonly Texture2D ImageZombieArmor_ScreenDoor3     = Load<Texture2D>("uid://c1vhpkckajf1x"); // Load<Texture2D>("res://art/MainGame/Zombie/Zombie_screendoor3.png");

				public static readonly Texture2D ImageZombieArmor_Paper1          = Load<Texture2D>("uid://t7r0s6088fqf");  // Load<Texture2D>("res://art/MainGame/Zombie/Zombie_paper_paper1.png");
				public static readonly Texture2D ImageZombieArmor_Paper2          = Load<Texture2D>("uid://pluvay7inrtt");  // Load<Texture2D>("res://art/MainGame/Zombie/Zombie_paper_paper2.png");
				public static readonly Texture2D ImageZombieArmor_Paper3          = Load<Texture2D>("uid://b2pgxhb7thg7");  // Load<Texture2D>("res://art/MainGame/Zombie/Zombie_paper_paper3.png");

				public static readonly Texture2D ImageZombieArmor_Ladder1_Damage0 = Load<Texture2D>("uid://bnorau7u4t3s7"); // Load<Texture2D>("res://art/MainGame/Zombie/Zombie_ladder1_damage1.png");
				public static readonly Texture2D ImageZombieArmor_Ladder1_Damage1 = Load<Texture2D>("uid://cxjnck4ee4xjb"); // Load<Texture2D>("res://art/MainGame/Zombie/Zombie_ladder1_damage2.png");
				public static readonly Texture2D ImageZombieArmor_Ladder1_Damage2 = Load<Texture2D>("uid://0dhkf7poy6us");  // Load<Texture2D>("res://art/MainGame/Zombie/Zombie_ladder1_damage3.png");

				public static readonly Texture2D ImageZombieArmor_FootballHelmet1 = Load<Texture2D>("uid://fnn30aw05u1b"); // Load<Texture2D>("res://art/MainGame/Zombie/FootballZombie/Zombie_football_helmet.png");
				public static readonly Texture2D ImageZombieArmor_FootballHelmet2 = Load<Texture2D>("uid://b44xptjtndskg"); // Load<Texture2D>("res://art/MainGame/Zombie/FootballZombie/Zombie_football_helmet2.png");
				public static readonly Texture2D ImageZombieArmor_FootballHelmet3 = Load<Texture2D>("uid://btanrekqiy2qd");  // Load<Texture2D>("res://art/MainGame/Zombie/FootballZombie/Zombie_football_helmet3.png");

				public static readonly Texture2D ImageZombieArmor_DiggerHardhat1  = Load<Texture2D>("uid://djft64hv1ltub"); // Load<Texture2D>("res://art/MainGame/Zombie/DiggerZombie/Zombie_digger_hardhat.png");
				public static readonly Texture2D ImageZombieArmor_DiggerHardhat2  = Load<Texture2D>("uid://gsbaabx3jiuq");  // Load<Texture2D>("res://art/MainGame/Zombie/DiggerZombie/Zombie_digger_hardhat2.png");
				public static readonly Texture2D ImageZombieArmor_DiggerHardhat3  = Load<Texture2D>("uid://ddj7h0vk1n84f"); // Load<Texture2D>("res://art/MainGame/Zombie/DiggerZombie/Zombie_digger_hardhat3.png");
			}
		}

		public static class BackGrounds
		{
			public static readonly Texture2D ImageBg_DayLawn   = Load<Texture2D>("uid://ckobsbgvjkrpo"); // Load<Texture2D>("res://art/MainGame/background1.jpg");
			public static readonly Texture2D ImageBg_NightLawn = Load<Texture2D>("uid://bbkefpgl0sfwq"); // Load<Texture2D>("res://art/MainGame/background2.jpg");
			public static readonly Texture2D ImageBg_PoolDay   = Load<Texture2D>("uid://cmq3dnt5rc8c0"); // Load<Texture2D>("res://art/MainGame/background3.jpg");
		}
	}

}
