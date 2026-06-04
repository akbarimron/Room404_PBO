using UnityEngine;
using UnityEngine.AI;

// AI script to control the ghost's patrolling, stalking, and chasing behaviors
public class GhostAI : MonoBehaviour
{
    public enum GhostState
    {
        Wandering,
        Chasing,
        Investigating
    }

    [Header("State Settings")]
    [SerializeField] private GhostState currentState = GhostState.Wandering;

    [Header("Movement Speeds")]
    [SerializeField] private float wanderSpeed = 1.8f;
    [SerializeField] private float chaseSpeed = 5.0f;

    [Header("Wander Settings")]
    [SerializeField] private float wanderRadius = 8f;
    [SerializeField] private float minWanderWaitTime = 0f;
    [SerializeField] private float maxWanderWaitTime = 0.5f;
    [Range(0f, 1f)]
    [SerializeField] private float playerBias = 0.25f; // Bias/kecenderungan mendekati arah player secara perlahan saat patroli

    [Header("Line of Sight Settings")]
    [SerializeField] private float viewDistance = 100f;
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

    [Header("Stuck Prevention Settings")]
    [SerializeField] private float stuckCheckThreshold = 2.0f; // Detik sebelum dianggap tersangkut
    private float stuckTimer = 0f;
    private Vector3 lastStuckCheckPosition;
    private float stuckPositionTimer = 0f;

    private Vector3 spawnPosition;

    private System.Collections.Generic.List<Vector3> activeWaypoints = new System.Collections.Generic.List<Vector3>();
    private int currentWaypointIndex = 0;
    private bool isFollowingWaypoints = false;
    private bool shouldEjectPlayer = false;
    private bool wasPlayerHiding = false;
    private int localWanderCount = 0;

    [Header("Hearing Settings")]
    [SerializeField] private float sprintHearingRange = 25f;
    [SerializeField] private float walkHearingRange = 10f;
    private float lastWalkHearTime = 0f;
    private float lastSprintHearTime = 0f;

    [Header("Investigate Settings")]
    [SerializeField] private float investigateSpeed = 2.2f;
    [SerializeField] private float investigateWaitTime = 4f; // Waktu hantu menyelidiki
    private Vector3 investigatePoint;
    private float investigateWaitTimer;
    private bool hasReachedInvestigatePoint = false;
    private float currentInvestigateSpeed;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource idleMusicAudioSource;  // BG music game biasa (2D, loop)
    [SerializeField] private AudioSource chaseMusicAudioSource; // BG music saat dikejar (2D, loop)
    [SerializeField] private AudioSource ghostSoundAudioSource; // Suara idle hantu (3D, loop)
    [SerializeField] private AudioSource screamAudioSource;     // Teriakan hantu saat melihat/dengar player (3D)
    [SerializeField] private AudioSource jumpscareAudioSource;  // Suara jumpscare saat menangkap player (2D/3D)
    [SerializeField] private AudioSource footstepAudioSource;   // Suara langkah kaki hantu (3D)
    [SerializeField] private AudioClip[] footstepClips;         // Audio clip langkah kaki hantu
    [SerializeField] private float walkFootstepInterval = 0.6f;
    [SerializeField] private float runFootstepInterval = 0.35f;
    private float footstepTimer = 0f;
    private float lastScreamTime = -10f;
    private const float SCREAM_COOLDOWN = 8f;
    private System.Collections.Generic.List<InteractiveDoor> doorsOpenedByGhost = new System.Collections.Generic.List<InteractiveDoor>();
    private AudioLowPassFilter ghostSoundLowPass;




    void Start()
    {
#if UNITY_EDITOR
        if (idleMusicAudioSource != null && idleMusicAudioSource.clip == null)
            idleMusicAudioSource.clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/NewAssets/SFX/idleMusic.mp3");
            
        if (chaseMusicAudioSource != null && chaseMusicAudioSource.clip == null)
            chaseMusicAudioSource.clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/NewAssets/SFX/chaseMusic.mp3");
            
        if (ghostSoundAudioSource != null && ghostSoundAudioSource.clip == null)
            ghostSoundAudioSource.clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/NewAssets/SFX/ghostSound.mp3");
            
        if (screamAudioSource != null && screamAudioSource.clip == null)
            screamAudioSource.clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/NewAssets/SFX/ghostScream.mp3");
            
        if (jumpscareAudioSource != null && jumpscareAudioSource.clip == null)
            jumpscareAudioSource.clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/NewAssets/SFX/jumpscare.mp3");
#endif

        if (ghostSoundAudioSource != null)
        {
            ghostSoundAudioSource.spatialBlend = 1.0f; // 3D sound
            ghostSoundAudioSource.minDistance = 4.0f;  // Keep full volume up to 4 meters
            ghostSoundAudioSource.maxDistance = 20.0f; // Linear range: complete silence at 20 meters
            ghostSoundAudioSource.rolloffMode = AudioRolloffMode.Linear; // Linear attenuation for reliable gameplay audibility
            ghostSoundAudioSource.loop = true;

            // Pre-add/get AudioLowPassFilter to prevent component lookup/creation in Update loop
            ghostSoundLowPass = ghostSoundAudioSource.GetComponent<AudioLowPassFilter>();
            if (ghostSoundLowPass == null)
            {
                ghostSoundLowPass = ghostSoundAudioSource.gameObject.AddComponent<AudioLowPassFilter>();
            }
            ghostSoundLowPass.cutoffFrequency = 22000f; // Start fully open
        }

        if (screamAudioSource != null)
        {
            screamAudioSource.playOnAwake = false;
            screamAudioSource.spatialBlend = 1.0f;
            screamAudioSource.minDistance = 5.0f;
            screamAudioSource.maxDistance = 45.0f;
            screamAudioSource.rolloffMode = AudioRolloffMode.Linear;
        }

        if (jumpscareAudioSource != null)
        {
            jumpscareAudioSource.playOnAwake = false;
            jumpscareAudioSource.spatialBlend = 0.0f; // 2D Full impact jumpscare
        }

        // Paksa nilai vision diperjauh dan waktu tunggu minimal agar terus berjalan
        viewDistance = 100f;
        minWanderWaitTime = 0f;
        maxWanderWaitTime = 0.5f;


        spawnPosition = transform.position;
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

        lastStuckCheckPosition = transform.position;
        stuckPositionTimer = 0f;
        currentInvestigateSpeed = investigateSpeed;

        // Atur agar collider hantu tidak terlalu lebar sehingga tidak mudah tersangkut di pintu/dinding
        CapsuleCollider capsule = GetComponent<CapsuleCollider>();
        if (capsule != null)
        {
            capsule.radius = Mathf.Min(capsule.radius, 0.3f);
        }

        if (agent != null)
        {
            // Set speed awal dan stopping distance agar tidak tersangkut jauh
            agent.speed = wanderSpeed;
            agent.stoppingDistance = 0.2f;
            agent.radius = 0.3f; // Lebih ramping untuk clearance pintu yang andal

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

        if (animator != null)
        {
            hasSpeedParameter = false;
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.name == speedAnimParameter)
                {
                    hasSpeedParameter = true;
                    break;
                }
            }
        }

