using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Godot;

public abstract partial class HealthEntity : Entity
{
    /// <summary>生命值</summary>
    public int HP;
    /// <summary>最大生命值</summary>
    public int MaxHP;

    public abstract int Hurt(int damage);
}
