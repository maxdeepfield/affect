using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Simplified enemy controller with clear state management.
/// Modern architecture: State machine pattern with focused responsibilities.
/// </summary>
public class SimpleEnemyController : MonoBehaviour
{
    // State enum
    private enum EnemyState { Idle, Patrol, Chase, Attack, Retreat }

    // References
    [SerializeField] private Transform _player;
    [SerializeField] private Transform[] _patrolPoints;
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private Transform _firePoint;

    // Configuration
    [SerializeField] private float _detectionRadius = 14f;
    [SerializeField] private float _attackRange = 9f;
    [SerializeField] private float _shootCooldown = 0.9f;
    [SerializeField] private float _lowHealthThreshold = 0.35f;
    [SerializeField] private float _projectileSpeed = 22f;

    // State
    private NavMeshAgent _agent;
    private Health _health;
    private EnemyState _currentState = EnemyState.Idle;
    private int _patrolIndex = 0;
    private float _lastShotTime = -999f;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _health = GetComponent<Health>();
    }

    private void Update()
    {
        if (_agent == null || (_health != null && _health.IsDead))
            return;

        FindPlayer();
        float distToPlayer = _player != null ? Vector3.Distance(transform.position, _player.position) : Mathf.Infinity;
        bool canSeePlayer = CanSeeTarget();

        UpdateState(distToPlayer, canSeePlayer);
        ExecuteState(distToPlayer);
    }

    private void UpdateState(float distToPlayer, bool canSeePlayer)
    {
        bool isLowHealth = _health != null && (_health.CurrentHealth / _health.MaxHealth) < _lowHealthThreshold;

        if (isLowHealth)
        {
            _currentState = EnemyState.Retreat;
            return;
        }

        switch (_currentState)
        {
            case EnemyState.Idle:
            case EnemyState.Patrol:
                if (canSeePlayer || distToPlayer < _detectionRadius)
                    _currentState = distToPlayer < _attackRange ? EnemyState.Attack : EnemyState.Chase;
                else
                    _currentState = _patrolPoints.Length > 0 ? EnemyState.Patrol : EnemyState.Idle;
                break;

            case EnemyState.Chase:
                if (!canSeePlayer && distToPlayer > _detectionRadius * 1.2f)
                    _currentState = _patrolPoints.Length > 0 ? EnemyState.Patrol : EnemyState.Idle;
                else if (canSeePlayer && distToPlayer < _attackRange)
                    _currentState = EnemyState.Attack;
                break;

            case EnemyState.Attack:
                if (!canSeePlayer || distToPlayer > _attackRange * 1.2f)
                    _currentState = EnemyState.Chase;
                break;

            case EnemyState.Retreat:
                if (!isLowHealth)
                    _currentState = canSeePlayer ? EnemyState.Chase : EnemyState.Patrol;
                break;
        }
    }

    private void ExecuteState(float distToPlayer)
    {
        switch (_currentState)
        {
            case EnemyState.Idle:
                HoldPosition();
                break;
            case EnemyState.Patrol:
                Patrol();
                break;
            case EnemyState.Chase:
                Chase();
                break;
            case EnemyState.Attack:
                TryShoot(distToPlayer);
                break;
            case EnemyState.Retreat:
                Retreat();
                break;
        }
    }

    private void HoldPosition()
    {
        _agent.speed = 0f;
        _agent.SetDestination(transform.position);
    }

    private void Patrol()
    {
        _agent.speed = 2.5f;

        if (_patrolPoints.Length == 0) return;

        Transform target = _patrolPoints[_patrolIndex];
        _agent.SetDestination(target.position);

        if (!_agent.pathPending && _agent.remainingDistance < 0.5f)
            _patrolIndex = (_patrolIndex + 1) % _patrolPoints.Length;
    }

    private void Chase()
    {
        if (_player == null) return;

        _agent.speed = 4f;
        _agent.stoppingDistance = 0.5f;
        _agent.SetDestination(_player.position);
    }

    private void TryShoot(float distToPlayer)
    {
        if (_projectilePrefab == null || _firePoint == null || _player == null) return;
        if (!CanSeeTarget() || distToPlayer > _attackRange + 0.5f) return;
        if (Time.time < _lastShotTime + _shootCooldown) return;

        _lastShotTime = Time.time;

        Vector3 dir = (_player.position - _firePoint.position).normalized;
        GameObject projectile = Instantiate(_projectilePrefab, _firePoint.position, Quaternion.LookRotation(dir));
        
        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
            rb.linearVelocity = dir * _projectileSpeed;
    }

    private void Retreat()
    {
        if (_player == null) { Patrol(); return; }

        _agent.speed = 5f;
        Vector3 away = (transform.position - _player.position).normalized;
        Vector3 retreatPos = transform.position + away * 10f;

        if (NavMesh.SamplePosition(retreatPos, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            _agent.SetDestination(hit.position);
    }

    private bool CanSeeTarget()
    {
        if (_player == null) return false;

        Vector3 toPlayer = _player.position - transform.position;
        float distToPlayer = toPlayer.magnitude;

        if (distToPlayer < 0.01f || distToPlayer > _detectionRadius * 1.3f) return false;
        if (Vector3.Angle(transform.forward, toPlayer) > 70f) return false;

        return !Physics.Raycast(transform.position + Vector3.up, toPlayer.normalized, distToPlayer + 0.5f);
    }

    private void FindPlayer()
    {
        if (_player != null) return;

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
            _player = playerObj.transform;
    }
}