        currentState = GhostState.Wandering;
        ChooseNextWanderPoint();
    }

    void Update()
    {
        UpdateStuckDetection();
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

        // Deteksi jika player baru saja masuk loker
        if (isPlayerHiding && !wasPlayerHiding && player != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            bool canSeeLocker = false;
            
            // Cek raycast apakah pandangan ke loker bersih (tidak terhalang tembok/pintu)
            Vector3 startPos = transform.position + Vector3.up * eyeHeight;
            Vector3 endPos = player.position + Vector3.up * 1.0f;
            
            Vector3 dir = endPos - startPos;
            float dist = dir.magnitude;
            RaycastHit[] hits = Physics.RaycastAll(startPos, dir.normalized, dist, obstacleMask);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            
            bool obstructed = false;
            foreach (var h in hits)
            {
                if (h.collider.transform.root == transform.root)
                    continue;
                    
                if (h.collider.CompareTag("Player") || h.collider.transform.root == player.root || h.collider.GetComponentInParent<LockerController>() != null)
                {
                    break;
                }
                
                obstructed = true;
                break;
            }
            
            if (!obstructed)
            {
                canSeeLocker = true;
            }

            if (canSeeLocker)
            {
                if (currentState == GhostState.Chasing && distanceToPlayer <= 6.0f)
                {
                    StartInvestigating(player.position, true, chaseSpeed); // RUN to eject!
                }
                else if (currentState == GhostState.Wandering && distanceToPlayer <= 4.0f)
                {
                    StartInvestigating(player.position, false, wanderSpeed); // Walk to investigate if just wandering
                }
            }
        }
        wasPlayerHiding = isPlayerHiding;

        // Sistem Pendengaran Hantu (Ghost Hearing System)
        if (player != null && currentState != GhostState.Chasing && !isPlayerHiding)
        {
            PlayerMovement pm = player.GetComponent<PlayerMovement>();
            if (pm != null)
            {
                if (pm.IsSprinting)
                {
                    // Trigger investigasi jika sedang Wandering atau jika sedang investigasi dengan cooldown 1.0 detik
                    // (Atau jika investigasi sebelumnya adalah suara jalan yang lambat)
                    if (currentState == GhostState.Wandering || 
                        (currentState == GhostState.Investigating && (currentInvestigateSpeed < chaseSpeed || Time.time - lastSprintHearTime >= 1.0f)))
                    {
                        lastSprintHearTime = Time.time;
                        Debug.Log("<color=red>[GhostAI-Hearing]</color> Hantu mendengar suara berlari player dari kejauhan! Mengejar cepat ke lokasi larian.");
                        StartInvestigating(player.position, false, chaseSpeed); // ejectPlayer = false (aman di loker), speed = chaseSpeed
                    }
                }
                else if (pm.IsWalking)
                {
                    float distanceToPlayer = Vector3.Distance(transform.position, player.position);
                    if (distanceToPlayer <= walkHearingRange)
                    {
                        // Hanya investigasi jika sedang Wandering, atau jika sedang berjalan menginvestigasi suara langkah sebelumnya dengan cooldown 2.0 detik
                        // (Jangan batalkan investigasi lari yang sedang aktif)
                        bool isSprintingInvestigationActive = currentState == GhostState.Investigating && currentInvestigateSpeed == chaseSpeed;
                        if (!isSprintingInvestigationActive)
                        {
                            if (currentState == GhostState.Wandering || (currentState == GhostState.Investigating && Time.time - lastWalkHearTime >= 2.0f))
                            {
                                lastWalkHearTime = Time.time;
                                Debug.Log($"<color=yellow>[GhostAI-Hearing]</color> Hantu mendengar langkah jalan player di dekatnya ({distanceToPlayer:F1}m). Berjalan pelan ke lokasi suara.");
                                StartInvestigating(player.position, false, wanderSpeed); // ejectPlayer = false (aman di loker), speed = wanderSpeed
                            }
                        }
                    }
                }
            }
        }

        // Jalankan pergerakan keliling ruangan (diagnosa) setiap 3 detik
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

            case GhostState.Investigating:
                HandleInvestigating(isPlayerHiding);
                break;
        }
        UpdateAudio();
    }

    private void CheckAndOpenDoors()
    {
        // 1. Deteksi dan buka pintu di depan hantu
        Vector3 checkCenter = transform.position + transform.forward * 0.6f + Vector3.up * 1.0f;
        Collider[] colliders = Physics.OverlapSphere(checkCenter, 1.2f);

        foreach (Collider col in colliders)
        {
            InteractiveDoor door = col.GetComponent<InteractiveDoor>();
            if (door == null)
            {
                door = col.GetComponentInParent<InteractiveDoor>();
            }

            if (door != null && !door.IsOpen)
            {
                door.Toggle(transform);
                Debug.Log($"<color=orange>[GhostAI]</color> Hantu membuka pintu: {door.gameObject.name}");
                
                if (!doorsOpenedByGhost.Contains(door))
                {
                    doorsOpenedByGhost.Add(door);
                }
            }
        }

        // 2. Tutup kembali pintu yang sudah dilewati hantu (jarak > 2.8 meter)
        for (int i = doorsOpenedByGhost.Count - 1; i >= 0; i--)
        {
            InteractiveDoor openedDoor = doorsOpenedByGhost[i];
            if (openedDoor == null)
            {
                doorsOpenedByGhost.RemoveAt(i);
                continue;
            }

            if (!openedDoor.IsOpen)
            {
                doorsOpenedByGhost.RemoveAt(i);
                continue;
            }

            float distanceToDoor = Vector3.Distance(transform.position, openedDoor.transform.position);
            if (distanceToDoor > 2.8f)
            {
                openedDoor.Toggle(transform);
                Debug.Log($"<color=orange>[GhostAI]</color> Hantu menutup kembali pintu yang dilewati: {openedDoor.gameObject.name}");
                doorsOpenedByGhost.RemoveAt(i);
            }
        }
    }

    private void UpdateStuckDetection()
    {
        if (agent == null) return;

        // Hanya deteksi stuck saat sedang bergerak ke arah target (wandering atau sedang jalan investigasi)
        bool isMovingToTarget = isFollowingWaypoints && (currentState == GhostState.Wandering || (currentState == GhostState.Investigating && !hasReachedInvestigatePoint));

        if (!isMovingToTarget)
        {
            stuckTimer = 0f;
            stuckPositionTimer = 0f;
            return;
        }

        stuckPositionTimer += Time.deltaTime;
        if (stuckPositionTimer >= 1.0f) // Cek setiap 1 detik
        {
            float distanceMoved = Vector3.Distance(transform.position, lastStuckCheckPosition);
            
            // Jika dalam 1 detik bergerak kurang dari 0.15 meter
            if (distanceMoved < 0.15f)
            {
                stuckTimer += 1.0f;
                if (stuckTimer >= stuckCheckThreshold)
                {
                    Debug.LogWarning($"<color=orange>[GhostAI]</color> Hantu terdeteksi stuck di {transform.position}. Mengatur ulang rute patroli.");
                    
                    if (currentState == GhostState.Investigating)
                    {
                        // Jika stuck saat investigasi, selesaikan investigasi dengan cepat
                        isFollowingWaypoints = false;
                        hasReachedInvestigatePoint = true;
                        investigateWaitTimer = 0.5f; 
                    }
                    else
                    {
                        // Jika stuck saat patroli biasa, pilih rute baru
                        ChooseNextWanderPoint();
                    }
                    stuckTimer = 0f;
                }
            }
            else
            {
                stuckTimer = 0f;
            }

            lastStuckCheckPosition = transform.position;
            stuckPositionTimer = 0f;
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

        if (agent.speed <= 0.01f && (currentState != GhostState.Investigating || !hasReachedInvestigatePoint))
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
            PlayAnimation(isFollowingWaypoints ? "walk" : "idle", isFollowingWaypoints ? wanderAnimValue : 0f);
        }

        // Cek jika hantu melihat player (dan player sedang tidak bersembunyi)
        if (!isPlayerHiding && CanSeePlayer())
        {
            StartChase();
            return;
        }

        // Jalankan pergerakan keliling ruangan (Wandering)
        if (isFollowingWaypoints && activeWaypoints != null && activeWaypoints.Count > 0)
        {
            // Cek jika sampai di waypoint saat ini
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.2f)
            {
                currentWaypointIndex++;
                if (currentWaypointIndex < activeWaypoints.Count)
                {
                    agent.SetDestination(activeWaypoints[currentWaypointIndex]);
                    stuckTimer = 0f;
                }
                else
                {
                    // Sampai di tujuan akhir!
                    isFollowingWaypoints = false;
                    hasWanderTarget = false;
                    wanderWaitTimer = Random.Range(minWanderWaitTime, maxWanderWaitTime);
                    stuckTimer = 0f;
                }
            }
        }
        else
        {
            if (hasWanderTarget)
            {
                hasWanderTarget = false;
                wanderWaitTimer = Random.Range(minWanderWaitTime, maxWanderWaitTime);
            }

            wanderWaitTimer -= Time.deltaTime;
            if (wanderWaitTimer <= 0)
            {
                ChooseNextWanderPoint();
            }
            stuckTimer = 0f;
        }
    }

    private void HandleChasing(bool isPlayerHiding)
    {
        if (agent == null) return;

        // Jika player tiba-tiba tidak terdeteksi atau hancur, hentikan pengejaran
        if (player == null)
        {
            currentState = GhostState.Wandering;
            ChooseNextWanderPoint();
            return;
        }

        // Jika player bersembunyi di locker, mulai penyelidikan di area terakhir player terlihat
        if (isPlayerHiding)
        {
            StartInvestigating(player.position, false, chaseSpeed); // RUN to investigate!
            return;
        }

        // Hantu berlari kencang mengejar player
        agent.speed = chaseSpeed;
        agent.SetDestination(player.position);

        // Atur animasi lari/sprint
        if (animator != null)
        {
            PlayAnimation("run", chaseAnimValue);
        }
    }

    private void StartChase()
    {
        if (currentState != GhostState.Chasing)
        {
            PlayScream();
        }
        currentState = GhostState.Chasing;
        hasWanderTarget = false;
        isFollowingWaypoints = false;
        activeWaypoints.Clear();
        shouldEjectPlayer = false;
        Debug.Log("<color=red>[GhostAI]</color> Hantu melihat Player! Memulai pengejaran kencang.");
    }

    private void StartInvestigating(Vector3 lastKnownPos, bool ejectPlayer = false, float customSpeed = -1f)
    {
        currentInvestigateSpeed = customSpeed >= 0f ? customSpeed : investigateSpeed;
        bool isRunningNoise = currentInvestigateSpeed >= chaseSpeed - 0.5f;

        bool isAlreadyRunning = (currentState == GhostState.Chasing) || 
                                (currentState == GhostState.Investigating && currentInvestigateSpeed >= chaseSpeed - 0.5f);

        if (isRunningNoise && !isAlreadyRunning)
        {
            PlayScream();
        }

        currentState = GhostState.Investigating;
        investigatePoint = lastKnownPos;
        hasReachedInvestigatePoint = false;
        investigateWaitTimer = investigateWaitTime;
        shouldEjectPlayer = ejectPlayer;
        stuckTimer = 0f;

        if (agent != null)
        {
            agent.speed = currentInvestigateSpeed;
            SetupWaypoints(investigatePoint);
        }

        if (shouldEjectPlayer)
        {
            Debug.Log("<color=red>[GhostAI]</color> Player bersembunyi di depan mata hantu! Hantu pergi untuk memaksa player keluar.");
        }
        else
        {
            Debug.Log("<color=yellow>[GhostAI]</color> Player bersembunyi di Locker. Hantu pergi ke area terakhir player untuk menyelidiki.");
        }
    }

    private void HandleInvestigating(bool isPlayerHiding)
    {
        if (agent == null) return;

        // Jika player keluar dari locker dan terlihat oleh hantu, kejar kembali!
        if (!isPlayerHiding && CanSeePlayer())
        {
            StartChase();
            return;
        }

        if (!hasReachedInvestigatePoint)
        {
            agent.speed = currentInvestigateSpeed;
            if (animator != null)
            {
                bool isFast = currentInvestigateSpeed >= chaseSpeed - 0.5f;
                PlayAnimation(isFast ? "run" : "walk", isFast ? chaseAnimValue : wanderAnimValue);
            }

            // Ikuti waypoints jika ada
            if (isFollowingWaypoints && activeWaypoints != null && activeWaypoints.Count > 0)
            {
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.2f)
                {
                    currentWaypointIndex++;
                    if (currentWaypointIndex < activeWaypoints.Count)
                    {
                        agent.SetDestination(activeWaypoints[currentWaypointIndex]);
                        stuckTimer = 0f;
                    }
                    else
                    {
                        // Sampai di titik investigasi akhir!
                        isFollowingWaypoints = false;
                        hasReachedInvestigatePoint = true;
                        agent.speed = 0f; // Diam di tempat untuk menyelidiki
                        if (animator != null)
                        {
                            PlayAnimation("idle", 0f); // Animasi idle
                        }
                        stuckTimer = 0f;

                        if (shouldEjectPlayer)
                        {
                            LockerController locker = FindOccupiedLocker(investigatePoint, 2.5f);
                            if (locker != null)
                            {
                                locker.ForceEject();
                                StartChase();
                                shouldEjectPlayer = false;
                                return;
                            }
                        }
                    }
                }
            }
            else
            {
                // Fallback jika tidak menggunakan waypoints
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
                {
                    hasReachedInvestigatePoint = true;
                    agent.speed = 0f;
                    if (animator != null)
                    {
                        PlayAnimation("idle", 0f);
                    }
                    stuckTimer = 0f;

                    if (shouldEjectPlayer)
                    {
                        LockerController locker = FindOccupiedLocker(investigatePoint, 2.5f);
                        if (locker != null)
                        {
                            locker.ForceEject();
                            StartChase();
                            shouldEjectPlayer = false;
                            return;
                        }
                    }
                }
            }
        }
        else
        {
            if (shouldEjectPlayer)
            {
                LockerController locker = FindOccupiedLocker(investigatePoint, 2.5f);
                if (locker != null)
                {
                    locker.ForceEject();
                    StartChase();
                    shouldEjectPlayer = false;
                    return;
                }
            }

            // Tunggu di tempat untuk mencari-cari selama beberapa detik
            investigateWaitTimer -= Time.deltaTime;
            if (animator != null)
            {
                PlayAnimation("idle", 0f);
            }

            if (investigateWaitTimer <= 0f)
            {
                // Penyelidikan selesai, kembali ke patroli normal
                currentState = GhostState.Wandering;
                ChooseNextWanderPoint();
                Debug.Log("<color=green>[GhostAI]</color> Penyelidikan di area locker selesai. Hantu melanjutkan patroli.");
            }
        }
    }

    private LockerController FindOccupiedLocker(Vector3 position, float radius)
    {
        LockerController[] lockers = Object.FindObjectsByType<LockerController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var locker in lockers)
        {
            if (locker != null && locker.IsOccupied)
            {
                if (Vector3.Distance(locker.transform.position, position) <= radius)
                {
                    return locker;
                }
            }
        }
        return null;
    }

    private void ChooseNextWanderPoint(float customPlayerBias = -1f)
    {
        if (agent == null) return;

        Vector3 bestPoint = transform.position;
        float maxEdgeDistance = -1f;
        bool foundValidPoint = false;

        // Tentukan apakah ingin bergerak jauh (patroli jarak jauh) - 40% kesempatan
        // Paksa bergerak jauh jika sudah 2 kali berturut-turut melakukan patroli lokal, agar hantu keluar dari ruangan
        bool isFarRoam = (localWanderCount >= 2) || (Random.value < 0.4f);
        
        if (isFarRoam)
        {
            localWanderCount = 0; // Reset counter
        }
        else
        {
            localWanderCount++;
        }
        
        float currentRadius = isFarRoam ? wanderRadius * 3.5f : wanderRadius;

        // Coba cari beberapa opsi titik (maksimal 10 kali percobaan), lalu pilih yang paling jauh dari tembok (ke tengah)
        for (int i = 0; i < 10; i++)
        {
            // Dapatkan titik acak
            Vector3 randomDirection = Random.insideUnitSphere * currentRadius;

            // Jika roam far, bias/ambil titik relatif terhadap spawnPosition atau posisi sekarang agar bergerak jauh
            if (isFarRoam)
            {
                randomDirection += (Random.value < 0.5f ? spawnPosition : transform.position);
            }
            else
            {
                randomDirection += transform.position;
            }

            // Bias/dorongan ke arah player
            Vector3 targetDirection = randomDirection;
            if (player != null)
            {
                float currentBias = customPlayerBias >= 0f ? customPlayerBias : playerBias;
                targetDirection = Vector3.Lerp(randomDirection, player.position, currentBias);
            }

            NavMeshHit navHit;
            // Sampling dengan currentRadius
            if (NavMesh.SamplePosition(targetDirection, out navHit, currentRadius, NavMesh.AllAreas))
            {
                // Pastikan berada di lantai yang sama
                if (Mathf.Abs(navHit.position.y - transform.position.y) < 2.0f)
                {
                    // Cari jarak ke tepi NavMesh terdekat (tembok/pinggir)
                    NavMeshHit edgeHit;
                    if (NavMesh.FindClosestEdge(navHit.position, out edgeHit, NavMesh.AllAreas))
                    {
                        // Pilih titik yang paling jauh dari tembok (paling tengah)
                        if (edgeHit.distance > maxEdgeDistance)
                        {
                            maxEdgeDistance = edgeHit.distance;
                            bestPoint = navHit.position;
                            foundValidPoint = true;
                        }
                    }
                    else if (!foundValidPoint)
                    {
                        // Fallback jika tidak menemukan tepi
                        bestPoint = navHit.position;
                        foundValidPoint = true;
                    }
                }
            }
        }

        if (foundValidPoint)
        {
            targetWanderPoint = bestPoint;
            SetupWaypoints(targetWanderPoint);
            return;
        }

        // Fallback jika tidak menemukan titik di lantai yang sama setelah 10 kali coba (misal di tangga/ramps)
        Vector3 fallbackDir = Random.insideUnitSphere * currentRadius + transform.position;
        NavMeshHit fallbackHit;
        if (NavMesh.SamplePosition(fallbackDir, out fallbackHit, currentRadius, NavMesh.AllAreas))
        {
            targetWanderPoint = fallbackHit.position;
            SetupWaypoints(targetWanderPoint);
        }
    }

    private void SetupWaypoints(Vector3 target)
    {
        activeWaypoints.Clear();
        currentWaypointIndex = 0;
        isFollowingWaypoints = false;

        if (agent == null || !agent.isOnNavMesh) return;

        NavMeshPath path = new NavMeshPath();
        if (agent.CalculatePath(target, path) && (path.status == NavMeshPathStatus.PathComplete || path.status == NavMeshPathStatus.PathPartial))
        {
            Vector3[] corners = path.corners;
            if (corners.Length > 1)
            {
                // Modifikasi corner agar berada di tengah jalan (kecuali start dan end)
                float desiredBuffer = 1.3f; // Buffer minimal dari tembok
                
                // Tambahkan start corner (posisi hantu saat ini)
                activeWaypoints.Add(corners[0]);

                for (int i = 1; i < corners.Length - 1; i++)
                {
                    Vector3 adjustedCorner = corners[i];
                    NavMeshHit edgeHit;
                    if (NavMesh.FindClosestEdge(corners[i], out edgeHit, NavMesh.AllAreas))
                    {
                        if (edgeHit.distance < desiredBuffer)
                        {
                            // Ambil normal tepi yang menunjuk ke dalam area walkable
                            Vector3 pushDir = edgeHit.normal;
                            pushDir.y = 0f;
                            if (pushDir.sqrMagnitude > 0.001f)
                            {
                                pushDir.Normalize();
                                Vector3 proposed = corners[i] + pushDir * (desiredBuffer - edgeHit.distance);
                                
                                NavMeshHit navHit;
                                if (NavMesh.SamplePosition(proposed, out navHit, desiredBuffer + 0.5f, NavMesh.AllAreas))
                                {
                                    adjustedCorner = navHit.position;
                                }
                            }
                        }
                    }
                    activeWaypoints.Add(adjustedCorner);
                }

                // Tambahkan end corner (tujuan akhir)
                activeWaypoints.Add(corners[corners.Length - 1]);
            }
            else
            {
                activeWaypoints.Add(target);
            }
        }
        else
        {
            activeWaypoints.Add(target);
        }

        if (activeWaypoints.Count > 0)
        {
            currentWaypointIndex = 0;
            isFollowingWaypoints = true;
            agent.SetDestination(activeWaypoints[0]);
            hasWanderTarget = true;
        }
    }

    private bool CheckPathClear(Vector3 start, Vector3 target)
    {
        Vector3 direction = target - start;
        float distance = direction.magnitude;
        Vector3 dirNormalized = direction.normalized;

        RaycastHit[] hits = Physics.RaycastAll(start, dirNormalized, distance, obstacleMask);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            if (hit.collider.transform.root == transform.root)
                continue;

            if (hit.collider.CompareTag("Player") || hit.collider.transform.root == player.root)
            {
                return true;
            }

            return false;
        }

        return true;
    }

    private bool CanSeePlayer()
    {
        if (player == null) return false;
        if (playerHealth != null && playerHealth.isHiding) return false;


        PlayerMovement pm = player.GetComponent<PlayerMovement>();
        bool isCrouching = pm != null && pm.IsCrouching;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // 1. Cek jarak pandang maksimum
        if (distanceToPlayer > viewDistance) return false;

        // Deteksi Proksimitas: jika player sangat dekat (di bawah 2.5 meter), hantu menyadarinya walaupun berada di belakang
        float proximityRadius = 2.5f;
        bool isCloseEnough = distanceToPlayer <= proximityRadius;

        // 2. Cek sudut pandang (Field of View) atau dalam jangkauan sangat dekat
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);

        if (isCloseEnough || (angleToPlayer < viewAngle / 2f))
        {
            // 3. Cek Raycast apakah terhalang dinding/pintu (tetap tidak boleh terhalang tembok)
            Vector3 startPos = transform.position + Vector3.up * eyeHeight;
            
            // Sesuaikan tinggi target raycast berdasarkan status jongkok
            float chestHeight = isCrouching ? 0.4f : 1.0f;
            float headHeight = isCrouching ? 0.75f : 1.65f;

            Vector3 endChest = player.position + Vector3.up * chestHeight;
            Vector3 endHead = player.position + Vector3.up * headHeight;

            // Jika dekat sekali (di bawah 2.5m) dan jongkok, tetap terdeteksi kecuali terhalang dinding/pintu utama
            if (isCloseEnough && isCrouching)
            {
                Vector3 direction = endHead - startPos;
                float distance = direction.magnitude;
                Vector3 dirNormalized = direction.normalized;

                RaycastHit[] hits = Physics.RaycastAll(startPos, dirNormalized, distance, obstacleMask);
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                bool hasObstacle = false;
                foreach (var hit in hits)
                {
                    if (hit.collider.transform.root == transform.root)
                        continue;

                    if (hit.collider.CompareTag("Player") || hit.collider.transform.root == player.root)
                        break;

                    string hitName = hit.collider.gameObject.name.ToLower();
                    if (hitName.Contains("wall") || hitName.Contains("door") || hitName.Contains("partition") || hitName.Contains("floor") || hitName.Contains("ceiling"))
                    {
                        hasObstacle = true;
                        break;
                    }
                }
                
                if (!hasObstacle)
                {
                    return true;
                }
            }

            bool canSeeChest = CheckPathClear(startPos, endChest);
            bool canSeeHead = CheckPathClear(startPos, endHead);

            if (canSeeChest || canSeeHead)
            {
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

    private string currentAnimState = "";
    private bool hasSpeedParameter = false;

    private string GetValidStateName(string stateName)
    {
        if (animator == null) return null;
        if (animator.HasState(0, Animator.StringToHash(stateName)))
            return stateName;

        // Coba versi huruf kapital pertama (PascalCase)
        string pascalCase = char.ToUpper(stateName[0]) + stateName.Substring(1);
        if (animator.HasState(0, Animator.StringToHash(pascalCase)))
            return pascalCase;

        // Coba versi huruf besar semua (UPPERCASE)
        string upperCase = stateName.ToUpper();
        if (animator.HasState(0, Animator.StringToHash(upperCase)))
            return upperCase;

        return null;
    }

    private void PlayAnimation(string stateName, float speedValue)
    {
        if (animator == null) return;

        if (hasSpeedParameter && !string.IsNullOrEmpty(speedAnimParameter))
        {
            animator.SetFloat(speedAnimParameter, speedValue);
        }

        string validStateName = GetValidStateName(stateName);
        if (validStateName != null)
        {
            if (currentAnimState != validStateName)
            {
                currentAnimState = validStateName;
                animator.CrossFadeInFixedTime(validStateName, 0.15f);
            }
        }
        else
        {
            // Fallback ke "walk" / "Walk" / "WALK" jika state target tidak ditemukan agar hantu tidak stuck di T-pose/bind pose (clipping ke lantai)
            string fallbackWalk = GetValidStateName("walk");
            if (fallbackWalk != null && currentAnimState != fallbackWalk)
            {
                currentAnimState = fallbackWalk;
                animator.CrossFadeInFixedTime(fallbackWalk, 0.15f);
            }
        }
    }

    private void PlayScream()
    {
        if (screamAudioSource != null && Time.time - lastScreamTime >= SCREAM_COOLDOWN)
        {
            lastScreamTime = Time.time;
            screamAudioSource.Play();
            Debug.Log("[GhostAI-Audio] Ghost Scream triggered!");
        }
    }

    public void TriggerJumpscare()
    {
        if (jumpscareAudioSource != null)
        {
            jumpscareAudioSource.Play();
            Debug.Log("[GhostAI-Audio] Jumpscare audio triggered!");
        }
    }

    /// <summary>
    /// Sembunyikan atau tampilkan model/renderer hantu.
    /// Dipanggil oleh JumpscareManager saat jumpscare berlangsung.
    /// </summary>
    public void SetInvisible(bool invisible)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            r.enabled = !invisible;
        }

        // Juga disable NavMeshAgent movement saat invisible agar hantu diam
        if (agent != null)
        {
            agent.isStopped = invisible;
            if (invisible) agent.velocity = Vector3.zero;
        }

        Debug.Log($"[GhostAI] Ghost {(invisible ? "disembunyikan" : "ditampilkan")} (invisible={invisible})");
    }

    /// <summary>
    /// Teleport hantu ke posisi jauh dari player (simulasi lantai 3-4).
    /// Dicari NavMesh point yang valid minimal 'minDistance' meter dari player,
    /// dan Y offset lebih tinggi untuk simulasi lantai atas.
    /// </summary>
    [Header("Jumpscare Teleport Settings")]
    [SerializeField] private float jumpscareMinTeleportDistance = 25f;
    [SerializeField] private float jumpscareYOffset = 8f;  // Tinggi lantai 3-4 (sesuaikan)
    [SerializeField] private Transform[] jumpscareSpawnPoints; // (Opsional) titik teleport manual

    public void TeleportFarFromPlayer()
    {
        if (player == null)
        {
            Debug.LogWarning("[GhostAI] Tidak bisa teleport: player reference null.");
            return;
        }

        // Prioritas 1: Gunakan spawnPoints manual jika diassign di Inspector
        if (jumpscareSpawnPoints != null && jumpscareSpawnPoints.Length > 0)
        {
            // Pilih spawnPoint yang paling jauh dari player
            Transform best = null;
            float bestDist = -1f;
            foreach (var sp in jumpscareSpawnPoints)
            {
                if (sp == null) continue;
                float d = Vector3.Distance(sp.position, player.position);
                if (d > bestDist)
                {
                    bestDist = d;
                    best = sp;
                }
            }

            if (best != null)
            {
                NavMeshHit hit;
                if (NavMesh.SamplePosition(best.position, out hit, 5f, NavMesh.AllAreas))
                {
                    agent.Warp(hit.position);
                    currentState = GhostState.Wandering;
                    ChooseNextWanderPoint();
                    Debug.Log($"[GhostAI] Teleport ke spawnPoint '{best.name}' ({hit.position})");
                    return;
                }
            }
        }

        // Prioritas 2: Cari NavMesh point jauh dari player + Y offset (lantai atas)
        Vector3 playerPos = player.position;
        int attempts = 30;
        float searchRadius = jumpscareMinTeleportDistance * 2f;

        for (int i = 0; i < attempts; i++)
        {
            // Arah acak, tapi minimal sejauh jumpscareMinTeleportDistance
            Vector3 randomDir = Random.insideUnitSphere;
            randomDir.y = 0f;
            randomDir = randomDir.normalized;

            float distance = Random.Range(jumpscareMinTeleportDistance, jumpscareMinTeleportDistance * 1.8f);
            Vector3 candidatePos = playerPos + randomDir * distance;
            candidatePos.y += jumpscareYOffset; // Simulasi lantai atas

            NavMeshHit hit;
            if (NavMesh.SamplePosition(candidatePos, out hit, searchRadius, NavMesh.AllAreas))
            {
                float actualDist = Vector3.Distance(hit.position, playerPos);
                if (actualDist >= jumpscareMinTeleportDistance * 0.5f)
                {
                    agent.Warp(hit.position);
                    currentState = GhostState.Wandering;
                    ChooseNextWanderPoint();
                    Debug.Log($"[GhostAI] Teleport jauh berhasil ke {hit.position} (jarak dari player: {actualDist:F1}m)");
                    return;
                }
            }
        }

        // Fallback: Warp ke spawnPosition awal (posisi saat scene dimulai)
        NavMeshHit spawnHit;
        if (NavMesh.SamplePosition(spawnPosition, out spawnHit, 10f, NavMesh.AllAreas))
        {
            agent.Warp(spawnHit.position);
            currentState = GhostState.Wandering;
            ChooseNextWanderPoint();
            Debug.Log($"[GhostAI] Fallback teleport ke spawnPosition awal: {spawnHit.position}");
        }
    }

    private void UpdateAudio()
    {
        // Pindahkan posisi GameObject audio hantu agar menempel pada hantu jika tidak menjadi child
        if (ghostSoundAudioSource != null && ghostSoundAudioSource.transform.parent != transform)
        {
            ghostSoundAudioSource.transform.position = transform.position;
        }
        if (screamAudioSource != null && screamAudioSource.transform.parent != transform)
        {
            screamAudioSource.transform.position = transform.position;
        }
        if (footstepAudioSource != null && footstepAudioSource.transform.parent != transform)
        {
            footstepAudioSource.transform.position = transform.position;
        }

        // 1. Fade Chase Music, Idle Music, & Ghost Passive Sound
        bool isGhostRunning = (currentState == GhostState.Chasing) || 
                               (currentState == GhostState.Investigating && currentInvestigateSpeed >= chaseSpeed - 0.5f);

        if (chaseMusicAudioSource != null)
        {
            float targetVol = isGhostRunning ? 1.0f : 0f;
            if (targetVol > 0f && !chaseMusicAudioSource.isPlaying)
            {
                chaseMusicAudioSource.volume = 0f;
                chaseMusicAudioSource.Play();
            }

            chaseMusicAudioSource.volume = Mathf.MoveTowards(chaseMusicAudioSource.volume, targetVol, Time.deltaTime * 1.5f);

            if (chaseMusicAudioSource.volume <= 0f && chaseMusicAudioSource.isPlaying)
            {
                chaseMusicAudioSource.Stop();
            }
        }

        if (idleMusicAudioSource != null)
        {
            float targetVol = isGhostRunning ? 0f : 1.0f;
            if (targetVol > 0f && !idleMusicAudioSource.isPlaying)
            {
                idleMusicAudioSource.volume = 0f;
                idleMusicAudioSource.Play();
            }

            idleMusicAudioSource.volume = Mathf.MoveTowards(idleMusicAudioSource.volume, targetVol, Time.deltaTime * 1.5f);

            if (idleMusicAudioSource.volume <= 0f && idleMusicAudioSource.isPlaying)
            {
                idleMusicAudioSource.Stop();
            }
        }

        if (ghostSoundAudioSource != null)
        {
            float targetVol = 1.0f; // Default volume target

            // 1. Raycast check for wall/floor/ceiling occlusion (starting from ghost chest/head level to avoid floor collision)
            bool isOccluded = false;
            if (player != null)
            {
                Vector3 startPos = transform.position + Vector3.up * 1.5f; // Raised to avoid floor collider intersection
                Vector3 endPos = player.position + Vector3.up * 1.5f; // Player head level
                Vector3 direction = endPos - startPos;
                float distanceToPlayer = direction.magnitude;

                if (distanceToPlayer > 0.5f)
                {
                    int layerMask = ~LayerMask.GetMask("Ignore Raycast", "UI", "Water");
                    RaycastHit[] hits = Physics.RaycastAll(startPos, direction.normalized, distanceToPlayer, layerMask, QueryTriggerInteraction.Ignore);
                    
                    // Sort hits by distance
                    System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                    
                    foreach (var hit in hits)
                    {
                        // Ignore the ghost's own colliders and child parts
                        if (hit.collider.transform.root == transform.root || hit.collider.transform.IsChildOf(transform))
                            continue;
                            
                        // Ignore trigger colliders
                        if (hit.collider.isTrigger)
                            continue;

                        // Ignore player colliders and child parts
                        if (hit.collider.CompareTag("Player") || hit.collider.transform.root == player.root || hit.collider.transform.IsChildOf(player))
                        {
                            // Hit player first, path is clear!
                            break;
                        }

                        // If the first solid collider hit is NOT the player, it's a wall/door/obstacle
                        isOccluded = true;
                        break;
                    }
                }
            }

            // 2. Muffle/occlude sound using cached LowPass filter (2500Hz when occluded to let voice/whisper through clearly, 22000Hz normal)
            if (ghostSoundLowPass != null)
            {
                float targetCutoff = isOccluded ? 2500f : 22000f;
                ghostSoundLowPass.cutoffFrequency = Mathf.Lerp(ghostSoundLowPass.cutoffFrequency, targetCutoff, Time.deltaTime * 6f);
            }

            // Dim volume slightly by 20% (multiplier 0.8f) when behind walls to keep it highly audible
            float occlusionVolumeMultiplier = isOccluded ? 0.8f : 1.0f;
            targetVol *= occlusionVolumeMultiplier;

            if (!ghostSoundAudioSource.isPlaying)
            {
                ghostSoundAudioSource.volume = 0f;
                ghostSoundAudioSource.Play();
            }

            ghostSoundAudioSource.volume = Mathf.MoveTowards(ghostSoundAudioSource.volume, targetVol, Time.deltaTime * 1.5f);
        }

        // 2. Ghost Footstep Sounds
        if (agent != null && agent.enabled && agent.isOnNavMesh && footstepAudioSource != null && footstepClips != null && footstepClips.Length > 0)
        {
            // Bersuara hanya jika bergerak secara fisik
            bool isMovingPhysically = agent.velocity.sqrMagnitude > 0.05f;
            if (isMovingPhysically)
            {
                footstepTimer -= Time.deltaTime;
                if (footstepTimer <= 0f)
                {
                    bool isRunning = currentState == GhostState.Chasing || (currentState == GhostState.Investigating && currentInvestigateSpeed >= chaseSpeed - 0.5f);
                    float interval = isRunning ? runFootstepInterval : walkFootstepInterval;
                    
                    footstepTimer = interval;

                    int randomIndex = Random.Range(0, footstepClips.Length);
                    AudioClip clip = footstepClips[randomIndex];
                    if (clip != null)
                    {
                        footstepAudioSource.PlayOneShot(clip);
                    }
                }
            }
            else
            {
                footstepTimer = 0f; // Reset jeda
            }
        }
    }
}

