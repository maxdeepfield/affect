using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Lightweight state machine for spider enemies. Handles idle/patrol, chasing the player,
/// shooting when in range, and backing off to cover when health is low.
/// Attach to the spider root that also has a NavMeshAgent (and optionally AbsoluteSpiderFreakout).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshAgent))]
public class SpiderEnemyController : MonoBehaviour
{
    private enum EnemyState
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        Retreat
    }

    [Header("Targeting")]
    [SerializeField] private Transform player;
    [SerializeField] private float detectionRadius = 14f;
    [SerializeField] private float loseSightRadius = 18f;
    [SerializeField, Range(0f, 180f)] private float fieldOfView = 140f;
    [SerializeField] private float eyeHeight = 1f;
    [SerializeField] private LayerMask sightLayers = ~0;
    [SerializeField] private LayerMask obstacleLayers = ~0;

    [Header("Movement")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float patrolSpeed = 2.5f;
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private float retreatSpeed = 5f;
    [SerializeField] private float waypointTolerance = 0.5f;
    [SerializeField] private float patrolWaitTime = 1.2f;
    [SerializeField] private float wanderRadius = 6f;

    [Header("Combat")]
    [SerializeField] private float attackRange = 9f;
    [SerializeField] private float shootingCooldown = 0.9f;
    [SerializeField] private float projectileSpeed = 22f;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;

    [Header("Survival / Cover")]
    [SerializeField, Range(0f, 1f)] private float lowHealthFraction = 0.35f;
    [SerializeField] private float retreatDistance = 10f;
    [SerializeField] private float coverSearchRadius = 8f;
    [SerializeField] private float coverCheckHeight = 1.1f;
    [SerializeField, Range(4, 24)] private int coverSamples = 12;

    [Header("Debug")]
    [SerializeField] private bool drawDebug = true;

    private NavMeshAgent _agent;
    private Health _health;
    private EnemyState _state = EnemyState.Idle;
    private int _patrolIndex;
    private float _waitTimer;
    private float _lastShotTime = -999f;
    private Vector3 _spawnPoint;
    private bool _hasLineOfSight;

    private bool HasPatrolPoints => patrolPoints != null && patrolPoints.Length > 0;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _health = GetComponent<Health>();
        _spawnPoint = transform.position;
    }

    private void OnEnable()
    {
        _lastShotTime = -999f;
        if (_agent != null) _agent.isStopped = false;
    }

    private void Update()
    {
        if (_agent == null) return;
        if (_health != null && _health.IsDead)
        {
            _agent.isStopped = true;
            return;
        }

        EnsurePlayerReference();

        float playerDistance = player != null ? Vector3.Distance(transform.position, player.position) : Mathf.Infinity;
        _hasLineOfSight = player != null && HasLineOfSight();

        UpdateState(playerDistance);
        RunState(playerDistance);
    }

    public void SetPlayer(Transform target)
    {
        player = target;
    }

    private void UpdateState(float playerDistance)
    {
        bool lowHealth = IsLowHealth();

        if (lowHealth)
        {
            _state = EnemyState.Retreat;
            return;
        }

        switch (_state)
        {
            case EnemyState.Idle:
            case EnemyState.Patrol:
                if (_hasLineOfSight || playerDistance <= detectionRadius)
                {
                    _state = playerDistance <= attackRange ? EnemyState.Attack : EnemyState.Chase;
                }
                else if (!HasPatrolPoints)
                {
                    _state = EnemyState.Idle;
                }
                else
                {
                    _state = EnemyState.Patrol;
                }
                break;

            case EnemyState.Chase:
                if (!_hasLineOfSight && playerDistance > loseSightRadius)
                {
                    _state = HasPatrolPoints ? EnemyState.Patrol : EnemyState.Idle;
                }
                else if (_hasLineOfSight && playerDistance <= attackRange)
                {
                    _state = EnemyState.Attack;
                }
                break;

            case EnemyState.Attack:
                if (!_hasLineOfSight || playerDistance > attackRange * 1.2f)
                {
                    _state = EnemyState.Chase;
                }
                break;

            case EnemyState.Retreat:
                if (!lowHealth)
                {
                    if (_hasLineOfSight || playerDistance <= detectionRadius)
                        _state = EnemyState.Chase;
                    else
                        _state = HasPatrolPoints ? EnemyState.Patrol : EnemyState.Idle;
                }
                break;
        }
    }

    private void RunState(float playerDistance)
    {
        switch (_state)
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
                Attack(playerDistance);
                break;
            case EnemyState.Retreat:
                Retreat();
                break;
        }
    }

    private void HoldPosition()
    {
        _agent.stoppingDistance = 0f;
        _agent.speed = 0f;
        _agent.SetDestination(transform.position);
    }

    private void Patrol()
    {
        _agent.speed = patrolSpeed;
        _agent.stoppingDistance = 0f;

        if (HasPatrolPoints)
        {
            Transform target = patrolPoints[_patrolIndex];
            if (target == null)
                return;

            _agent.SetDestination(target.position);

            if (!_agent.pathPending && _agent.remainingDistance <= waypointTolerance)
            {
                _waitTimer += Time.deltaTime;
                if (_waitTimer >= patrolWaitTime)
                {
                    _patrolIndex = (_patrolIndex + 1) % patrolPoints.Length;
                    _waitTimer = 0f;
                }
            }
            else
            {
                _waitTimer = 0f;
            }
        }
        else
        {
            Wander();
        }
    }

    private void Wander()
    {
        if (_agent.pathPending || _agent.remainingDistance > waypointTolerance) return;

        Vector2 random = Random.insideUnitCircle * wanderRadius;
        Vector3 target = _spawnPoint + new Vector3(random.x, 0f, random.y);
        if (NavMesh.SamplePosition(target, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            _agent.SetDestination(hit.position);
        }
    }

    private void Chase()
    {
        if (player == null) return;

        _agent.speed = chaseSpeed;
        _agent.stoppingDistance = Mathf.Max(attackRange * 0.6f, 0.5f);
        _agent.SetDestination(player.position);
    }

    private void Attack(float playerDistance)
    {
        if (player == null) return;

        _agent.speed = chaseSpeed;
        _agent.stoppingDistance = Mathf.Max(attackRange * 0.8f, 0.5f);
        _agent.SetDestination(player.position);

        TryShoot(playerDistance);
    }

    private void TryShoot(float playerDistance)
    {
        if (projectilePrefab == null || firePoint == null || player == null) return;
        if (!_hasLineOfSight || playerDistance > attackRange + 0.5f) return;
        if (Time.time < _lastShotTime + shootingCooldown) return;

        _lastShotTime = Time.time;

        Vector3 toPlayer = (player.position - firePoint.position);
        Vector3 dir = toPlayer.normalized;
        Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);

        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, rot);
        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = dir * projectileSpeed;
        }

        BulletController bullet = projectile.GetComponent<BulletController>();
        if (bullet != null)
        {
            bullet.SetInitialVelocity(dir * projectileSpeed);
        }
    }

    private void Retreat()
    {
        if (player == null)
        {
            Patrol();
            return;
        }

        _agent.speed = retreatSpeed;
        _agent.stoppingDistance = 0f;

        if (TryFindCoverPosition(out Vector3 cover))
        {
            _agent.SetDestination(cover);
            return;
        }

        Vector3 away = (transform.position - player.position).normalized;
        Vector3 target = transform.position + away * retreatDistance;

        if (NavMesh.SamplePosition(target, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            _agent.SetDestination(hit.position);
        }
    }

    private bool TryFindCoverPosition(out Vector3 coverPosition)
    {
        coverPosition = Vector3.zero;
        if (player == null) return false;

        float bestScore = float.NegativeInfinity;
        Vector3 origin = transform.position + Vector3.up * coverCheckHeight;
        Vector3 awayFromPlayer = (transform.position - player.position).normalized;

        int samples = Mathf.Max(coverSamples, 4);
        float angleStep = 360f / samples;

        for (int i = 0; i < samples; i++)
        {
            float angle = angleStep * i;
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            Vector3 candidate = transform.position + dir.normalized * coverSearchRadius;

            if (!NavMesh.SamplePosition(candidate, out NavMeshHit navHit, 2f, NavMesh.AllAreas))
                continue;

            Vector3 eye = navHit.position + Vector3.up * coverCheckHeight;
            Vector3 toPlayer = player.position - eye;
            float playerDistance = toPlayer.magnitude;
            if (playerDistance < 0.01f) continue;

            bool blocked = Physics.Raycast(eye, toPlayer.normalized, out RaycastHit hit, playerDistance, obstacleLayers);
            if (!blocked || hit.transform == player)
                continue;

            float score = Vector3.Dot(dir.normalized, awayFromPlayer) + Random.Range(-0.05f, 0.05f);
            if (score > bestScore)
            {
                bestScore = score;
                coverPosition = navHit.position;
            }
        }

        return bestScore > float.NegativeInfinity;
    }

    private bool HasLineOfSight()
    {
        if (player == null) return false;

        Vector3 origin = transform.position + Vector3.up * eyeHeight;
        Vector3 toPlayer = player.position - origin;
        float sqrDistance = toPlayer.sqrMagnitude;
        float distanceToPlayer = Mathf.Sqrt(sqrDistance);
        if (distanceToPlayer < 0.01f) return false;

        if (sqrDistance > loseSightRadius * loseSightRadius) return false;

        float angle = Vector3.Angle(transform.forward, toPlayer);
        if (angle > fieldOfView * 0.5f) return false;

        if (Physics.Raycast(origin, toPlayer.normalized, out RaycastHit hit, distanceToPlayer + 0.5f, sightLayers))
        {
            return hit.transform == player;
        }

        return false;
    }

    private bool IsLowHealth()
    {
        if (_health == null) return false;
        if (_health.MaxHealth <= 0.01f) return false;

        return (_health.CurrentHealth / _health.MaxHealth) <= lowHealthFraction;
    }

    private void EnsurePlayerReference()
    {
        if (player != null) return;

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebug) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, coverSearchRadius);
    }
}
