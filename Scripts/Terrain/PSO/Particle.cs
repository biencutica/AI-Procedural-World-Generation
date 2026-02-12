using UnityEngine;

public class Particle
{
    private Vector2 _position;
    private Vector2 _velocity;
    private Vector2 _local_bestPosition;
    private float _local_bestFitness;

    public Vector2 Position
    {
        get => _position;
        set => _position = value;
    }

    public Vector2 Velocity
    {
        get => _velocity;
        set => _velocity = value;
    }

    public Vector2 LocalBestPosition
    {
        get => _local_bestPosition;
        set => _local_bestPosition = value;
    }

    public float LocalBestFitness
    {
        get => _local_bestFitness;
        set => _local_bestFitness = value;
    }

    public Particle(Vector2 startPosition)
    {
        _position = startPosition;
        _velocity = Vector2.zero;
        _local_bestPosition = startPosition;
        _local_bestFitness = float.MinValue;
    }
}
 
