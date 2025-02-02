using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AIController : MonoBehaviour
{
    public NavMeshAgent navMeshAgent;
    public float startWaitTime = 4;
    public float timeToRotate = 2;
    public float speedWalk = 6;
    public float speedRun = 9;

    public float viewRadius = 15;
    public float viewAngle = 90;
    public LayerMask playerMask;
    public LayerMask obstacleMask;
    public float meshResolution = 1.0f;
    public int edgeIterations = 4;
    public float edgeDistance = 0.5f;

    public Transform[] waypoints;
    private int m_CurrentWaypointIndex;

    private Vector3 playerLastPosition = Vector3.zero;
    private Vector3 m_PlayerPosition;
    private Transform playerTransform;

    private float m_WaitTime;
    private float m_TimeToRotate;
    private bool m_playerInRange;
    private bool m_PlayerNear;
    private bool m_IsPatrol;
    private bool m_CaughtPlayer;
    private PlayerRagdoll playerRagdoll;
    Animator animate;

    void Start()
    {
        playerRagdoll = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerRagdoll>();
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        m_PlayerPosition = Vector3.zero;
        m_IsPatrol = true;
        m_CaughtPlayer = false;
        m_playerInRange = false;
        m_PlayerNear = false;
        m_WaitTime = startWaitTime;
        m_TimeToRotate = timeToRotate;

        m_CurrentWaypointIndex = 0;
        navMeshAgent = GetComponent<NavMeshAgent>();

        animate = GetComponent<Animator>();

        if (waypoints.Length > 0) // Added a check here to ensure waypoints are set.
        {
            navMeshAgent.isStopped = false;
            navMeshAgent.speed = speedWalk;
            navMeshAgent.SetDestination(waypoints[m_CurrentWaypointIndex].position);
        }
    }

    private void Update()
    {
        EnviromentView();
        if (playerRagdoll.IsRagdollEnabled)
        {
            Debug.Log("Enemy aware of player's ragdoll state.");
            Stop();
            return;
        }

        if (m_playerInRange && !m_IsPatrol)
        {
            Chasing();
        }
        else
        {
            Patroling();
        }
    }

    private void Chasing()
    {
        m_PlayerNear = false;
        m_IsPatrol = false;

        if (playerRagdoll.IsRagdollEnabled)
        {
            // Stop all AI activities if the player is in ragdoll mode
            Stop();
            return;
        }

        // Check if enemy is close enough to the player to start attacking
        if (Vector3.Distance(transform.position, playerTransform.position) < 1.8f) // 3.0f is the attack range
        {
            animate.SetBool("IsAttacking", true);
            animate.SetBool("IsMoving", false);  // Stop the enemy from moving when attacking
            return;  // Return from the function, effectively preventing the code below from running
        }
        else
        {
            animate.SetBool("IsAttacking", false);
        }

        // If the AI hasn't caught the player yet, continue chasing
        if (!m_CaughtPlayer)
        {
            Move(speedRun);
            navMeshAgent.SetDestination(playerTransform.position);
        }

        // If AI has reached the player's last known position and still doesn't see the player, return to patrolling
        if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
        {
            if (!m_playerInRange)
            {
                m_IsPatrol = true;
                return;
            }
            HandleStoppingDistance();
        }
    }


    private void HandleStoppingDistance()
    {
        if (m_WaitTime <= 0 && !m_CaughtPlayer && Vector3.Distance(transform.position, GameObject.FindGameObjectWithTag("Player").transform.position) >= 6f)
        {
            m_IsPatrol = true;
            m_PlayerNear = false;
            Move(speedWalk);
            m_TimeToRotate = timeToRotate;
            m_WaitTime = startWaitTime;
            if (waypoints.Length > 0 && m_CurrentWaypointIndex < waypoints.Length) // Added a check here
            {
                navMeshAgent.SetDestination(waypoints[m_CurrentWaypointIndex].position);
            }
        }
        else
        {
            if (Vector3.Distance(transform.position, GameObject.FindGameObjectWithTag("Player").transform.position) >= 2.5f)
            {
                Stop();
            }
            m_WaitTime -= Time.deltaTime;
        }
    }

    private void Patroling()
    {
        HandlePlayerNear();

        if (!m_PlayerNear && waypoints.Length > 0 && m_CurrentWaypointIndex < waypoints.Length) // Added a check here
        {
            navMeshAgent.SetDestination(waypoints[m_CurrentWaypointIndex].position);
            if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
            {
                HandlePatrolStoppingDistance();
            }
        }
    }

    private void HandlePlayerNear()
    {
        if (m_PlayerNear)
        {
            if (m_TimeToRotate <= 0)
            {
                Move(speedWalk);
                LookingPlayer(playerLastPosition);
            }
            else
            {
                Stop();
                m_TimeToRotate -= Time.deltaTime;
            }
        }
    }

    private void HandlePatrolStoppingDistance()
    {
        if (m_WaitTime <= 0)
        {
            NextPoint();
            Move(speedWalk);
            m_WaitTime = startWaitTime;
        }
        else
        {
            Stop();
            m_WaitTime -= Time.deltaTime;
        }
    }

    public void NextPoint()
    {
        if (waypoints.Length == 0) return; // Guard against empty array
        m_CurrentWaypointIndex = (m_CurrentWaypointIndex + 1) % waypoints.Length;
        navMeshAgent.SetDestination(waypoints[m_CurrentWaypointIndex].position);
    }

    void Move(float speed)
    {
        navMeshAgent.isStopped = false;
        navMeshAgent.speed = speed;
        animate.SetBool("IsMoving", true);
    }

    void Stop()
    {
        navMeshAgent.isStopped = true;
        navMeshAgent.speed = 0;
        animate.SetBool("IsMoving", false);
        animate.SetBool("IsAttacking", false);
    }



    void CaughtPlayer()
    {
        m_CaughtPlayer = true;
    }

    void LookingPlayer(Vector3 player)
    {
        navMeshAgent.SetDestination(player);
        if (Vector3.Distance(transform.position, player) <= 0.3)
        {
            HandleLookPlayerStoppingDistance();
        }
    }

    private void HandleLookPlayerStoppingDistance()
    {
        if (m_WaitTime <= 0)
        {
            m_PlayerNear = false;
            Move(speedWalk);
            if (waypoints.Length > 0 && m_CurrentWaypointIndex < waypoints.Length) // Added a check here
            {
                navMeshAgent.SetDestination(waypoints[m_CurrentWaypointIndex].position);
            }
            m_WaitTime = startWaitTime;
            m_TimeToRotate = timeToRotate;
        }
        else
        {
            Stop();
            m_WaitTime -= Time.deltaTime;
        }
    }

    void EnviromentView()
    {
        Collider[] playerInRange = Physics.OverlapSphere(transform.position, viewRadius, playerMask);   //  Make an overlap sphere around the enemy to detect the playermask in the view radius

        for (int i = 0; i < playerInRange.Length; i++)
        {
            Transform player = playerInRange[i].transform;
            Vector3 dirToPlayer = (player.position - transform.position).normalized;
            if (Vector3.Angle(transform.forward, dirToPlayer) < viewAngle / 2)
            {
                float dstToPlayer = Vector3.Distance(transform.position, player.position);          //  Distance of the enmy and the player
                if (!Physics.Raycast(transform.position, dirToPlayer, dstToPlayer, obstacleMask))
                {
                    m_playerInRange = true;             //  The player has been seeing by the enemy and then the nemy starts to chasing the player
                    m_IsPatrol = false;                 //  Change the state to chasing the player
                }
                else
                {
                    /*
                     *  If the player is behind a obstacle the player position will not be registered
                     * */
                    m_playerInRange = false;
                }
            }
            if (Vector3.Distance(transform.position, player.position) > viewRadius)
            {
                /*
                 *  If the player is further than the view radius, then the enemy will no longer keep the player's current position.
                 *  Or the enemy is a safe zone, the enemy will no chase
                 * */
                m_playerInRange = false;                //  Change the sate of chasing
            }
            if (m_playerInRange)
            {
                /*
                 *  If the enemy no longer sees the player, then the enemy will go to the last position that has been registered
                 * */
                m_PlayerPosition = player.transform.position;       //  Save the player's current position if the player is in range of vision
            }
        }
    }
  
}