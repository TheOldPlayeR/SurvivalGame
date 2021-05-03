using Godot;
using System;

public struct TerrainData
{
    public float _value;
    public Color _colour;

    // Future things like what material it actually is, probs make colour generate based off id (maybe add randomness later)
    // public int _id

    public TerrainData(float i_value, Color i_colour)
    {
        _value = i_value;
        _colour = i_colour;
    }
}
