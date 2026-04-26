using Godot;

[GlobalClass]
public partial class Abc : Resource
{
    [Export] public int A { get; set; } = 1;
}