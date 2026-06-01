using UnityEngine;
using UnityEngine.AI;

// AI script to control the ghost's patrolling, stalking, and chasing behaviors
public class GhostAI : MonoBehaviour
{
    public enum GhostState
    {
        Wandering,
        Chasing
    }

    [Header("State Settings")]
    [SerializeField] private GhostState currentState = GhostState.Wandering;

    [Header("Movement Speeds")]
    [SerializeField] private float wanderSpeed = 1.8f;
    [SerializeField] private float chaseSpeed = 5.0f;

    [Header("Wander Settings")]
    [SerializeField] private float wanderRadius = 8f;
    [SerializeField] private float minWanderWaitTime = 2f;
    [SerializeField] private float maxWanderWaitTime = 5f;
    [Range(0f, 1f)]
    [SerializeField] private float playerBias = 0.25f; // Bias/kecenderungan mendekati arah player secara perlahan saat patroli

    [Header("Line of Sight Settings")]
    [SerializeField] private float viewDistance = 15f;
    [SerializeField] private float viewAngle = 110f;
    [SerializeField] private float eyeHeight = 1.6f;
    [SerializeField] private LayerMask obstacleMask = ~0; // Layer penghalang pandangan (dinding, pintu, dll)

    [Header("Animation Settings")]
    [SerializeField] private string speedAnimParameter = "Speed";
    [SerializeField] private float wanderAnimValue = 1.0f; // Nilai animasi jalan (slow)
    [SerializeField] private float chaseAnimValue = 2.0f;  // Nilai animasi lari (fast)

    private NavMeshAgent agent;
    private Animator animator;
    private Transform player;
    private PlayerHealth playerHealth;

    private float wanderWaitTimer;
    private Vector3 targetWanderPoint;
    private bool hasWanderTarget = false;

    private float doorCheckTimer = 0f;
    private const float DOOR_CHECK_INTERVAL = 0.2f;

    private float diagTimer = 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        // Cari Player secara otomatis (berdasarkan Component PlayerMovement terlebih dahulu, lalu fallback ke Tag)
        GameObject playerObj = null;
        PlayerMovement pm = Object.FindFirstObjectByType<PlayerMovement>();
        if (pm != null)
        {
            playerObj = pm.gameObject;
        }
        else
        {
            playerObj = GameObject.FindWithTag("Player");
        }

        if (playerObj != null)
        {
            player = playerObj.transform;
            playerHealth = playerObj.GetComponent<PlayerHealth>();
            Debug.Log($"<color=green>[GhostAI]</color> Berhasil mendeteksi Player: {playerObj.name}");
        }
        else
        {
            Debug.LogWarning("[GhostAI] Player tidak ditemukan pada Start! Hantu akan tetap berpatroli acak.");
        }

