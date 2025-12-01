using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Main orchestrator for the Spider IK Walker Animation System.
/// Automatically walks when the spider is moved (dragged in editor or moved by physics).
/// Works in both editor and play mode.
/// </summary>
public class SpiderIKSystem : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private IKConfiguration _config;

    [Header("Legs")]
    [SerializeField] private LegData[] _legs;

    [Header("References")]
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private Collider _collider;

    // Module references
    private List<ISpiderModule> _modules = new List<ISpiderModule>();
    private LegSolver _legSolver;
    private GaitController _gaitController;
    private TerrainAdapter _terrainAdapter;
    private BodyStabilizer _bodyStabilizer;
    private StepAnimator _stepAnimator;

    // Velocity tracking
    private Vector3 _lastPosition;
    private Vector3 _smoothedVelocity;
    private int _activeStepGroup = 0;
    private float _editorDeltaTime = 0.016f; // ~60fps

    public IKConfiguration Config
    {
        get => _config;
        set
        {
            _config = value ?? new IKConfiguration();
            ApplyConfigurationToModules();
        }
    }

    public LegData[] Legs => _legs;
    public Vector3 BodyVelocity => _smoothedVelocity;

    private void OnEnable()
    {
        if (_config == null) _config = new IKConfiguration();
        _rigidbody = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
        _lastPosition = transform.position;
        Initialize();
    }

    private void Update()
    {
        // Works in both editor and play mode
        float deltaTime = Time.deltaTime;
        if (deltaTime <= 0) deltaTime = _editorDeltaTime;

        // Track velocity from position changes (works when dragging in editor)
        UpdateVelocity(deltaTime);

        // Update gait - trigger leg stepping based on velocity
        UpdateGait(deltaTime);

        // Update step animation
        if (_stepAnimator != null && _stepAnimator.IsEnabled && _legs != null)
        {
            for (int i = 0; i < _legs.Length; i++)
            {
                if (_legs[i] != null && _legs[i].isStepping)
                {
                    _stepAnimator.UpdateStep(_legs[i], deltaTime);
                    if (_legs[i].stepProgress >= 1f)
                    {
                        // Notify gait controller that step is complete
                        if (_gaitController != null)
                        {
                            _gaitController.CompleteStep(_legs[i]);
                        }
                        else
                        {
                            _legs[i].isStepping = false;
                            _legs[i].plantedPos = _legs[i].currentTarget;
                        }
                    }
                }
            }
        }

        // Update terrain adaptation
        if (_terrainAdapter != null && _terrainAdapter.IsEnabled && _legs != null)
        {
            for (int i = 0; i < _legs.Length; i++)
            {
                if (_legs[i] != null && _legs[i].foot != null)
                {
                    Vector3 footPos = _terrainAdapter.FindSurfacePosition(_legs[i].foot.position, Vector3.down);
                    if (!_legs[i].isStepping)
                    {
                        _legs[i].plantedPos = footPos;
                    }
                }
            }
        }

        // Update other modules
        foreach (var module in _modules)
        {
            if (module != null && module.IsEnabled && 
                !(module is GaitController) && 
                !(module is StepAnimator) && 
                !(module is TerrainAdapter))
            {
                module.OnUpdate(deltaTime);
            }
        }

        // Solve IK after all updates
        if (_legSolver != null && _legSolver.IsEnabled && _legs != null)
        {
            Vector3 bodyCenter = transform.position;
            foreach (var leg in _legs)
            {
                if (leg != null)
                {
                    Vector3 target = leg.isStepping ? leg.currentTarget : leg.plantedPos;
                    _legSolver.SolveIK(leg, target, bodyCenter);
                }
            }
        }
    }

    private void FixedUpdate()
    {
        foreach (var module in _modules)
        {
            if (module != null && module.IsEnabled)
            {
                module.OnFixedUpdate(Time.fixedDeltaTime);
            }
        }
    }

    private void UpdateVelocity(float deltaTime)
    {
        if (deltaTime <= 0f) return;
        Vector3 currentPos = transform.position;
        Vector3 velocity = (currentPos - _lastPosition) / deltaTime;
        
        // Use faster smoothing for better responsiveness
        // Especially important for editor dragging
        _smoothedVelocity = Vector3.Lerp(_smoothedVelocity, velocity, Mathf.Min(1f, deltaTime * 10f));
        _lastPosition = currentPos;

        // Update planted positions to track current foot positions when not stepping
        // This ensures legs know where they are when the body moves
        if (_legs != null)
        {
            foreach (var leg in _legs)
            {
                if (leg != null && !leg.isStepping && leg.foot != null)
                {
                    leg.plantedPos = leg.foot.position;
                }
            }
        }
    }

    private void UpdateGait(float deltaTime)
    {
        if (_legs == null || _legs.Length == 0 || _gaitController == null) return;

        // Use GaitController to manage stepping
        _gaitController.UpdateGait(_legs, _smoothedVelocity);
    }

    public void Initialize()
    {
        DiscoverModules();
        foreach (var module in _modules)
        {
            if (module != null) module.Initialize(this);
        }
        RebuildLegData();
        ApplyConfigurationToModules();
    }

    private void DiscoverModules()
    {
        _modules.Clear();
        var allModules = GetComponents<ISpiderModule>();
        foreach (var module in allModules)
        {
            _modules.Add(module);
            if (module is LegSolver ls) _legSolver = ls;
            if (module is GaitController gc) _gaitController = gc;
            if (module is TerrainAdapter ta) _terrainAdapter = ta;
            if (module is BodyStabilizer bs) _bodyStabilizer = bs;
            if (module is StepAnimator sa) _stepAnimator = sa;
        }
    }

    public void RebuildLegData()
    {
        if (_config == null) _config = new IKConfiguration();
        int legCount = _config.legCount;
        _legs = new LegData[legCount];

        Transform legsContainer = transform.Find("Legs");
        if (legsContainer == null)
        {
            for (int i = 0; i < legCount; i++)
            {
                _legs[i] = new LegData { legIndex = i, diagonalGroup = i % 2 };
            }
            return;
        }

        for (int i = 0; i < legCount && i < legsContainer.childCount; i++)
        {
            Transform legRoot = legsContainer.GetChild(i);
            var legData = new LegData { root = legRoot, legIndex = i, diagonalGroup = i % 2 };

            if (legRoot != null)
            {
                legData.hip = FindChild(legRoot, "Hip");
                if (legData.hip != null)
                {
                    legData.knee = FindChild(legData.hip, "Knee");
                    if (legData.knee != null)
                    {
                        legData.foot = FindChild(legData.knee, "Foot");
                    }
                    else
                    {
                        legData.foot = FindChild(legData.hip, "Foot");
                    }
                }
            }

            legData.InitializeSegments(_config.boneCount, _config.segmentHealth);
            if (legData.foot != null)
            {
                legData.restTarget = transform.InverseTransformPoint(legData.foot.position);
                legData.currentTarget = legData.foot.position;
                legData.plantedPos = legData.foot.position;
            }

            _legs[i] = legData;
        }
    }

    private Transform FindChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            var found = FindChild(child, name);
            if (found != null) return found;
        }
        return null;
    }

    private void ApplyConfigurationToModules()
    {
        foreach (var module in _modules)
        {
            if (module is IConfigurable configurable)
            {
                configurable.ApplyConfiguration(_config);
            }
        }
    }

    public void ApplyDamageToSegment(int legIndex, int segmentIndex, float damage)
    {
        var damageHandler = GetComponent<LegDamageHandler>();
        if (damageHandler != null)
        {
            damageHandler.ApplyDamage(legIndex, segmentIndex, damage);
        }
    }

    public void ResetAllLegs()
    {
        if (_legs != null)
        {
            foreach (var leg in _legs)
            {
                if (leg != null) leg.Reset();
            }
        }
    }

    public void SetConfiguration(IKConfiguration config)
    {
        Config = config;
    }

    public float CalculateStrideLength(Vector3 velocity)
    {
        if (_config == null) return 0.3f;
        float baseStride = _config.strideForward;
        float velocityMagnitude = velocity.magnitude;
        float velocityScale = _config.strideVelocityScale;
        return baseStride + velocityMagnitude * velocityScale;
    }

    public Vector3 CalculateStrideDirection(Vector3 velocity)
    {
        if (velocity.sqrMagnitude < 0.001f)
        {
            return transform.forward;
        }
        return velocity.normalized;
    }
}

public interface IConfigurable
{
    void ApplyConfiguration(IKConfiguration config);
}
