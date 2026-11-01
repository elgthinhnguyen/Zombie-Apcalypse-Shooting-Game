using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class WaypointZombieAI : MonoBehaviour
{
    public NavMeshAgent navAgent;

    public enum ZombieState { Walk, Chase, Attack, Dead }
    public Animator animator;
    public ZombieState currentState = ZombieState.Walk;

    public Transform player;
    public float chaseDistance = 10f;
    public float attackDistance = 2f;
    public float attackCooldown = 2f;
    public float attackDelay = 1.5f;
    public int damege = 10;
    public int health = 100;

    private CapsuleCollider capsuleCollider;
    private bool isAttacking;
    private bool isMoving;
    private float lastAttackTime;

    public GameObject bloodScreenEffect;

    private GameObject instantiatedObject;
    void Start()
    {
        float difficultyMultiplier = SaveMenu.GetDifficultyMultiplier();
        damege = Mathf.Max(1, Mathf.RoundToInt(damege * difficultyMultiplier));
        health = Mathf.Max(1, Mathf.RoundToInt(health * difficultyMultiplier));

        capsuleCollider = GetComponent<CapsuleCollider>();
        navAgent = GetComponent<NavMeshAgent>();
        lastAttackTime = -attackCooldown; // Initialize last attack time to allow immediate attack
        animator = GetComponent<Animator>();
        // Tự động tìm player nếu chưa assign trong Inspector
        if (player == null)
        {
             GameObject playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null)
            player = playerObject.transform;
            else
            Debug.LogError("WaypointZombieAI: Không tìm thấy Player! Hãy gắn tag 'Player'.");
        }
    }

    void Update()
    {
        switch (currentState)
        {
            case ZombieState.Walk:
                if(!isMoving || navAgent.remainingDistance <0.1f)
                {
                    Walk();
                }
                if (IsPlayerInRange(chaseDistance))
                {
                    currentState = ZombieState.Chase;
                    break;
                }
                break;
            case ZombieState.Chase:
                ChasePlayer();
                if (IsPlayerInRange(attackDistance))
                {
                    currentState = ZombieState.Attack;
                    break;
                }
                break;
            case ZombieState.Attack:
                AttackPlayer();
                if (!IsPlayerInRange(attackDistance))
                {
                    currentState = ZombieState.Chase;
                    break;
                }
                break;
            case ZombieState.Dead:
                animator.SetBool("IsChasing", false);
                animator.SetBool("IsAttacking", false);
                animator.SetBool("IsDead", true);
                navAgent.enabled = false;
                capsuleCollider.enabled = false;
                enabled = false; // Disable the script to stop further updates
                Debug.Log("Dead");
                break;
        }
    }

    private bool IsPlayerInRange(float range)
    {
        return Vector3.Distance(transform.position, player.position) <= range;
    }
    private void Walk()
    {
        navAgent.speed = 0.3f;
        Vector3 randomPosition = RandomNavMeshPosition();
        navAgent.SetDestination(randomPosition);
        isMoving = true;
    }
    private Vector3 RandomNavMeshPosition()
    {
        Vector3 randomDirection = Random.insideUnitSphere * 10f; 
        randomDirection += transform.position; 

        NavMeshHit hit;
        NavMesh.SamplePosition(randomDirection, out hit, 10f, NavMesh.AllAreas);
        return hit.position;  
    }
    private void ChasePlayer()
    {

        navAgent.speed = 3f;
        animator.SetBool("IsChasing", true);
        animator.SetBool("IsAttacking", false);
        navAgent.SetDestination(player.position);
    }
    private void AttackPlayer()
    {
        animator.SetBool("IsChasing", false);
        animator.SetBool("IsAttacking", true);
        navAgent.SetDestination(transform.position); 
        if(!isAttacking && Time.time - lastAttackTime >= attackCooldown)
        {
            StartCoroutine(AttackWithDelay());
            StartCoroutine(ActiveBloodScreenEffect());
            lastAttackTime = Time.time;

            PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                playerMovement.TakeDamage(damege);
            }
        }
    }

    private IEnumerator AttackWithDelay()
    {
        isAttacking = true;
        yield return new WaitForSeconds(attackDelay);
        isAttacking = false;
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
        currentState = ZombieState.Dead;

        if (GameManager.instance != null)
        {
            GameManager.instance.AddScore(10);
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
