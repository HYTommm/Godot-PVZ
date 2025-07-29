using Godot;
using System;

public partial class Global : Node
{
    public static Global Instance => _instance;

    private static Global _instance;

    public override void _Ready()
    {
        _instance = this;
    }

}