        if (agent != null)
        {
            // Set speed awal dan stopping distance agar tidak tersangkut jauh
            agent.speed = wanderSpeed;
            agent.stoppingDistance = 0.2f;

            // WARPING/SNAP ke NavMesh terdekat pada awal permainan jika posisi hantu sedikit melayang/geser
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 10.0f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position); // Warp instan ke posisi NavMesh yang valid
                Debug.Log($"<color=green>[GhostAI]</color> Warp hantu berhasil ke posisi NavMesh terdekat: {hit.position}");
            }
            else
            {
                Debug.LogWarning("[GhostAI] Gagal menemukan NavMesh dalam radius 10 meter dari hantu pada awal game.");
            }
        }

        currentState = GhostState.Wandering;
        ChooseNextWanderPoint();
    }

    void Update()
    {
        // Cari player secara dinamis jika pada Start() belum ditemukan (misal karena loading scene)
        if (player == null)
        {
            GameObject playerObj = null;
            PlayerMovement pm = Object.FindFirstObjectByType<PlayerMovement>();
            if (pm != null)
            {
                playerObj = pm.gameObject;
            }
            else
            {
                playerObj = GameObject.FindWithTag("Player");
            }

            if (playerObj != null)
            {
                player = playerObj.transform;
                playerHealth = playerObj.GetComponent<PlayerHealth>();
                Debug.Log($"<color=green>[GhostAI]</color> Player ditemukan secara dinamis: {playerObj.name}");
            }
        }

        // Cek apakah player bersembunyi di locker (jika player terdaftar)
        bool isPlayerHiding = playerHealth != null && playerHealth.isHiding;

        // Jalankan diagnosa pergerakan setiap 3 detik di konsol untuk mencari tahu mengapa hantu tidak bergerak
        diagTimer -= Time.deltaTime;
        if (diagTimer <= 0f)
        {
            diagTimer = 3.0f;
            RunMovementDiagnostics();
        }

        // Cek dan buka pintu di depan hantu secara berkala
        doorCheckTimer -= Time.deltaTime;
        if (doorCheckTimer <= 0f)
        {
            doorCheckTimer = DOOR_CHECK_INTERVAL;
            CheckAndOpenDoors();
        }

        // State Machine
        switch (currentState)
        {
            case GhostState.Wandering:
                HandleWandering(isPlayerHiding);
                break;

            case GhostState.Chasing:
                HandleChasing(isPlayerHiding);
                break;
        }
    }

    private void CheckAndOpenDoors()
    {
        // Deteksi collider pintu di depan hantu (maju 0.6m dari pusat, tinggi 1.0m, radius deteksi 1.2m)
        Vector3 checkCenter = transform.position + transform.forward * 0.6f + Vector3.up * 1.0f;
        Collider[] colliders = Physics.OverlapSphere(checkCenter, 1.2f);

        foreach (Collider col in colliders)
        {
            // Cari komponen InteractiveDoor pada collider atau induknya
            InteractiveDoor door = col.GetComponent<InteractiveDoor>();
            if (door == null)
            {
                door = col.GetComponentInParent<InteractiveDoor>();
            }

            // Jika pintu ditemukan dan dalam keadaan tertutup, suruh hantu membukanya
            if (door != null && !door.IsOpen)
            {
                door.Toggle(transform);
                Debug.Log($"<color=orange>[GhostAI]</color> Hantu membuka pintu: {door.gameObject.name}");
            }
        }
    }

    private void RunMovementDiagnostics()
    {
        if (agent == null)
        {
            Debug.LogError("<color=red>[GhostAI-Diag]</color> Komponen NavMeshAgent tidak ditemukan pada hantu!");
            return;
        }

        if (!agent.enabled)
        {
            Debug.LogError("<color=red>[GhostAI-Diag]</color> NavMeshAgent dalam keadaan DISABLED/MATI!");
            return;
        }

        if (!agent.isOnNavMesh)
        {
            Debug.LogError("<color=red>[GhostAI-Diag]</color> Hantu TIDAK berada di atas NavMesh yang valid! Pastikan area lantai telah berwarna biru setelah di-bake dan letakkan hantu di atasnya.");
            return;
        }

        if (agent.isStopped)
        {
            Debug.LogWarning("<color=yellow>[GhostAI-Diag]</color> NavMeshAgent.isStopped bernilai TRUE! Navigasi terhenti secara program.");
        }

        if (agent.acceleration <= 0.01f)
        {
            Debug.LogError("<color=red>[GhostAI-Diag]</color> NavMeshAgent.acceleration bernilai 0 di Inspector! Hantu tidak akan pernah bisa berakselerasi untuk jalan. Ubah nilai Acceleration di Inspector minimal ke 8.");
        }

        if (agent.speed <= 0.01f)
        {
            Debug.LogError("<color=red>[GhostAI-Diag]</color> NavMeshAgent.speed bernilai 0! Hantu tidak memiliki kecepatan untuk jalan.");
        }

        // Cek tabrakan komponen
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null && cc.enabled)
        {
            Debug.LogError("<color=red>[GhostAI-Diag]</color> BENTROK KOMPONEN: Ada CharacterController aktif pada hantu. Ini memblokir NavMeshAgent untuk memindahkan posisi hantu secara fisik. Silakan hapus/matikan komponen CharacterController pada objek hantu!");
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
        {
            Debug.LogWarning("<color=yellow>[GhostAI-Diag]</color> Rigidbody aktif dan 'Is Kinematic' tidak dicentang! Hal ini dapat bentrok dengan dorongan fisik NavMeshAgent.");
        }

        if (!agent.hasPath)
        {
            Debug.LogWarning("<color=yellow>[GhostAI-Diag]</color> Hantu tidak memiliki rute tujuan jalan (No Path). Sedang mencari titik baru.");
        }
        else
        {
            Debug.Log($"<color=cyan>[GhostAI-Diag]</color> Status Rute: {agent.pathStatus}, Sisa Jarak: {agent.remainingDistance}, Kecepatan Fisik Aktual: {agent.velocity.magnitude}");
        }
    }

    private void HandleWandering(bool isPlayerHiding)
    {
        if (agent == null) return;

        agent.speed = wanderSpeed;
        
        // Atur parameter animasi agar memainkan animasi jalan lambat/idle
        if (animator != null)
        {
            animator.SetFloat(speedAnimParameter, agent.velocity.magnitude > 0.1f ? wanderAnimValue : 0f);
        }

        // Cek jika hantu melihat player (dan player sedang tidak bersembunyi)
        if (!isPlayerHiding && CanSeePlayer())
        {
            StartChase();
            return;
        }

        // Jalankan pergerakan keliling ruangan (Wandering)
        if (agent.remainingDistance <= agent.stoppingDistance + 0.1f)
        {
            if (hasWanderTarget)
            {
                // Menunggu beberapa saat di titik tujuan sebelum jalan lagi
                hasWanderTarget = false;
                wanderWaitTimer = Random.Range(minWanderWaitTime, maxWanderWaitTime);
            }

            wanderWaitTimer -= Time.deltaTime;
            if (wanderWaitTimer <= 0)
            {
                ChooseNextWanderPoint();
            }
        }
    }

    private void HandleChasing(bool isPlayerHiding)
    {
        if (agent == null) return;

        // Jika player tiba-tiba tidak terdeteksi atau hancur, hentikan pengejaran
        if (player == null)
        {
            StopChase();
            return;
        }

        // Jika player bersembunyi di locker, hentikan pengejaran
        if (isPlayerHiding)
        {
            StopChase();
            return;
        }

        // Hantu berlari kencang mengejar player
        agent.speed = chaseSpeed;
        agent.SetDestination(player.position);

        // Atur animasi lari/sprint
        if (animator != null)
        {
            animator.SetFloat(speedAnimParameter, chaseAnimValue);
        }
    }

    private void StartChase()
    {
        currentState = GhostState.Chasing;
        hasWanderTarget = false;
        Debug.Log("<color=red>[GhostAI]</color> Hantu melihat Player! Memulai pengejaran kencang.");
    }

    private void StopChase()
    {
        currentState = GhostState.Wandering;
        ChooseNextWanderPoint();
        Debug.Log("<color=green>[GhostAI]</color> Player bersembunyi di Locker. Hantu kehilangan target dan kembali berpatroli.");
    }

    private void ChooseNextWanderPoint()
    {
        if (agent == null) return;

        // Coba mencari titik patroli acak di lantai yang sama (maksimal 10 kali percobaan)
        for (int i = 0; i < 10; i++)
        {
            // Dapatkan titik acak di sekitar hantu
            Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
            randomDirection += transform.position;

            // Berikan bias/dorongan ke arah player secara perlahan (drift mendekati area player) jika player terdaftar
            Vector3 targetDirection = randomDirection;
            if (player != null)
            {
                targetDirection = Vector3.Lerp(randomDirection, player.position, playerBias);
            }

            NavMeshHit navHit;
            if (NavMesh.SamplePosition(targetDirection, out navHit, wanderRadius, NavMesh.AllAreas))
            {
                // Cegah hantu berpindah lantai secara acak saat patroli (selisih tinggi Y maksimal 2.0 meter)
                if (Mathf.Abs(navHit.position.y - transform.position.y) < 2.0f)
                {
                    targetWanderPoint = navHit.position;
                    agent.SetDestination(targetWanderPoint);
                    hasWanderTarget = true;
                    return; // Sukses menemukan titik di lantai yang sama
                }
            }
        }

        // Fallback jika tidak menemukan titik di lantai yang sama setelah 10 kali coba (misal di tangga/ramps)
        Vector3 fallbackDir = Random.insideUnitSphere * wanderRadius + transform.position;
        NavMeshHit fallbackHit;
        if (NavMesh.SamplePosition(fallbackDir, out fallbackHit, wanderRadius, NavMesh.AllAreas))
        {
            targetWanderPoint = fallbackHit.position;
            agent.SetDestination(targetWanderPoint);
            hasWanderTarget = true;
        }
    }

    private bool CanSeePlayer()
    {
        if (player == null) return false;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // 1. Cek jarak pandang maksimum
        if (distanceToPlayer > viewDistance) return false;

        // 2. Cek sudut pandang (Field of View)
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);

        if (angleToPlayer < viewAngle / 2f)
        {
            // 3. Cek Raycast apakah terhalang dinding/pintu
            Vector3 startPos = transform.position + Vector3.up * eyeHeight;
            Vector3 endPos = player.position + Vector3.up * 1.0f; // Target dada player

            RaycastHit hit;
            if (Physics.Linecast(startPos, endPos, out hit, obstacleMask))
            {
                // Jika linecast mengenai player (baik tag 'Player' atau root transform yang sama dengan player)
                if (hit.collider.CompareTag("Player") || hit.collider.transform.root == player.root)
                {
                    return true;
                }
            }
            else
            {
                // Jika linecast sama sekali tidak menabrak collider statis, pandangan bersih
                return true;
            }
        }

        return false;
    }

    // Menggambar visual visualisasi Field of View hantu di jendela Scene Unity
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewDistance);

        Vector3 leftBoundary = Quaternion.Euler(0, -viewAngle / 2f, 0) * transform.forward;
        Vector3 rightBoundary = Quaternion.Euler(0, viewAngle / 2f, 0) * transform.forward;

        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position + Vector3.up * eyeHeight, leftBoundary * viewDistance);
        Gizmos.DrawRay(transform.position + Vector3.up * eyeHeight, rightBoundary * viewDistance);
    }
}
