using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class ZombieAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private NavMeshAgent navAgent;
    [SerializeField] private Animator animator;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 0.5f;
    [SerializeField] private float angularSpeed = 120f;
    [SerializeField] private float acceleration = 8f;

    [Header("Detection")]
    [SerializeField] private float chaseDistance = 10f;
    [SerializeField] private float attackDistance = 1.8f;

    [Header("Combat")]
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float attackDelay = 0.5f;

    [Header("Health")]
    [SerializeField] private int maxHealth = 100;

    [Header("State")]
    [SerializeField] private ZombieState currentState = ZombieState.Idle;

    public enum ZombieState { Idle, Chase, Attack, Dead }

    private float nextAttackTime;
    public int health = 100;
    private CapsuleCollider capsuleCollider;
    
    private bool isAttacking;
    private bool isDead;
    private int lastAnimationHash = -1;

    public GameObject bloodScreenEffect;
    private GameObject instantiatedObject;

    private static readonly int IdleHash = Animator.StringToHash("Idle");
    private static readonly int WalkHash = Animator.StringToHash("Walk");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int DeadHash = Animator.StringToHash("Dead");

    private void Awake()
    {
        capsuleCollider = GetComponent<CapsuleCollider>();
        if (navAgent == null)
            navAgent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        float difficultyMultiplier = SaveMenu.GetDifficultyMultiplier();
        attackDamage *= difficultyMultiplier;
        maxHealth = Mathf.Max(1, Mathf.RoundToInt(maxHealth * difficultyMultiplier));

        GameObject playerObject = GameObject.FindWithTag("Player");
        if(playerObject != null)
        {
            player = playerObject.transform;
        }
        else
        {
            Debug.Log("Player object not found !");
        }
        health = maxHealth;

        if (player == null)
        {
            GameObject obj = GameObject.FindGameObjectWithTag("Player");
            if (obj != null)
                player = obj.transform;
        }

        if (navAgent != null)
        {
            navAgent.speed = moveSpeed;
            navAgent.angularSpeed = angularSpeed;
            navAgent.acceleration = acceleration;
            navAgent.stoppingDistance = attackDistance;
            navAgent.updateRotation = true;
        }
        else
        {
            Debug.LogError("Zombie cần có NavMeshAgent!");
        }
        
    }

    private void Update()
    {
        if (isDead || player == null)
            return;

        float distance = DistanceToPlayer();

        if (distance <= attackDistance)
            currentState = ZombieState.Attack;
        else if (distance <= chaseDistance)
            currentState = ZombieState.Chase;
        else
            currentState = ZombieState.Idle;

        switch (currentState)
        {
            case ZombieState.Idle:
                Idle();
                SetAnimationFlags(false, false, false);
                break;
            case ZombieState.Chase:
                Chase();
                SetAnimationFlags(true, false, false);
                break;
            case ZombieState.Attack:
                Attack();
                SetAnimationFlags(false, true, false);
                break;
            case ZombieState.Dead:
                break;
        }
    }

    private void Idle()
    {
        StopMoving();
        PlayAnimation(IdleHash);
    }

    private void Chase()
    {
        if (navAgent == null || !navAgent.isOnNavMesh)
            return;

        navAgent.isStopped = false;
        navAgent.SetDestination(player.position);
        PlayAnimation(WalkHash);
    }

    private void Attack()
    {
        StopMoving();
        LookAtPlayer();

        if (!isAttacking && Time.time >= nextAttackTime)
            StartCoroutine(PerformAttack());
    }

    private void SetAnimationFlags(bool isWalking, bool isAttacking, bool isDead)
    {
        if (animator == null)
            return;

        animator.SetBool("IsWalking", isWalking);
        animator.SetBool("IsAttacking", isAttacking);
        animator.SetBool("IsDead", isDead);
    }

    private IEnumerator PerformAttack()
    {
        isAttacking = true;
        nextAttackTime = Time.time + attackCooldown;

        PlayAnimation(AttackHash);
        // Hiệu ứng máu trên màn hình
        StartCoroutine(ActiveBloodScreenEffect());

        // damage the player
        PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();

        yield return new WaitForSeconds(attackDelay);

        if (!isDead && player != null && DistanceToPlayer() <= attackDistance)
            DamagePlayer();

        isAttacking = false;
    }

    private void DamagePlayer()
    {
        player.SendMessage(
            "TakeDamage",
            attackDamage,
            SendMessageOptions.DontRequireReceiver
        );

        Debug.Log("Zombie gây " + attackDamage + " damage");
    }

  

    private void StopMoving()
    {
        if (navAgent != null && navAgent.isOnNavMesh)
        {
            navAgent.isStopped = true;
            navAgent.ResetPath();
        }
    }

    private void LookAtPlayer()
    {
        if (player == null)
            return;

        Vector3 direction = player.position - transform.position;
        direction.y = 0;

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion rotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                rotation,
                Time.deltaTime * 10f
            );
        }
    }

    private float DistanceToPlayer()
    {
        return player == null
            ? Mathf.Infinity
            : Vector3.Distance(transform.position, player.position);
    }

    private void PlayAnimation(int animationHash)
    {
        if (animator == null || lastAnimationHash == animationHash)
            return;

        lastAnimationHash = animationHash;
        animator.Play(animationHash, 0, 0f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDistance);
    }
    private IEnumerator ActiveBloodScreenEffect()
    {
        InstantianObject();
        yield return new WaitForSeconds(attackDelay);
        DeleteObject();
    }

    public void TakeDamage(int damageAmount)
    {
        if(currentState == ZombieState.Dead)
            return;
        health -= damageAmount;
        if (health <= 0)
        {
            health = 0;
            Die();
        }
    }
    private void Die()
    {
        isDead = true;
        currentState = ZombieState.Dead;
        SetAnimationFlags(false, false, true);

        if (animator != null)
        {
            lastAnimationHash = DeadHash;
            animator.Play(DeadHash, 0, 0f);
        }

        if (navAgent != null && navAgent.isOnNavMesh)
        {
            navAgent.isStopped = true;
            navAgent.ResetPath();
        }

        if (capsuleCollider != null)
            capsuleCollider.enabled = false;
        
        if(GameManager.instance != null)
        {
            GameManager.instance.AddScore(10); // Gọi hàm cộng điểm từ GameManager
        }
    }
    void InstantianObject()
    {
        instantiatedObject = Instantiate(bloodScreenEffect);
    }
    void DeleteObject()
    {
        if (instantiatedObject != null)
        {
            Destroy(instantiatedObject);
            instantiatedObject = null;
        }
    }
}