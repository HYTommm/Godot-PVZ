using Godot;
using static ResourceDB.Sounds;

/// <summary>
/// 命中表现：溅射粒子与溅射音。
///
/// 子弹是数据集合，命中不再为每颗子弹建节点——这里也不能：
/// 初始化时把两个池一次建好，之后每次命中只是从池里取一个复用，**运行时零节点创建**。
///
/// 池子有上限，取到哪算哪、满了就把这一下丢掉。超频豌豆 1000 发/秒时，
/// 听感上本来就是"一片响"，丢几个分辨不出来；反过来若每次命中都建节点，
/// 帧率会被拖垮，还留下一堆孤儿节点。
/// </summary>
public class HitEffectSystem
{
	/// <summary>同时能响的溅射音个数。全占满时新的那发就不出声了，不会去掐已经在响的</summary>
	private const int SoundVoices = 16;

	/// <summary>同时能响的开火音个数。理由同上</summary>
	private const int ShootVoices = 16;

	/// <summary>每种弹的溅射粒子池大小</summary>
	private const int ParticlesPerKind = 32;

	/// <summary>
	/// 粒子总开关。关掉后**连池子都不建**——每个 GpuParticles2D 各自带一份 GPU 缓冲
	/// （粒子数、材质、曲线），就算一次都不发射也占着显存和每帧的更新成本，
	/// 所以"不发射"和"不建"是两回事，这里要的是后者。音效不受影响。
	/// 用来量粒子本身占多少开销，量完改回 true。
	/// </summary>
	public static bool BEnableParticles = true;

	/// <summary>溅射点相对子弹原点的偏移。沿用原 Splats 节点在子弹里的位置</summary>
	private static readonly Vector2 SplatOffset = new(33, 14);

	private const string PeaBulletScenePath = "res://MainGame/Plants/Bullet/Pea.tscn";
	private const string SnowPeaBulletScenePath = "res://MainGame/Plants/Bullet/SnowPea.tscn";

	private AudioStreamPlayer[] _sounds;
	private AudioStreamPlayer[] _shootSounds;
	private GpuParticles2D[] _peaSplats;
	private GpuParticles2D[] _snowPeaSplats;

	private bool _initialized;

	public void Initialize(Node2D host)
	{
		if (_initialized)
		{
			return;
		}

		_sounds = new AudioStreamPlayer[SoundVoices];
		for (int i = 0; i < _sounds.Length; i++)
		{
			AudioStreamPlayer voice = new();
			host.AddChild(voice);
			_sounds[i] = voice;
		}

		_shootSounds = new AudioStreamPlayer[ShootVoices];
		for (int i = 0; i < _shootSounds.Length; i++)
		{
			AudioStreamPlayer voice = new();
			host.AddChild(voice);
			_shootSounds[i] = voice;
		}

		if (BEnableParticles)
		{
			_peaSplats = BuildParticlePool(host, PeaBulletScenePath, ParticlesPerKind);
			_snowPeaSplats = BuildParticlePool(host, SnowPeaBulletScenePath, ParticlesPerKind);
		}
		else
		{
			// 空池：FireParticle 遍历长度为 0 的数组会直接返回，等于不表现
			_peaSplats = System.Array.Empty<GpuParticles2D>();
			_snowPeaSplats = System.Array.Empty<GpuParticles2D>();
			GD.Print("[HitEffectSystem] 粒子已禁用（BEnableParticles = false），只留音效");
		}

		_initialized = true;
	}

	public void Dispose()
	{
		// 池里的节点都挂在 host 上了，随 host 一起释放，这里只是断开引用
		_sounds = null;
		_shootSounds = null;
		_peaSplats = null;
		_snowPeaSplats = null;
		_initialized = false;
	}

	/// <summary>
	/// 植物开火。也走池子——每株植物自带一个播放器的话，超频时同一个播放器
	/// 会被自己的下一发反复打断重播，听感是"哒哒哒"而不是连续的射击声
	/// </summary>
	public void PlayShoot()
	{
		if (!_initialized)
		{
			return;
		}

		AudioStreamPlayer voice = TakeFreeVoice(_shootSounds);
		if (voice == null)
		{
			return; // 全在响，这一发不出声
		}

		voice.Stream = Sound_Throw;
		voice.Play();
	}

