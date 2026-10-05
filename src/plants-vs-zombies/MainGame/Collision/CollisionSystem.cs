using System;
using System.Collections.Generic;

namespace PlantsVsZombies.Collision;

/// <summary>
/// 一次判定的命中规则：给源箱子、一个候选、以及候选的箱子，说算不算命中。
///
/// **特判全挂在这里**——交叠要超过多少才算（阈值）、对手是谁（这类僵尸免疫）、
/// 当前是什么状态（跳跃中不算），都在这一个谓词里表达。不必为了换个判据去动
/// 实体类的继承关系，也不必让实体实现一堆虚方法。
///
/// **分配陷阱**：捕获了局部变量的 lambda，每次求值都会新建一个对象。判定跑在
/// 每物理帧的热循环里，所以谓词要**建一次、存在实体字段上**（在 `_Ready` 里赋）。
/// 只捕获 this（读自己的字段）或不捕获的 `static` 写法，编译期就会被缓存成单例，
/// 那种是安全的，可以就地写。
/// </summary>
public delegate bool HitPredicate<in T>(IHitBox source, T target, IHitBox targetBox);

/// <summary>
/// 全局的碰撞调度。
///
/// 它存在的理由是两件引擎给不了的事：判定要**每帧重算**（而不是"区域进入"那种
/// 状态转移事件），顺序要**按数组下标**（而不是 broadphase 给什么顺序算什么顺序）。
///
/// 顺序这条不是可有可无的约定：原版所有判定都是对大数组从下标 0 开始一个个判、
/// 第一个满足条件的即采用，所以"谁优先"等于"谁的下标小"，跟几何位置无关。
/// 自算这条路天然满足它——候选是调用方自己的数组，遍历顺序就是数组顺序。
///
/// 候选由调用方传进来，而不是内部持有一张世界表：判定是分层的（僵尸查植物、
/// 植物查僵尸、子弹查僵尸），"重叠"有方向，不是全表对全表。
/// </summary>
public static class CollisionSystem
{
    /// <summary>
    /// 按候选的**下标顺序**挑第一个接触的，返回它的下标；都不命中返回 -1。
    /// 接触的定义是交叠 ≥ 0；要别的阈值就用下面那个带谓词的重载。
    /// </summary>
    public static int PickFirst<T>(IReadOnlyList<T> candidates, IHitBox source)
        where T : IHittable
    {
        // static 无捕获，编译期缓存成单例，不会每帧分配
        return PickFirst(candidates, source, static (s, _, targetBox) => HitMath.OverlapX(s, targetBox) >= 0);
    }

    /// <summary>
    /// 同上，命中规则由调用方给。**特判走这个重载**。
    /// </summary>
    public static int PickFirst<T>(IReadOnlyList<T> candidates, IHitBox source, HitPredicate<T> hit)
        where T : IHittable
    {
        for (int i = 0; i < candidates.Count; i++)
        {
            T item = candidates[i];

            // 先问有效性，再碰箱子：候选可能是延迟回收窗口里**已经释放的节点**，
            // 那个状态下读箱子的节点属性会抛 ObjectDisposedException
            if (item is null || !item.IsValid)
            {
                continue;
            }

            IHitBox target = item.HitBox;
            if (target is not { Monitorable: true })
            {
                continue;
            }

            if (hit(source, item, target))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// 对所有接触的候选各调一次（爆炸那种"全都要"）。顺序同样是下标序——
    /// 多目标结算的先后也由它决定。
    /// </summary>
    public static void ForEachHit<T>(IReadOnlyList<T> candidates, IHitBox source, Action<T> onHit)
        where T : IHittable
    {
        ForEachHit(candidates, source, static (s, _, targetBox) => HitMath.OverlapX(s, targetBox) >= 0, onHit);
    }

    /// <summary>
    /// 同上，命中规则由调用方给。
    ///
    /// onHit 由调用方持有、别在循环里新建：捕获局部变量的委托每次创建都会分配。
    /// </summary>
    public static void ForEachHit<T>(IReadOnlyList<T> candidates, IHitBox source, HitPredicate<T> hit, Action<T> onHit)
        where T : IHittable
    {
        foreach (T item in candidates)
        {
            if (item is null || !item.IsValid)
            {
                continue;
            }

            IHitBox target = item.HitBox;
            if (target is not { Monitorable: true })
            {
                continue;
            }

            if (hit(source, item, target))
            {
                onHit(item);
            }
        }
    }
}
