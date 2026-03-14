using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class Companion : MonoBehaviour
{
    public GameObject Body;

    public Player player;
    public Vector3 Offset;
    public Vector3 SpearOffset;

    public NavMeshAgent agent;

    [SerializeField] private GameObject Spear;
    public List<SpearAttackScript> previousSpears;
    private SpearAttackScript.SpearSpawnState lastState;

    public float spearAttackCooldown = 0.6f;
    public float slamCooldown = 1.1f;

    public float slamShockwaveRadius = 6f;

    private bool canAttack = true;
    public bool SlamAttacking { get; private set; } = false;
    private bool shouldMove = true;
    [HideInInspector] public bool movementOverride = false;

    public Animator _animator;
    public float ActionInterval = 5f;
    private float idleAnimTimer;
    private bool playingIdleAnim = false;

    public Enemy.EnemyAction[] IdleActions;

    private bool clearOverhead = true;

    [Header("Positioning")]
    public float baseYHeight = 1.8f;
    public float normalCirclingRadius = 1.5f;
    public float sprintingCirclingRadius = 1f;
    public float circlingSpeed = 1.2f;
    public float walkingCirclingSpeed = 1.5f;
    public float sprintingCirclingSpeed = 2.2f;

    [Header("Follow Smoothing")]
    public float followLerpSpeed = 8f;

    [Header("Bobbing")]
    public float bobbingStrength = 0.25f;
    public float bobbingSpeed = 2f;

    [Header("Stopping Behavior")]
    [Range(0f, 1f)]
    public float chanceToStopCircling = 0.3f;
    public float minCirclingTime = 3f;
    public float maxCirclingTime = 7f;
    public float chanceToFlipOrbitDirection = 0.3f;

    [Header("Look Around")]
    public Vector2Int lookDirectionCountRange = new Vector2Int(1, 3);
    public Vector2 lookDurationRange = new Vector2(1.5f, 3f);
    public float minLookAngleDelta = 30f;

    [Header("Rotation")]
    public float rotationSpeed = 5f;

    [SerializeField] int orbitProbeCount = 16;
    [SerializeField] int minFreeTravelOrbitProbeCount = 3;
    [SerializeField] float probeRadius = 0.35f;
    [SerializeField] LayerMask obstacleMask;

    private bool[] orbitBlocked;
    private Vector3[] orbitProbeWorld;
    private int probesBlocked;

    private bool[] ghostOrbitBlocked;
    private Vector3[] ghostOrbitProbeWorld;
    private int ghostWallCheckProbesBlocked;
    private int ghostWallCheckProbeCount = 12;

    private float orbitAngle;
    private float bobTime;
    private float circlingTimer;
    private float timeSinceLastLookLoop = 0f;
    private float origCirclingSpeed;
    private float circlingRadius;
    private bool isCircling = true;
    private bool isReturning = false;
    private bool isLooking = false;
    private float? prevCirclingSpeed;
    private float? overrideCirclingSpeed;
    private float? prevBaseYHeight;
    private int orbitDirection = 1; // +1 = CCW, -1 = CW

    enum CompanionMovementMode
    {
        Orbit,      // Circling / bobbing movement
        NavMove,     // NavMeshAgent has full control
    }
    CompanionMovementMode movementMode = CompanionMovementMode.Orbit;

    private bool playerMoving;
    private bool playerSprinting;
    private bool playerTooFarAway;
    private bool clearAbovePlayer;
    public Vector3 followVelocity;
    Vector3 sprintFacing;
    Vector3? forcedWorldTarget;

    private Transform grabPosition;
    public GameObject heldObject;
    private int heldObjectPrevLayer;
    public float? carriedObjectExtentsY;
    private float carryOffsetDistance = 1.35f;
    private float prevCarryOffsetDistance;
    [HideInInspector] public bool isCarrying = false;
    [HideInInspector] public bool isPlayingGrabAnim = false;
    [HideInInspector] public float agentSpeedBeforeGrab = 24f;
    [HideInInspector] public float agentAccelerationBeforeGrab = 24f;
    [HideInInspector] public float carryBobTime = 0f;
    [HideInInspector] public bool disableAgentOnReachTarget = false;

    [SerializeField] private bool drawGhostProbesGizmo = false;
    private bool isPlayingEntranceAnim = false;

    public bool IsDoingRunePuzzle = false;
    public bool CanExitRunePuzzleState = false;

    public Material CrystalBallMaterial;
    public Color OrigCrystalColor;
    public Color DeadCrystalColor = new Color32(22, 34, 89, 255);

    public BlobShadowMesh BlobShadow;
    private float? prevBlobShadowHeight;

    private Coroutine behaviorRoutine;
    private Coroutine attackCooldownRoutine;
    private Coroutine outOfEnergyShakeRoutine;
    public Coroutine DoorEntranceAnimRoutine;
    public Coroutine PickUpRoutine;
    public Coroutine PutDownRoutine;

    private void OnEnable()
    {
        Companion existing = GameObject.FindObjectOfType<Companion>();
        if (existing != null && existing != this)
        {
            Debug.Log("Companion already exists, cancelling spawn.");
            Destroy(gameObject);
            return;
        }
    }
    private void Awake()
    {
        orbitBlocked = new bool[orbitProbeCount];
        orbitProbeWorld = new Vector3[orbitProbeCount];

        ghostOrbitBlocked = new bool[ghostWallCheckProbeCount];
        ghostOrbitProbeWorld = new Vector3[ghostWallCheckProbeCount];
    }

    private void Start()
    {
        ParticleSpawner.OnSendEnergy += CollectEnergy;
        _animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        circlingTimer = minCirclingTime;
        origCirclingSpeed = circlingSpeed;
        circlingRadius = normalCirclingRadius;
        sprintFacing = transform.forward;

        agentSpeedBeforeGrab = agent.speed;
        agentAccelerationBeforeGrab = agent.acceleration;

        Renderer renderer = Body.GetComponent<Renderer>();

        // Cache once
        int crystalIndex = Array.FindIndex(renderer.sharedMaterials,
            m => m.name.Contains("CrystalBall"));

        if (crystalIndex == -1)
        {
            Debug.LogError("CrystalBall material not found.");
            return;
        }

        CrystalBallMaterial = renderer.materials[crystalIndex];
        OrigCrystalColor = CrystalBallMaterial.GetColor("_EmissionColor");

        if (BlobShadow == null)
        {
            BlobShadowMesh blob = GameObject.Find("ConstructBlobShadow").GetComponent<BlobShadowMesh>();
            if (blob != null)
            {
                BlobShadow = blob;
            }
        }

        IdleActions = new[]
        {
            new Enemy.EnemyAction
            {
                TriggerName = "Idle_Twitch",
                Weight = 0.6f,
                CanUse = () => !movementOverride && !isLooking,
                CustomData = new []
                {
                    "slow;0.25", 
                    "reset_look_timer"
                }
            },
            new Enemy.EnemyAction
            {
                TriggerName = "Idle_Grip",
                Weight = 0.6f,
                CanUse = () => !movementOverride && !isLooking,
                CustomData = new []
                {
                    "slow;0.5"
                }
            },
            new Enemy.EnemyAction
            {
                TriggerName = "Idle_Spin",
                Weight = 0.2f,
                CanUse = () => !movementOverride && clearOverhead && !isLooking && !playerMoving && clearAbovePlayer,
                CustomData = new []
                {
                    "stop", 
                    "change_wings;0",
                    "reset_look_timer"
                }
            }
        };

        behaviorRoutine = StartCoroutine(BehaviorLoop());
    }
    private void OnDisable()
    {
        ParticleSpawner.OnSendEnergy -= CollectEnergy;
    }

    private void Update()
    {
        // -----------------------------
        // 1. NON-MOVEMENT LOGIC (always runs)
        // -----------------------------

        probesBlocked = 0;
        for (int i = 0; i < orbitProbeCount; i++)
        {
            if (orbitBlocked[i] == true)
            {
                probesBlocked++;
            }
        }

        ghostWallCheckProbesBlocked = 0;
        for (int i = 0; i < ghostWallCheckProbeCount; i++)
        {
            if (ghostOrbitBlocked[i] == true)
            {
                ghostWallCheckProbesBlocked++;
            }
        }

        Vector3 playerTop = player.transform.position + (Vector3.up * player.MainCollider.height);
        float roofCheckRayLength;
        float origBaseHeight;

        if (prevBaseYHeight.HasValue)
        {
            origBaseHeight = prevBaseYHeight.Value;
        }
        else
        {
            origBaseHeight = baseYHeight;
        }

        float probeHalfHeight = (1 + probeRadius * 2f) / 2;
        roofCheckRayLength = (origBaseHeight + probeHalfHeight) - player.MainCollider.height;

        float checkExtent = 0.75f; // Half size of square

        LayerMask mask = LayerMask.GetMask("Default", "Enemy", "LightReflector", "Pushable", "Shrouders");

        Vector3[] offsets =
        {
            Vector3.zero, // center
            new Vector3( checkExtent, 0f,  checkExtent),
            new Vector3(-checkExtent, 0f,  checkExtent),
            new Vector3( checkExtent, 0f, -checkExtent),
            new Vector3(-checkExtent, 0f, -checkExtent),
        };

        bool allHit = true;
        float lowestHitY = float.MaxValue;

        for (int v = 0; v < offsets.Length; v++)
        {
            Vector3 offset = offsets[v];

            if (v != 0)
            {
                if (Physics.Raycast(playerTop, offset.normalized, out RaycastHit hit, offset.magnitude, mask))
                {
                    offset = hit.point - player.transform.position;
                    offset.y = 0f;
                }
            }

            Vector3 origin = playerTop + offset;

            if (Physics.Raycast(origin, Vector3.up, out RaycastHit hit2, roofCheckRayLength, mask))
            {
                lowestHitY = Mathf.Min(lowestHitY, hit2.point.y);

                Debug.DrawLine(origin, hit2.point, Color.magenta);
            }
            else
            {
                allHit = false;
                Debug.DrawLine(origin, origin + Vector3.up * roofCheckRayLength, Color.cyan);
            }
        }

        if (allHit)
        {
            clearAbovePlayer = false;

            if (!prevBaseYHeight.HasValue)
                prevBaseYHeight = baseYHeight;

            DrawUI.Draw($"Lowering probe ring height...",
                new Vector2(1200, 50),
                Color.white,
                8);

            baseYHeight = lowestHitY - probeHalfHeight - player.transform.position.y - 0.15f;
        }
        else
        {
            clearAbovePlayer = true;

            if (prevBaseYHeight.HasValue)
            {
                baseYHeight = prevBaseYHeight.Value;
                prevBaseYHeight = null;
            }
        }

        UpdateOrbitProbes();
        UpdateGhostProbes();

        if (isPlayingEntranceAnim)
            return;

        if (!isCarrying)
        {
            SpearAttack();
            SlamAttack();
        }

        // -----------------------------
        // 2. SENSING / STATE DETECTION
        // -----------------------------
        Vector3 dirToPlayer = player.transform.position - transform.position;

        bool playerCanNotBeSeen = true;
        if (Physics.Raycast(
            transform.position + dirToPlayer.normalized,
            dirToPlayer,
            out RaycastHit hit3,
            float.MaxValue,
            LayerMask.GetMask("Default", "Player", "Pushable", "LightReflector")))
        {
            playerCanNotBeSeen =
                hit3.collider.gameObject.layer != LayerMask.NameToLayer("Player");
        }

        playerMoving =
            player.moveDir.magnitude > 0.05f &&
            player.moveInput.magnitude > 0.05f;

        playerSprinting = playerMoving && player.Sprinting;

        playerTooFarAway =
            Vector3.Distance(transform.position, player.transform.position) >
            circlingRadius + (playerCanNotBeSeen ? 0.5f : 3f);

        // -----------------------------
        // 3. MOVEMENT MODE SWITCHING
        // -----------------------------

        // ENTER NAV MODE
        if (movementMode == CompanionMovementMode.Orbit &&
            (playerTooFarAway || playerCanNotBeSeen) && !movementOverride && agent.enabled)
        {
            EnterNavMove(player.transform.position);
        }

        // EXIT NAV MODE
        if (movementMode == CompanionMovementMode.NavMove &&
            !playerTooFarAway &&
            !playerCanNotBeSeen && !isCarrying && !movementOverride && agent.enabled)
        {
            ExitNavMove();
            return;
        }

        // -----------------------------
        // 4. NAV MODE UPDATE (EARLY OUT)
        // -----------------------------
        if (movementMode == CompanionMovementMode.NavMove)
        {
            if (isPlayingGrabAnim || movementOverride || !agent.enabled)
                return;

            // Update destination only if player moved meaningfully
            if ((agent.destination - player.transform.position).sqrMagnitude > 0.25f)
            {
                agent.SetDestination(player.transform.position);
            }

            // Optional arrival check safeguard
            if (!isCarrying && !agent.pathPending &&
                agent.remainingDistance <= agent.stoppingDistance + 0.1f)
            {
                ExitNavMove();
            }

            if (isCarrying)
            {
                carryBobTime += Time.deltaTime * bobbingSpeed;
                agent.baseOffset = 3.5f + Mathf.Sin(carryBobTime) * bobbingStrength * 0.3f;
            }

            return; // NOTHING BELOW THIS RUNS
        }

        // -----------------------------
        // 5. ORBIT / MANUAL MOVEMENT
        // -----------------------------

        AnimatorStateInfo info = _animator.GetCurrentAnimatorStateInfo(0);
        if (playingIdleAnim && info.IsTag("Idle"))
        {
            ResetAnimValues();
            playingIdleAnim = false;
        }

        if (!movementOverride && shouldMove)
        {
            ApplyBobbing();

            if (isCircling)
                UpdateCirclingMovement();
            else
                UpdateIdleFollow();
        }

        // -----------------------------
        // 6. MISC / UI / IDLE BEHAVIOR
        // -----------------------------
        //DrawUI.Draw(
        //    $"ForcedTarget: {forcedWorldTarget.HasValue}",
        //    new Vector2(Screen.width * 0.65f, Screen.height * 0.3f),
        //    Color.white,
        //    8
        //);

        clearOverhead = !Physics.Raycast(
            transform.position,
            Vector3.up,
            6f,
            LayerMask.GetMask("Default", "Water", "Enemy", "LightReflector", "Pushable")
        );

        if (!movementOverride && !isLooking)
        {
            idleAnimTimer += Time.deltaTime;

            if (idleAnimTimer >= ActionInterval)
            {
                idleAnimTimer = 0f;
                PickAction();
            }
        }
    }

    public void EnterNavMove(Vector3 destination, float stoppingDistance = 0f)
    {
        if (movementMode == CompanionMovementMode.NavMove)
            return;

        movementMode = CompanionMovementMode.NavMove;

        // HARD STOP all manual systems
        StopMovement();
        forcedWorldTarget = null;
        shouldMove = false;

        // Reset agent state
        agent.enabled = true;
        agent.ResetPath();
        agent.isStopped = false;
        agent.SetDestination(destination);
        agent.stoppingDistance = stoppingDistance;
    }
    public void ExitNavMove(bool disableAgent = true)
    {
        agent.isStopped = true;
        agent.ResetPath();
        if (disableAgent)
        {
            agent.enabled = false;
        }

        movementMode = CompanionMovementMode.Orbit;

        // Re-enter orbit smoothly
        MoveToClosestProbe();
        shouldMove = true;
        ResumeMovement();
    }

    private void UpdateOrbitProbes()
    {
        Vector3 center = player.transform.position;
        float probeY = center.y + baseYHeight;

        for (int i = 0; i < orbitProbeCount; i++)
        {
            float angle = (i / (float)orbitProbeCount) * Mathf.PI * 2f;

            Vector3 pos =
                center +
                new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * circlingRadius;

            pos.y = probeY;

            orbitProbeWorld[i] = pos;

            orbitBlocked[i] = Physics.CheckCapsule(
                pos + (0.5f * Vector3.up),
                pos - (0.5f * Vector3.up),
                probeRadius,
                obstacleMask,
                QueryTriggerInteraction.Ignore
            );
        }
    }

    private void UpdateGhostProbes()
    {
        Vector3 center = player.transform.position;
        float probeY = center.y + baseYHeight;

        for (int i = 0; i < ghostWallCheckProbeCount; i++)
        {
            float angle = (i / (float)ghostWallCheckProbeCount) * Mathf.PI * 2f;

            Vector3 pos =
                center +
                new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * normalCirclingRadius;

            pos.y = probeY;

            ghostOrbitProbeWorld[i] = pos;

            ghostOrbitBlocked[i] = Physics.CheckCapsule(
                pos + (0.5f * Vector3.up),
                pos - (0.5f * Vector3.up),
                probeRadius,
                obstacleMask,
                QueryTriggerInteraction.Ignore
            );
        }
    }

    int GetCurrentProbeIndex()
    {
        float normalized = Mathf.Repeat(orbitAngle, Mathf.PI * 2f);
        float percent = normalized / (Mathf.PI * 2f);
        return Mathf.RoundToInt(percent * orbitProbeCount) % orbitProbeCount;
    }

    bool HasFreeRun(int startIndex, int direction, int requiredFree)
    {
        int count = 0;

        for (int i = 1; i <= requiredFree; i++)
        {
            int idx = (startIndex + i * direction + orbitProbeCount) % orbitProbeCount;

            if (orbitBlocked[idx])
                break;

            count++;
        }

        return count >= requiredFree;
    }

    void TryRelocateToFreeProbe()
    {
        for (int i = 0; i < orbitProbeCount; i++)
        {
            if (orbitBlocked[i])
                continue;

            Vector3 target = orbitProbeWorld[i];
            Vector3 dir = target - transform.position;

            if (!Physics.Raycast(
                transform.position,
                dir.normalized,
                dir.magnitude,
                obstacleMask))
            {
                orbitAngle = (i / (float)orbitProbeCount) * Mathf.PI * 2f;
                forcedWorldTarget = target;
                return;
            }
        }

        // No free probe reachable -> fallback
        MoveToPlayerXZ();
    }
    void MoveToPlayerXZ()
    {
        Vector3 pos = player.transform.position;
        pos.y = transform.position.y;

        forcedWorldTarget = pos;
    }

    private void MoveToClosestProbe()
    {
        // Temporarily prevent circling / look logic

        int bestIndex = -1;
        float bestDistSq = float.PositiveInfinity;
        Vector3 from = transform.position;

        for (int i = 0; i < orbitProbeCount; i++)
        {
            if (orbitBlocked[i])
                continue;

            Vector3 target = orbitProbeWorld[i];
            Vector3 dir = target - from;
            float distSq = dir.sqrMagnitude;

            if (distSq < 0.001f)
                continue;

            if (Physics.Raycast(
                from,
                dir.normalized,
                Mathf.Sqrt(distSq),
                obstacleMask))
            {
                continue;
            }

            if (distSq < bestDistSq)
            {
                bestDistSq = distSq;
                bestIndex = i;
            }
        }

        // No valid probe
        if (bestIndex == -1)
        {
            MoveToPlayerXZ();
            return;
        }

        // Request relocation
        orbitAngle = (bestIndex / (float)orbitProbeCount) * Mathf.PI * 2f;
        forcedWorldTarget = orbitProbeWorld[bestIndex];
    }

    private IEnumerator BehaviorLoop()
    {
        while (true)
        {
            while (isReturning || !shouldMove || playerSprinting)
                yield return null;

            isCircling = true;

            circlingTimer = minCirclingTime;
            while (circlingTimer > 0f)
            {
                circlingTimer -= Time.deltaTime;
                timeSinceLastLookLoop += Time.deltaTime;
                yield return null;
            }
            circlingTimer = minCirclingTime;

            if (UnityEngine.Random.value <= chanceToStopCircling || timeSinceLastLookLoop > maxCirclingTime && chanceToStopCircling != 0f)
            {
                if (!prevCirclingSpeed.HasValue)
                {
                    prevCirclingSpeed = circlingSpeed;
                }

                ReanchorBobPhase(bobbingStrength * 0.3f);

                float duration = 0.5f;
                float t = 0f;

                while (t < 1f)
                {
                    t += Time.deltaTime / duration;
                    t = Mathf.Clamp01(t);
                    circlingSpeed = Mathf.Lerp(prevCirclingSpeed.Value, 0f, t);
                    yield return null;
                }
                circlingSpeed = 0f;
                isCircling = false;
                isLooking = true;
                _animator.SetBool("Looking", true);

                yield return StartCoroutine(HandlePauseAndLook());
            }
        }
    }

    void UpdateCirclingMovement()
    {
        int currentProbe = GetCurrentProbeIndex();

        if (orbitBlocked[currentProbe])
        {
            TryRelocateToFreeProbe();
            return;
        }

        bool forwardOK = HasFreeRun(currentProbe, orbitDirection, minFreeTravelOrbitProbeCount);
        bool backwardOK = HasFreeRun(currentProbe, -orbitDirection, minFreeTravelOrbitProbeCount);

        if (!forwardOK)
        {
            if (backwardOK)
            {
                orbitDirection *= -1;
            }
            else
            {
                // Neither direction viable
                isCircling = false;
                return;
            }
        }

        float speed = origCirclingSpeed;
        float radius = normalCirclingRadius;

        if (overrideCirclingSpeed.HasValue)
        {
            speed = overrideCirclingSpeed.Value;
        }
        else
        {
            speed = origCirclingSpeed;
        }

        if (playerMoving)
        {
            if (player.Sprinting)
            {
                speed = sprintingCirclingSpeed;
                radius = sprintingCirclingRadius;
            }
            else
            {
                speed = walkingCirclingSpeed;
                radius = normalCirclingRadius;
            }
        }
        if (ghostWallCheckProbesBlocked >= ghostWallCheckProbeCount * 0.5f)
        {
            radius = normalCirclingRadius * 0.65f;
        }

        circlingSpeed = speed;
        orbitAngle += circlingSpeed * orbitDirection * Time.deltaTime;
        circlingRadius = radius;

        Vector3 desiredPos;

        if (forcedWorldTarget.HasValue)
        {
            Vector3 pos = transform.position;
            pos.y = forcedWorldTarget.Value.y;
            if ((pos - forcedWorldTarget.Value).sqrMagnitude < 0.5f)
            {
                if (probesBlocked < orbitProbeCount)
                {
                    forcedWorldTarget = null;
                }
                if (disableAgentOnReachTarget)
                {
                    agent.enabled = false;
                    disableAgentOnReachTarget = false;
                }
            }
        }

        if (forcedWorldTarget.HasValue)
        {
            desiredPos = forcedWorldTarget.Value;
        }
        else
        {
            desiredPos = player.transform.position + GetOrbitOffset(orbitAngle);
        }

        desiredPos = Vector3.SmoothDamp(
            transform.position,
            desiredPos,
            ref followVelocity,
            1f / followLerpSpeed
        );
        desiredPos.y = GetCurrentBobbingY(GetBobbingStrengthMultiplier());
        transform.position = desiredPos;

        agent.baseOffset = transform.position.y - player.transform.position.y;

        // Look / return has priority
        if (isLooking || isReturning)
            return;

        Vector3 forward;

        // Sprint override
        if (player.Sprinting && playerMoving)
        {
            Vector3 desired = player.transform.forward;
            desired.y = 0f;

            if (desired.sqrMagnitude > 0.001f)
            {
                sprintFacing = Vector3.Slerp(
                    sprintFacing,
                    desired.normalized,
                    rotationSpeed * 0.8f * Time.deltaTime
                );

                forward = sprintFacing;
            }
            else
            {
                return;
            }
        }
        // Orbit tangent
        else
        {
            forward = GetOrbitTangent(orbitAngle);
        }

        if (forward.sqrMagnitude < 0.001f)
            return;

        Quaternion target = Quaternion.LookRotation(forward.normalized);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            target,
            rotationSpeed * Time.deltaTime
        );
    }
    void UpdateIdleFollow()
    {
        Vector3 pos;
        int currentProbe = GetCurrentProbeIndex();

        if (orbitBlocked[currentProbe])
        {
            TryRelocateToFreeProbe();
            pos = forcedWorldTarget.Value;
        }
        else
        {
            pos = orbitProbeWorld[GetCurrentProbeIndex()];
        }

        Vector3 finalPos = Vector3.SmoothDamp(
            transform.position,
            pos,
            ref followVelocity,
            1f / followLerpSpeed
        );
        finalPos.y = GetCurrentBobbingY(GetBobbingStrengthMultiplier());
        transform.position = finalPos;
    }

    private void ApplyBobbing()
    {
        bobTime += Time.deltaTime * bobbingSpeed;
    }
    private float GetCurrentBobbingY(float strengthMultiplier = 1f)
    {
        return player.transform.position.y
            + baseYHeight
            + Mathf.Sin(bobTime) * bobbingStrength * strengthMultiplier;
    }
    float GetBobbingStrengthMultiplier()
    {
        if (isLooking && !isCircling)
            return 0.3f;

        return 1f;
    }

    IEnumerator HandlePauseAndLook()
    {
        forcedWorldTarget = null;

        int lookCount = UnityEngine.Random.Range(
            lookDirectionCountRange.x,
            lookDirectionCountRange.y + 1
        );

        yield return new WaitForSeconds(0.5f);

        for (int i = 0; i < lookCount; i++)
        {
            float targetYaw = GetValidRandomYaw();
            float duration = UnityEngine.Random.Range(
                lookDurationRange.x,
                lookDurationRange.y
            );

            yield return RotateToYaw(targetYaw);
            yield return new WaitForSeconds(duration);
        }

        if (UnityEngine.Random.value <= chanceToFlipOrbitDirection)
        {
            orbitDirection *= -1;
        }

        isCircling = true;
        isReturning = true;
        yield return ReturnToOrbit();
    }

    IEnumerator ReturnToOrbit(float returnDuration = 1.25f)
    {
        ReanchorBobPhase(bobbingStrength);

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        float prevValue;
        if (prevCirclingSpeed.HasValue)
        {
            prevValue = prevCirclingSpeed.Value;
        }
        else
        {
            prevValue = origCirclingSpeed;
        }

        float duration = returnDuration;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            t = Mathf.Clamp01(t);

            circlingSpeed = Mathf.Lerp(0f, prevValue, t);

            // Target orbit position (XZ only)
            Vector3 orbitXZ = player.transform.position + GetOrbitOffset(orbitAngle);

            Vector3 blendedPos = Vector3.Lerp(
                new Vector3(startPos.x, 0f, startPos.z),
                new Vector3(orbitXZ.x, 0f, orbitXZ.z),
                t
            );

            // Apply live bobbing Y (NO lerp)
            blendedPos.y = GetCurrentBobbingY();

            transform.position = blendedPos;

            // Rotation blend
            Quaternion targetRot = Quaternion.LookRotation(GetOrbitTangent(orbitAngle));
            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);

            yield return null;
        }

        circlingSpeed = prevValue;
        prevCirclingSpeed = null;
        isReturning = false;
        isLooking = false;
        _animator.SetBool("Looking", false);
    }

    void ReanchorBobPhase(float strength)
    {
        float offset =
            transform.position.y
            - (player.transform.position.y + baseYHeight);

        if (Mathf.Abs(strength) < 0.0001f)
            return;

        float normalized = Mathf.Clamp(offset / strength, -1f, 1f);
        float candidate = Mathf.Asin(normalized);

        // Two possible sine angles
        float alt = Mathf.PI - candidate;

        // Pick closest to current phase
        bobTime = Mathf.Abs(candidate - bobTime) <
                  Mathf.Abs(alt - bobTime)
            ? candidate
            : alt;
    }

    private IEnumerator RotateToYaw(float targetYaw)
    {
        Quaternion start = transform.rotation;
        Quaternion end = Quaternion.Euler(0f, targetYaw, 0f);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * rotationSpeed;
            transform.rotation = Quaternion.Slerp(start, end, t);
            yield return null;
        }
    }

    private float GetValidRandomYaw()
    {
        float currentYaw = transform.eulerAngles.y;

        for (int i = 0; i < 10; i++)
        {
            float randomYaw = UnityEngine.Random.Range(0f, 360f);
            float delta = Mathf.Abs(Mathf.DeltaAngle(currentYaw, randomYaw));

            if (delta >= minLookAngleDelta)
                return randomYaw;
            if (IsYawNearPlayer(delta))
                continue;
        }

        // Fallback if unlucky
        return currentYaw + minLookAngleDelta;
    }

    bool IsYawNearPlayer(float yaw, float tolerance = 10f)
    {
        float playerYaw = GetYawToPlayerXZ();
        float delta = Mathf.Abs(Mathf.DeltaAngle(playerYaw, yaw));
        return delta <= tolerance;
    }
    float GetYawToPlayerXZ()
    {
        Vector3 toPlayer = player.transform.position - transform.position;
        toPlayer.y = 0f;

        if (toPlayer.sqrMagnitude < 0.0001f)
            return transform.eulerAngles.y;

        return Mathf.Atan2(toPlayer.x, toPlayer.z) * Mathf.Rad2Deg;
    }


    public Vector3 GetOrbitOffset(float angle)
    {
        return new Vector3(
            Mathf.Cos(angle),
            0f,
            Mathf.Sin(angle)
        ) * circlingRadius;
    }

    public Vector3 GetOrbitTangent(float angle)
    {
        float delta = 0.01f * orbitDirection;
        Vector3 p1 = GetOrbitOffset(angle);
        Vector3 p2 = GetOrbitOffset(angle + delta);
        return (p2 - p1).normalized;
    }
    public Vector3 GetOrbitTangent(float angle, float direction)
    {
        float delta = 0.01f * direction;
        Vector3 p1 = GetOrbitOffset(angle);
        Vector3 p2 = GetOrbitOffset(angle + delta);
        return (p2 - p1).normalized;
    }

    public void StopMovement()
    {
        if (behaviorRoutine != null)
        {
            StopCoroutine(behaviorRoutine);
            behaviorRoutine = null;
        }

        if (!playerMoving && !playerSprinting)
        {
            prevCirclingSpeed = circlingSpeed;
        }
        isCircling = false;
        isReturning = false;
        _animator.SetBool("Looking", false);
    }

    public void ResumeMovement(bool onlyStartCoroutine = false)
    {
        if (!onlyStartCoroutine)
        {
            isLooking = false;
            isReturning = false;
            isCircling = true;
        }

        if (behaviorRoutine == null)
            behaviorRoutine = StartCoroutine(BehaviorLoop());

        if (!onlyStartCoroutine)
        {
            if (prevCirclingSpeed.HasValue)
            {
                circlingSpeed = prevCirclingSpeed.Value;
            }
            else if (circlingSpeed <= 0.05f)
            {
                circlingSpeed = origCirclingSpeed;
            }
        }
    }

    public bool PickAction()
    {
        float totalWeight = 0f;

        foreach (var action in IdleActions)
            if (action.CanUse == null || action.CanUse())
                totalWeight += action.Weight;

        if (totalWeight <= 0f)
            return false;

        float choice = UnityEngine.Random.value * totalWeight;
        float cumulative = 0f;

        foreach (var action in IdleActions)
        {
            if (action.CanUse == null || action.CanUse())
            {
                cumulative += action.Weight;
                if (choice <= cumulative)
                {
                    foreach (string data in action.CustomData)
                    {
                        string modifier;
                        float value = 1f;

                        if (data.Contains(';'))
                        {
                            var parts = data.Split(';').ToArray();
                            modifier = parts[0];
                            float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out value);
                        }
                        else
                        {
                            modifier = data;
                        }

                        switch (modifier)
                        {
                            case "stop":
                                shouldMove = false;
                                break;

                            case "slow":
                                overrideCirclingSpeed = circlingSpeed * value;
                                break;

                            case "change_wings":
                                _animator.SetLayerWeight(1, value);
                                break;

                            case "reset_look_timer":
                                circlingTimer = minCirclingTime;
                                break;

                            default:
                                break;
                        }
                    }

                    _animator.SetTriggerOneFrame(this, action.TriggerName);
                    playingIdleAnim = true;

                    return true;
                }
            }
        }

        return false;
    }

    private void ResetAnimValues()
    {
        overrideCirclingSpeed = null;
        shouldMove = true;
        _animator.SetLayerWeight(1, 1f);
    }
    private IEnumerator OutOfEnergyShakeWait()
    {
        var clip = _animator.runtimeAnimatorController.animationClips.FirstOrDefault(c => c.name == "Idle_Twitch");
        float totalTime;

        if (clip == null)
        {
            Debug.LogWarning("Idle_Twitch animation not found!");
            totalTime = 0.5f;
        }
        else
        {
            totalTime = clip.length;
        }

        float prevWeight = IdleActions[0].Weight;
        var prevConditions = IdleActions[0].CanUse;

        IdleActions[0].Weight = 99999f;
        IdleActions[0].CanUse = () => !movementOverride;
        _animator.SetLayerWeight(2, 1f);
        _animator.SetBool("IdleAnimOverride", true);

        PickAction();

        IdleActions[0].Weight = prevWeight;
        IdleActions[0].CanUse = prevConditions;

        yield return new WaitForSeconds(totalTime);

        _animator.SetLayerWeight(2, 0f);
        _animator.SetBool("IdleAnimOverride", false);
        outOfEnergyShakeRoutine = null;
    }

    public void StartCarry(Transform target, bool runePuzzle = false)
    {
        if (!isPlayingGrabAnim && !movementOverride)
        {
            Debug.Log("Started carry!");
            if (movementMode == CompanionMovementMode.Orbit)
            {
                EnterNavMove(player.transform.position);
            }

            if (PickUpRoutine != null)
                StopCoroutine(PickUpRoutine);
            PickUpRoutine = StartCoroutine(PickUpObject(target, runePuzzle));
        }
    }

    public void StopCarry(Transform target = null)
    {
        if (!isPlayingGrabAnim && !movementOverride)
        {
            if (PutDownRoutine != null)
                StopCoroutine(PutDownRoutine);
            PutDownRoutine = StartCoroutine(PutDownObject(target));
        }
    }

    private IEnumerator PickUpObject(Transform target, bool runePuzzle = false)
    {
        isCarrying = true;
        isPlayingGrabAnim = true;
        movementOverride = true;
        agent.enabled = true;
        gameObject.GetComponent<Collider>().enabled = false;

        if (BlobShadow != null)
        {
            StartCoroutine(FadeAwayBlobShadow());
        }

        if (runePuzzle)
        {
            IsDoingRunePuzzle = true;
            CanExitRunePuzzleState = false;
        }

        if (outOfEnergyShakeRoutine != null)
        {
            StopCoroutine(outOfEnergyShakeRoutine);
            _animator.SetLayerWeight(2, 0f);
            _animator.SetBool("IdleAnimOverride", false);
        }

        if (playingIdleAnim)
        {
            _animator.SetBool("Looking", true);
            ResetAnimValues();
            yield return null;
            _animator.SetBool("Looking", false);
            playingIdleAnim = false;
        }

        if (!runePuzzle)
        {
            if (TryGetMesh(target, out Mesh mesh))
            {
                Debug.Log("Got mesh!");
                carriedObjectExtentsY = Mathf.Abs(mesh.bounds.min.y) * transform.lossyScale.y;
                print($"{carriedObjectExtentsY}");
            }
        }
        else
        {
            carriedObjectExtentsY = 0f;
            prevCarryOffsetDistance = carryOffsetDistance;
            carryOffsetDistance = 0f;
        }

        if (target.TryGetComponent<Collider>(out Collider col))
        {
            col.enabled = false;
        }

        agent.SetDestination(target.position);
        bool completed = false;

        while (!completed)
        {
            if (!agent.pathPending)
            {
                if (agent.remainingDistance <= agent.stoppingDistance + 0.05f)
                {
                    if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                    {
                        completed = true;
                    }
                }
            }

            yield return null;
        }

        agent.isStopped = true;
        agent.ResetPath();
        agent.enabled = false;

        transform.position = new Vector3(target.position.x, transform.position.y, target.position.z);

        float rotateDuration = 0.5f;
        float rotateTimer = 0f;
        Quaternion startRot = transform.rotation;
        Quaternion targetRot = Quaternion.Euler(startRot.x, target.transform.rotation.y, startRot.z);

        while (rotateTimer < rotateDuration)
        {
            rotateTimer += Time.deltaTime;
            float tRot = Mathf.Clamp01(rotateTimer / rotateDuration);

            transform.rotation = Quaternion.Slerp(startRot, targetRot, tRot);
            yield return null;
        }

        //float startHeight = agent.baseOffset;
        //float endHeight = carryOffsetDistance;

        Vector3 start = transform.position;
        Vector3 end = target.position;
        end.y += carryOffsetDistance;

        float sinkDuration = 0.8f;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / sinkDuration;
            t = Mathf.Clamp01(t);

            transform.position = Vector3.Lerp(start, end, t);
            //agent.baseOffset = Mathf.Lerp(startHeight, endHeight, t);

            yield return null;
        }

        transform.position = end;
        if (!runePuzzle)
        {
            _animator.SetBool("Grabbing", true);
            yield return new WaitForSeconds(1f);

            heldObject = target.gameObject;
            heldObject.transform.SetParent(transform, true);
            heldObjectPrevLayer = heldObject.layer;
            heldObject.layer = LayerMask.NameToLayer("Player");
        }
        else
        {
            _animator.SetBool("RunePuzzle", true);
            _animator.SetLayerWeight(1, 0f);

            yield return new WaitForSeconds(0.25f);
            float glowDuration = 1f;
            float glowTimer = 0f;

            while (glowTimer < glowDuration)
            {
                glowTimer += Time.deltaTime;
                float t3 = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(glowTimer / glowDuration));

                Color newColor = Color.Lerp(OrigCrystalColor, OrigCrystalColor * 1.75f, t3);

                CrystalBallMaterial.SetColor("_EmissionColor", newColor);
                yield return null;
            }
            CrystalBallMaterial.SetColor("_EmissionColor", OrigCrystalColor * 1.75f);

            CanExitRunePuzzleState = true;

            while (IsDoingRunePuzzle)
            {
                yield return null;
            }

            _animator.SetBool("RunePuzzle", false);

            glowTimer = 0f;
            bool activatedWings = false;
            while (glowTimer < glowDuration)
            {
                glowTimer += Time.deltaTime;

                float t3 = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(glowTimer / glowDuration));
                Color newColor = Color.Lerp(OrigCrystalColor * 1.75f, OrigCrystalColor, t3);

                CrystalBallMaterial.SetColor("_EmissionColor", newColor);

                if (t3 >= 0.25f && !activatedWings)
                {
                    _animator.SetLayerWeight(1, 1f);
                    activatedWings = true;
                }

                yield return null;
            }
            CrystalBallMaterial.SetColor("_EmissionColor", OrigCrystalColor);
        }

        Vector3 start2 = transform.position;
        Vector3 end2 = start;

        float riseDuration = 1.2f;
        float t2 = 0f;

        while (t2 < 1f)
        {
            t2 += Time.deltaTime / riseDuration;
            t2 = Mathf.Clamp01(t2);

            transform.position = Vector3.Lerp(start2, end2, t2);

            yield return null;
        }

        if (runePuzzle)
        {
            carriedObjectExtentsY = null;
            isPlayingGrabAnim = false;
            isCarrying = false;
            movementOverride = false;
            agent.enabled = true;
            agent.isStopped = false;
            agent.stoppingDistance = 0f;
            disableAgentOnReachTarget = true;
            ExitNavMove(false);

            if (BlobShadow != null && prevBlobShadowHeight.HasValue)
            {
                StartCoroutine(FadeBackBlobShadow());
            }
        }
        else
        {
            isPlayingGrabAnim = false;
            movementOverride = false;
            agent.enabled = true;
            agent.isStopped = false;

            agentSpeedBeforeGrab = agent.speed;
            agentAccelerationBeforeGrab = agent.acceleration;

            agent.stoppingDistance = 3f;
            agent.speed = 12f;
            agent.acceleration = 12f;
            carryBobTime = 0f;
        }

        gameObject.GetComponent<Collider>().enabled = true;
        PickUpRoutine = null;
    }

    private IEnumerator PutDownObject(Transform target = null)
    {
        bool targetNull = false;

        if (target == null)
            targetNull = true;

        isCarrying = true;
        isPlayingGrabAnim = true;
        movementOverride = true;
        agent.enabled = true;

        if (outOfEnergyShakeRoutine != null)
        {
            StopCoroutine(outOfEnergyShakeRoutine);
            _animator.SetLayerWeight(2, 0f);
            _animator.SetBool("IdleAnimOverride", false);
        }

        if (!targetNull)
        {
            agent.SetDestination(target.position);

            bool completed = false;

            while (!completed)
            {
                if (!agent.pathPending)
                {
                    if (agent.remainingDistance <= agent.stoppingDistance)
                    {
                        if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                        {
                            completed = true;
                        }
                    }
                }

                yield return null;
            }

            agent.isStopped = true;
            agent.ResetPath();

            transform.position = new Vector3(target.position.x, transform.position.y, target.position.z);

            yield return new WaitForSeconds(0.5f);
        }

        agent.isStopped = true;
        agent.ResetPath();
        agent.enabled = false;

        bool isStation = false;
        if (target != null)
        {
            if (target.CompareTag("CarryableStation"))
            {
                isStation = true;
            }
        }

        Vector3 end;

        if (targetNull || !isStation)
        {
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, float.MaxValue, LayerMask.GetMask("Default")))
            {
                end = hit.point;
                Debug.Log($"{hit.collider.gameObject.name}");

                if (carriedObjectExtentsY.HasValue)
                {
                    end.y += carriedObjectExtentsY.Value;
                }
            }
            else // Nowhere to place object, break out of loop
            {
                float totalTime = _animator.runtimeAnimatorController.animationClips.Where(c => c.name == "Idle_Twitch").FirstOrDefault().length;
                _animator.SetLayerWeight(2, 1f);
                _animator.SetTriggerOneFrame(this, "Idle_Twitch");

                yield return new WaitForSeconds(totalTime);
                _animator.SetLayerWeight(2, 0f);

                yield break;
            }
        }
        else if (!targetNull && isStation)
        {
            end = target.position;
        }
        else
        {
            end = transform.position;
            throw new System.Exception("How");
        }

        //float startHeight = agent.baseOffset;
        //float endHeight = carryOffsetDistance;
        Vector3 start = transform.position;
        end.y += carryOffsetDistance;

        float sinkDuration = 0.8f;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / sinkDuration;
            t = Mathf.Clamp01(t);

            transform.position = Vector3.Lerp(start, end, t);
            //agent.baseOffset = Mathf.Lerp(startHeight, endHeight, t);

            yield return null;
        }

        transform.position = end;
        _animator.SetBool("Grabbing", false);

        yield return new WaitForSeconds(1f);

        if (heldObject != null)
        {
            heldObject.transform.SetParent(null, true);
            heldObject.layer = heldObjectPrevLayer;

            if (heldObject.TryGetComponent<Collider>(out Collider col))
            {
                col.enabled = true;
            }

            for (int i = 0; i < SceneManager.loadedSceneCount; i++)
            {
                string sceneName = SceneManager.GetSceneAt(i).name;
                if (sceneName != "DeathScene" && sceneName != "MainMenu")
                {
                    SceneManager.MoveGameObjectToScene(heldObject, SceneManager.GetSceneAt(i));
                    heldObject = null;
                    break;
                }
            }
        }

        //float startHeight2 = agent.baseOffset;
        //float endHeight2 = 3.5f;
        Vector3 start2 = transform.position;
        Vector3 end2 = start;

        float riseDuration = 0.8f;
        float t2 = 0f;

        while (t2 < 1f)
        {
            t2 += Time.deltaTime / riseDuration;
            t2 = Mathf.Clamp01(t2);

            transform.position = Vector3.Lerp(start2, end2, t2);
            //agent.baseOffset = Mathf.Lerp(startHeight2, endHeight2, t2);

            yield return null;
        }

        carriedObjectExtentsY = null;
        isPlayingGrabAnim = false;
        isCarrying = false;
        movementOverride = false;
        agent.enabled = true;
        agent.isStopped = false;
        agent.stoppingDistance = 0f;
        agent.speed = agentSpeedBeforeGrab;
        agent.acceleration = agentAccelerationBeforeGrab;
        disableAgentOnReachTarget = true;
        ExitNavMove(false);

        if (BlobShadow != null && prevBlobShadowHeight.HasValue)
        {
            StartCoroutine(FadeBackBlobShadow());
        }

        PutDownRoutine = null;
    }

    bool TryGetMesh(Transform t, out Mesh mesh)
    {
        // MeshFilter
        if (t.gameObject.TryGetComponent(out MeshFilter mf) && mf.sharedMesh != null)
        {
            mesh = mf.sharedMesh;
            return true;
        }

        // Skinned mesh
        if (t.gameObject.TryGetComponent(out SkinnedMeshRenderer smr) && smr.sharedMesh != null)
        {
            mesh = smr.sharedMesh;
            return true;
        }

        // Children
        foreach (Transform child in t)
        {
            if (TryGetMesh(child, out mesh))
                return true;
        }

        mesh = null;
        return false;
    }

    private IEnumerator FadeAwayBlobShadow()
    {
        prevBlobShadowHeight = BlobShadow.maxAirHeight;

        float fadeDuration = 0.3f;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / fadeDuration;
            t = Mathf.Clamp01(t);

            if (BlobShadow != null)
            {
                float height = Mathf.Lerp(prevBlobShadowHeight.Value, 0f, t);
                BlobShadow.maxAirHeight = height;
            }

            yield return null;
        }
    }
    private IEnumerator FadeBackBlobShadow()
    {
        float startHeight = BlobShadow.maxAirHeight;
        float fadeDuration = 0.3f;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / fadeDuration;
            t = Mathf.Clamp01(t);

            if (BlobShadow != null)
            {
                float height = Mathf.Lerp(startHeight, prevBlobShadowHeight.HasValue ? prevBlobShadowHeight.Value : 12f, t);
                BlobShadow.maxAirHeight = height;
            }

            yield return null;
        }
    }

    public void SpearAttack()
    {
        if (UserInput.SpearAttackPressed && canAttack)
        {
            if (TryAttack(2))
            {
                SpearOffset = GetRandomSpawnPosition(transform, out var spawnState);

                GameObject instance = Instantiate(Spear, SpearOffset, Quaternion.identity);
                var spearAttack = instance.GetComponent<SpearAttackScript>();
                spearAttack.State = spawnState;

                previousSpears.Add(spearAttack);

                if (attackCooldownRoutine == null)
                {
                    StartCoroutine(AttackCooldown(spearAttackCooldown));
                }
                else
                {
                    StopCoroutine(attackCooldownRoutine);
                    attackCooldownRoutine = StartCoroutine(AttackCooldown(spearAttackCooldown));
                }
            }
        }
    }

    public void SlamAttack()
    {
        if (UserInput.SlamAttackPressed && canAttack && !SlamAttacking && !player.Parrying && !player.Pushing && player.Grounded)
        {
            if (TryAttack(2))
            {
                StartCoroutine(SlamAttackRoutine());

                if (attackCooldownRoutine == null)
                {
                    StartCoroutine(AttackCooldown(slamCooldown));
                }
                else
                {
                    StopCoroutine(attackCooldownRoutine);
                    attackCooldownRoutine = StartCoroutine(AttackCooldown(slamCooldown));
                }
            }
        }
    }

    public bool TryAttack(int energyCost)
    {
        if (player.Energy >= energyCost)
        {
            player.ConsumeEnergy(energyCost);
            return true;
        }
        else
        {
            if (outOfEnergyShakeRoutine != null)
            {
                StopCoroutine(outOfEnergyShakeRoutine);
                _animator.SetLayerWeight(2, 0f);
                _animator.SetBool("IdleAnimOverride", false);
            }
            outOfEnergyShakeRoutine = StartCoroutine(OutOfEnergyShakeWait());
            return false;
        }
    }

    private void CollectEnergy(Vector3 senderPos)
    {
        GameObject prefab = VFX.Construct_GainEnergy;

        StartCoroutine(EnergyCollectEffectTimer(0.5f, prefab, transform, senderPos, 1f));
    }

    private IEnumerator EnergyCollectEffectTimer(float time, GameObject instance, Transform transform, Vector3 senderPos, float lifetime)
    {
        yield return new WaitForSeconds(time);

        var instance2 = Instantiate(instance, transform.position, transform.rotation, transform);

        float timer = 0f;
        while (timer < lifetime)
        {
            timer += Time.deltaTime;

            Vector3 dir = senderPos - transform.position;
            Quaternion rotation = Quaternion.LookRotation(dir);
            rotation *= Quaternion.Euler(0f, -90f, 0f);

            instance2.transform.rotation = rotation;

            yield return null;
        }
        Destroy(instance2);
    }

    private IEnumerator SlamAttackRoutine()
    {
        SlamAttacking = true;
        StopMovement();

        if (outOfEnergyShakeRoutine != null)
        {
            StopCoroutine(outOfEnergyShakeRoutine);
            _animator.SetLayerWeight(2, 0f);
            _animator.SetBool("IdleAnimOverride", false);
        }

        if (playingIdleAnim)
        {
            _animator.SetBool("Looking", true);
            ResetAnimValues();
            yield return null;
            _animator.SetBool("Looking", false);
            playingIdleAnim = false;
        }

        player.animator.SetBool("SlamAttacking", true);
        _animator.SetBool("Looking", false);

        movementOverride = true;
        agent.enabled = false;

        Vector3 start = transform.position;
        Vector3 target = player.transform.position + new Vector3(0f, 7.5f, 0f);

        Vector3 startXZ = new Vector3(start.x, 0f, start.z);
        Vector3 targetXZ = new Vector3(target.x, 0f, target.z);

        float startY = start.y;
        float targetY = target.y;

        float duration = 0.5f;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            t = Mathf.Clamp01(t);

            float yWeight = t * t * t;      // cubic rise
            float xzWeight = 1f - yWeight;

            Vector3 xzPos = Vector3.Lerp(startXZ, targetXZ, t);
            float yPos = Mathf.Lerp(startY, targetY, yWeight);

            transform.position = new Vector3(
                xzPos.x,
                yPos,
                xzPos.z
            );

            yield return null;
        }

        transform.position = target;

        yield return new WaitForSeconds(0.1f);

        ParticleSpawner.Spawn(Particles.P_SlamAttack, player.transform.position);

        Vector3 start2 = transform.position;
        Vector3 target2 = player.transform.position + new Vector3(0f, 2f, 0f);
        float t2 = 0f;

        while (t2 < 1f)
        {
            t2 += Time.deltaTime / 0.1f;
            t2 = Mathf.Clamp01(t2);

            transform.position = Vector3.Lerp(start2, target2, t2);

            yield return null;
        }

        Freezer.Freeze(0.075f);
        yield return new WaitForSeconds(0.075f);

        movementOverride = false;
        CameraActions.Main.Shake(0.3f, 0.15f, 0.1f);

        Collider[] enemyColliders = Physics.OverlapSphere(
            player.transform.position,
            slamShockwaveRadius,
            LayerMask.GetMask("Enemy")
        );

        HashSet<Enemy> affectedEnemies = new HashSet<Enemy>();

        foreach (var col in enemyColliders)
        {
            if (col == null)
                continue;

            Enemy enemy = col.GetComponentInParent<Enemy>();
            if (enemy == null)
            {
                enemy = GetComponent<Enemy>();
                if (enemy == null)
                    continue;
            }

            // Skip if we've already handled this enemy
            if (!affectedEnemies.Add(enemy))
                continue;

            Vector3 pushDir = player.transform.position - enemy.transform.position;
            Vector3 final = new Vector3(-pushDir.x, 0, -pushDir.z);

            enemy.ApplyPushback(final, 12f, 0.2f);
            enemy.Stun(4f);
        }

        Collider[] breakableCollider = Physics.OverlapSphere(
            player.transform.position,
            slamShockwaveRadius - 1,
            LayerMask.GetMask("Default")
        );

        foreach (var col in breakableCollider)
        {
            if (col.TryGetComponent<WallCrack>(out WallCrack crack))
            {
                crack.Break();
            }
        }

        yield return new WaitForSeconds(0.5f);
        agent.enabled = true;
        player.animator.SetBool("SlamAttacking", false);
        SlamAttacking = false;
        ResumeMovement();
    }

    Vector3 GetRandomSpawnPosition(Transform origin, out SpearAttackScript.SpearSpawnState spawnState)
    {
        bool hasLeft = previousSpears.Exists(s => s.State == SpearAttackScript.SpearSpawnState.Left);
        bool hasRight = previousSpears.Exists(s => s.State == SpearAttackScript.SpearSpawnState.Right);
        bool hasTop = previousSpears.Exists(s => s.State == SpearAttackScript.SpearSpawnState.Top);

        SpearAttackScript.SpearSpawnState chosenState;

        if (!hasLeft && !hasRight)
        {
            chosenState = UnityEngine.Random.value < 0.5f ? SpearAttackScript.SpearSpawnState.Left : SpearAttackScript.SpearSpawnState.Right;
        }
        else if (hasLeft && !hasRight)
        {
            chosenState = SpearAttackScript.SpearSpawnState.Right;
        }
        else if (!hasLeft && hasRight)
        {
            chosenState = SpearAttackScript.SpearSpawnState.Left;
        }
        else if (!hasTop)
        {
            chosenState = SpearAttackScript.SpearSpawnState.Top;
        }
        else
        {
            float val = UnityEngine.Random.Range(0f, 9f);
            if (val <= 3f)
            {
                if (lastState != SpearAttackScript.SpearSpawnState.Left)
                {
                    chosenState = SpearAttackScript.SpearSpawnState.Left;
                }
                else
                {
                    chosenState = SpearAttackScript.SpearSpawnState.Right;
                }
            }
            else if (val <= 6f && val > 3f)
            {
                if (lastState != SpearAttackScript.SpearSpawnState.Right)
                {
                    chosenState = SpearAttackScript.SpearSpawnState.Right;
                }
                else
                {
                    chosenState = SpearAttackScript.SpearSpawnState.Left;
                }
            }
            else
            {
                if (lastState != SpearAttackScript.SpearSpawnState.Top)
                {
                    chosenState = SpearAttackScript.SpearSpawnState.Top;
                }
                else
                {
                    chosenState = SpearAttackScript.SpearSpawnState.Left;
                }
            }
        }

            // Define cube dimensions
        float halfWidth = 4f / 2f;
        float halfDepth = 2f / 2f;
        float halfHeight = 4f / 2f;
        float distance = 3f;

        // Random local offset inside cube
        float offsetX = UnityEngine.Random.Range(-halfWidth, halfWidth);
        float offsetY = UnityEngine.Random.Range(-halfHeight, halfHeight);
        float offsetZ = UnityEngine.Random.Range(-halfDepth, halfDepth);

        // Shift the cube depending on chosen side
        switch (chosenState)
        {
            case SpearAttackScript.SpearSpawnState.Left:
                offsetX -= (halfWidth + distance);
                break;
            case SpearAttackScript.SpearSpawnState.Right:
                offsetX += (halfWidth + distance);
                break;
            case SpearAttackScript.SpearSpawnState.Top:
                offsetY += (halfHeight + distance);
                break;
        }

        // Convert local offset to world space
        Vector3 localOffset = new Vector3(offsetX, offsetY, offsetZ);
        spawnState = chosenState;
        lastState = chosenState;
        return origin.TransformPoint(localOffset);
    }

    private IEnumerator AttackCooldown(float amount)
    {
        canAttack = false;
        yield return new WaitForSeconds(amount);
        canAttack = true;
    }

    public IEnumerator DoorEntranceAnimation(Vector3 spawnPos, Vector3 targetPos, Vector3 direction, bool shouldPlaySpinAnim)
    {
        StopMovement();
        _animator.SetBool("Looking", false);
        _animator.SetBool("IdleAnimOverride", false);
        _animator.Play("Fly", 0);
        isPlayingEntranceAnim = true;
        agent.enabled = false;
        previousSpears = new List<SpearAttackScript>();
        previousSpears.Clear();
        circlingSpeed = origCirclingSpeed;
        prevCirclingSpeed = null;

        transform.position = spawnPos;
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

        while (SceneFadeManager.instance.IsFadingIn)
        {
            yield return null;
        }

        Vector3 start = spawnPos + (direction * 2f);
        Vector3 target = targetPos;

        Vector3 startXZ = new Vector3(start.x, 0f, start.z);
        Vector3 targetXZ = new Vector3(target.x, 0f, target.z);

        float startY = start.y;
        float targetY = target.y;

        float duration = shouldPlaySpinAnim ? 0.45f : 0.6f;
        float t = 0f;

        bool playedSpinAnim = false;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            t = Mathf.Clamp01(t);

            float yWeight = t * t * t;      // cubic rise
            float xzWeight = 1f - yWeight;

            float finalTargetY;
            if (shouldPlaySpinAnim)
            {
                float dip = Mathf.Sin(t * Mathf.PI);
                finalTargetY = targetY - 1f * dip;
            }
            else
            {
                finalTargetY = targetY;
            }

            Vector3 xzPos = Vector3.Lerp(startXZ, targetXZ, t);
            float yPos = Mathf.Lerp(startY, finalTargetY, yWeight);

            Vector3 desiredPos = new Vector3(
                xzPos.x,
                yPos,
                xzPos.z
            );

            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPos,
                ref followVelocity,
                0.15f,        // smaller = snappier
                Mathf.Infinity
            );

            if (t >= 0.5f && !playedSpinAnim && shouldPlaySpinAnim)
            {
                playedSpinAnim = true;
                _animator.speed *= 1.2f;
                _animator.Play("Idle_Spin", 0);
                _animator.SetLayerWeight(1, 0f);
            }

            yield return null;
        }

        float inertiaTime = shouldPlaySpinAnim ? 0.55f : 0.75f;
        float inertiaT = 0f;

        while (inertiaT < inertiaTime)
        {
            inertiaT += Time.deltaTime;

            transform.position += followVelocity * Time.deltaTime;

            // natural damping
            followVelocity = Vector3.Lerp(followVelocity, Vector3.zero, 4f * Time.deltaTime);

            yield return null;
        }

        if (shouldPlaySpinAnim)
        {
            yield return new WaitForSeconds(0.6f); // Small wait for animation finish
            _animator.speed *= 1f / 1.2f;
            _animator.SetLayerWeight(1, 1f);
        }

        Vector3 start2 = transform.position;
        Vector3 target2 = player.transform.position;

        float duration2 = shouldPlaySpinAnim ? 0.55f : 0.75f;
        float t2 = 0f;

        while (t2 < 1f)
        {
            t2 += Time.deltaTime / duration2;
            t2 = Mathf.Clamp01(t2);

            MoveToClosestProbe();

            if (forcedWorldTarget.HasValue)
            {
                target2 = forcedWorldTarget.Value;
            }
            else
            {
                target2 = player.transform.position;
            }

            Vector3 pos = Vector3.Lerp(start2, target2, t2);

            pos = Vector3.SmoothDamp(
                transform.position,
                pos,
                ref followVelocity,
                0.15f,
                Mathf.Infinity
            );

            transform.position = pos;

            yield return null;
        }

        yield return ReturnToOrbit(0.25f);

        isLooking = false;
        isReturning = false;
        isCircling = true;

        ResumeMovement(true);

        isPlayingEntranceAnim = false;
        agent.enabled = true;

        DoorEntranceAnimRoutine = null;
    }

    void OnDrawGizmos()
    {
        if (orbitProbeWorld == null)
            return;

        for (int i = 0; i < orbitProbeCount; i++)
        {
            Color color = orbitBlocked[i] ? Color.red : Color.green;
            DrawMethods.DrawCapsuleGizmo(
                orbitProbeWorld[i] + (Vector3.up * 0.5f),
                orbitProbeWorld[i] - (Vector3.up * 0.5f),
                probeRadius,
                color);
        }

        if (!drawGhostProbesGizmo)
            return;

        if (ghostOrbitProbeWorld == null)
            return;

        for (int i = 0; i < ghostWallCheckProbeCount; i++)
        {
            Color prev = Gizmos.color;
            Gizmos.color = ghostOrbitBlocked[i] ? Color.yellow : Color.blue;
            Gizmos.DrawWireSphere(ghostOrbitProbeWorld[i], probeRadius);
            Gizmos.color = prev;
        }
    }
}