	/// <summary>
	/// 取一路空闲的播放器；全都在响就返回 null，这一下不播。
	///
	/// 刻意不用环形取用：轮到的可能正在播，Play() 会把它掐掉从头再来，
	/// 听感就是"哒哒哒"的断续。宁可少播几发，也不去打断已经在响的那发。
	/// 池子只有十几路、逐个查一遍比环形取用也贵不了多少
	/// </summary>
	private static AudioStreamPlayer TakeFreeVoice(AudioStreamPlayer[] pool)
	{
		for (int i = 0; i < pool.Length; i++)
		{
			if (!pool[i].Playing)
			{
				return pool[i];
			}
		}
		return null;
	}

	/// <summary>子弹命中：溅射粒子 + 溅射音。池子取完一圈也不会新建节点，只是这一下不表现</summary>
	public void PlaySplat(Vector2 bulletPosition, BulletKind kind)
	{
		if (!_initialized)
		{
			return;
		}

		Vector2 splatPosition = bulletPosition + SplatOffset;

		if (kind == BulletKind.SnowPea)
		{
			FireParticle(_snowPeaSplats, splatPosition);
		}
		else
		{
			FireParticle(_peaSplats, splatPosition);
		}

		PlaySplatSound();
	}

	/// <summary>取一个还没在放的粒子来重播；全在放就跳过。和音效一样：不打断在放的</summary>
	private static void FireParticle(GpuParticles2D[] pool, Vector2 position)
	{
		for (int i = 0; i < pool.Length; i++)
		{
			GpuParticles2D particles = pool[i];
			if (particles.Emitting)
			{
				continue; // 还在放自己的，别把它 Restart 掉
			}

			particles.GlobalPosition = position;
			particles.Restart();
			return;
		}
		// 池子全在放：这一下不表现。模板没加载出来时 pool 是空的，同样走这里
	}

	private void PlaySplatSound()
	{
		AudioStreamPlayer voice = TakeFreeVoice(_sounds);
		if (voice == null)
		{
			return; // 全在响，这一下不出声
		}

		voice.Stream = (GD.Randi() % 3) switch
		{
			0 => Sound_Splat,
			1 => Sound_Splat2,
			_ => Sound_Splat3,
		};
		voice.PitchScale = (float)GD.RandRange(1.0, 1.5);
		voice.Play();
	}

	/// <summary>
	/// 从子弹场景里把溅射粒子节点摘出来，复制成一个池。
	///
	/// 借现成的场景而不是另建一个 .tscn：粒子的材质、曲线、生命周期全配在里面，
	/// 复制时连子资源一起带过来，不用重配一遍。
	/// </summary>
	private static GpuParticles2D[] BuildParticlePool(Node2D host, string bulletScenePath, int count)
	{
		if (GD.Load<PackedScene>(bulletScenePath)?.Instantiate() is not Node2D template)
		{
			GD.PrintErr($"[HitEffectSystem] 粒子模板加载失败：{bulletScenePath}");
			return System.Array.Empty<GpuParticles2D>();
		}

		const string nodeName = "Splats";
		if (template.GetNodeOrNull<GpuParticles2D>(nodeName) is not GpuParticles2D splats)
		{
			GD.PrintErr($"[HitEffectSystem] {bulletScenePath} 里没有 {nodeName} 节点");
			template.QueueFree();
			return System.Array.Empty<GpuParticles2D>();
		}

		// 把粒子摘出来，模板树剩下的部分整个丢掉——池里只要那一个节点
		template.RemoveChild(splats);
		template.QueueFree();

		GpuParticles2D[] pool = new GpuParticles2D[count];
		for (int i = 0; i < count; i++)
		{
			GpuParticles2D instance = i == 0 ? splats : (GpuParticles2D)splats.Duplicate();
			instance.Emitting = false;
			host.AddChild(instance);
			pool[i] = instance;
		}

		return pool;
	}
}
