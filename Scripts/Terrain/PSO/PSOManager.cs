using System.Collections.Generic;
using UnityEngine;

public class PSOManager
{
    //hyperparameters - user-defined settings
    private int _numParticles = 100;
    private int _maxIterations = 50;
    private float _inertiaStart = 0.9f, _inertiaEnd = 0.4f;
    private float _alpha = 1.4f, _beta = 1.4f, _gamma = 1.0f;

    public int NumParticles
    {
        get => _numParticles;
        set => _numParticles = Mathf.Max(1, value); // avoid zero
    }

    public int MaxIterations
    {
        get => _maxIterations;
        set => _maxIterations = Mathf.Max(1, value);
    }

    public float InertiaStart
    {
        get => _inertiaStart;
        set => _inertiaStart = Mathf.Clamp01(value);
    }

    public float InertiaEnd
    {
        get => _inertiaEnd;
        set => _inertiaEnd = Mathf.Clamp01(value);
    }

    public float Alpha
    {
        get => _alpha;
        set => _alpha = Mathf.Max(0f, value);
    }

    public float Beta
    {
        get => _beta;
        set => _beta = Mathf.Max(0f, value);
    }

    public float Gamma
    {
        get => _gamma;
        set => _gamma = Mathf.Max(0f, value);
    }

    private List<Particle> particles;
    private Vector2 _globalBestPosition;
    private float _globalBestFitness;
    public bool UseExternalLeader { get; set; } = false;

    public Vector2 LeaderPosition { get; set; } = Vector2.zero;
    private Vector2 boundsMin, boundsMax; //search space 

    private System.Func<Vector2, float> EvaluateFitness;

    public PSOManager(Vector2 minBounds, Vector2 maxBounds, System.Func<Vector2, float> fitnessFunction)
    {
        // store bounds and fitness delegate
        boundsMin = minBounds;
        boundsMax = maxBounds;
        EvaluateFitness = fitnessFunction;
    }

    public List<Vector2> Run()
    {
        InitializeParticles(); //randomly scatter particles in the search space

        // iterate and update particle positions
        for(int iter = 0; iter < _maxIterations; iter++)
        {
            float inertia = Mathf.Lerp(_inertiaStart, _inertiaEnd, iter / (float)_maxIterations);

            foreach (var p in particles)
            {
                float fitness = EvaluateFitness(p.Position); //how good the current personal best is

                if(fitness > p.LocalBestFitness)
                {
                    p.LocalBestFitness = fitness;
                    p.LocalBestPosition = p.Position;
                }

                Vector2 r1 = Random.insideUnitCircle;
                Vector2 r2 = Random.insideUnitCircle;
                Vector2 r3 = Random.insideUnitCircle;

                Vector2 cognitive = _alpha * r1 * (p.LocalBestPosition - p.Position).normalized;
                Vector2 social = _beta * r2 * (_globalBestPosition - p.Position).normalized;
                Vector2 leader = _gamma * r3 * (LeaderPosition - p.Position).normalized; //force convergence toward a dominant direction

                p.Velocity = inertia * p.Velocity + cognitive + social + leader;
                p.Position += p.Velocity; //particle moves to new location
                p.Position = ClampToBounds(p.Position);
            }

            UpdateGlobalBest();
        }

        List<Vector2> results = new();
        foreach (var p in particles)
            results.Add(p.LocalBestPosition);

        //return final best positions
        return results;
    }

    private void InitializeParticles()
    {
        particles = new List<Particle>();
        _globalBestFitness = float.MinValue;

        if (_numParticles <= 0)
        {
            Debug.LogError("PSO Initialization: numParticles must be greater than 0.");
            return;
        }

        for (int i = 0; i < _numParticles; i++)
        {
            float x = Random.Range(boundsMin.x + 1f, boundsMax.x - 2f); //241-2=239 (in case x or z=239+1=240 last index of 241x241)
            float z = Random.Range(boundsMin.y + 1f, boundsMax.y - 2f);
            Vector2 startPos = new(x, z);

            Particle p = new(startPos);
            particles.Add(p);
        }

        if (particles.Count == 0)
        {
            Debug.LogError("PSO Initialization failed: No particles created. Check bounds.");
            return;
        }

        if (!UseExternalLeader)
            LeaderPosition = particles[0].Position;

    }

    private void UpdateGlobalBest()
    {
        // find best particle and update global/leader position
        Particle best = particles[0];

        foreach (var p in particles)
        {
            if (p.LocalBestFitness > best.LocalBestFitness){
                best = p;
            }
        }

        if(best.LocalBestFitness > _globalBestFitness)
        {
            _globalBestFitness = best.LocalBestFitness;
            _globalBestPosition = best.LocalBestPosition;
        }

        if (!UseExternalLeader)
            LeaderPosition = best.LocalBestPosition;

    }

    private Vector2 ClampToBounds(Vector2 pos)
    {
        // ensure particle stays inside bounds
        float clampedX = Mathf.Clamp(pos.x, boundsMin.x, boundsMax.x);
        float clampedY = Mathf.Clamp(pos.y, boundsMin.y, boundsMax.y);
        return new Vector2(clampedX, clampedY);
    }

}