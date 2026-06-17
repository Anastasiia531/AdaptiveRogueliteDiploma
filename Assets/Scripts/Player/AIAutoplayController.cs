using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIAutoplayController : MonoBehaviour
{
    private Player player;
    private HashSet<Vector2> visitedRooms = new HashSet<Vector2>();
    private Vector2 lastRoomCoord = new Vector2(-999, -999);
    private float challengeStateTimer = 0f;
    private Transform currentDoorTarget = null;
    private float doorRecheckTimer = 0f;

    private void Awake()
    {
        player = GetComponent<Player>();
    }

    private void Start()
    {
        // Add ourselves to the player's GameObject
        if (player == null)
        {
            player = FindObjectOfType<Player>();
        }
    }

    private void Update()
    {
        if (player == null || !player.isLive || !player.isControllable)
        {
            ResetAIInputs();
            return;
        }

        if (player.controlMode == Player.PlayerControlMode.Human)
        {
            ResetAIInputs();
            return;
        }

        Level level = GameManager.Instance != null ? GameManager.Instance.level : null;
        Room currentRoom = level != null ? level.currentRoom : null;

        if (currentRoom == null)
        {
            ResetAIInputs();
            return;
        }

        // Track visited rooms to enable smart map traversal
        if (currentRoom.coordinate != lastRoomCoord)
        {
            visitedRooms.Add(currentRoom.coordinate);
            lastRoomCoord = currentRoom.coordinate;
            currentDoorTarget = null; // Reset door target on new room
        }

        // 1. Check for active enemies
        List<Transform> activeMonsters = GetActiveMonsters(currentRoom);

        if (activeMonsters.Count > 0)
        {
            HandleCombat(activeMonsters);
        }
        // 2. Check for boss level up hatch
        else if (currentRoom.roomType == RoomType.Boss && IsBossHatchActive(currentRoom))
        {
            HandleHatchNavigation(currentRoom);
        }
        // 3. Check for uncleared mini-game challenges
        else if (currentRoom.roomType == RoomType.Challenge && !currentRoom.isCleared)
        {
            HandleChallengeRoom(currentRoom);
        }
        // 4. Default: Explore/Traverse to the next room
        else
        {
            HandleExploration(currentRoom);
        }
    }

    private void ResetAIInputs()
    {
        player.aiHorizontal = 0f;
        player.aiVertical = 0f;
        player.aiShootUp = false;
        player.aiShootDown = false;
        player.aiShootLeft = false;
        player.aiShootRight = false;
    }

    private List<Transform> GetActiveMonsters(Room room)
    {
        List<Transform> list = new List<Transform>();
        if (room.monsterContainer != null)
        {
            foreach (Transform child in room.monsterContainer)
            {
                if (child != null && child.gameObject.activeInHierarchy)
                {
                    Monster m = child.GetComponent<Monster>();
                    if (m != null && m.HP > 0)
                    {
                        list.Add(child);
                    }
                }
            }
        }
        return list;
    }

    private bool IsBossHatchActive(Room room)
    {
        Transform levelUp = FindChildRecursive(room.transform, "LevelUp");
        return levelUp != null && levelUp.gameObject.activeInHierarchy;
    }

    private Transform FindChildRecursive(Transform parent, string targetName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == targetName) return child;
            Transform found = FindChildRecursive(child, targetName);
            if (found != null) return found;
        }
        return null;
    }

    private void HandleCombat(List<Transform> monsters)
    {
        // Find closest monster
        Transform closest = null;
        float minDist = float.MaxValue;
        foreach (var m in monsters)
        {
            float d = Vector2.Distance(transform.position, m.position);
            if (d < minDist)
            {
                minDist = d;
                closest = m;
            }
        }

        if (closest == null) return;

        bool isPro = player.controlMode == Player.PlayerControlMode.AI_Pro;

        if (isPro)
        {
            // PRO AI: Circle target, keep safe distance, dodge bullets
            Vector2 toMonster = closest.position - transform.position;
            float dist = toMonster.magnitude;
            Vector2 moveDir = Vector2.zero;

            if (dist < 2.5f)
            {
                // Too close, retreat
                moveDir = -toMonster.normalized;
            }
            else if (dist > 4.5f)
            {
                // Too far, approach
                moveDir = toMonster.normalized;
            }
            else
            {
                // Strafe / circle target
                moveDir = Vector3.Cross(toMonster.normalized, Vector3.forward);
            }

            // Dodge incoming bullets
            Vector2 dodgeDir = GetBulletDodgeVector();
            if (dodgeDir != Vector2.zero)
            {
                moveDir = (moveDir + dodgeDir * 1.5f).normalized;
            }

            player.aiHorizontal = moveDir.x;
            player.aiVertical = moveDir.y;

            // Shoot at target perfectly (prioritize horizontal or vertical alignment)
            player.aiShootUp = false;
            player.aiShootDown = false;
            player.aiShootLeft = false;
            player.aiShootRight = false;

            if (Mathf.Abs(toMonster.x) > Mathf.Abs(toMonster.y))
            {
                if (toMonster.x > 0) player.aiShootRight = true;
                else player.aiShootLeft = true;
            }
            else
            {
                if (toMonster.y > 0) player.aiShootUp = true;
                else player.aiShootDown = true;
            }
        }
        else
        {
            // NOOB AI: Walk directly towards enemy (colliding and taking damage), shoot randomly or slowly
            Vector2 toMonster = closest.position - transform.position;
            Vector2 moveDir = toMonster.normalized;

            player.aiHorizontal = moveDir.x;
            player.aiVertical = moveDir.y;

            // Shoots randomly 25% of the time, often misses
            player.aiShootUp = false;
            player.aiShootDown = false;
            player.aiShootLeft = false;
            player.aiShootRight = false;

            if (Random.value < 0.25f)
            {
                int shootDirIndex = Random.Range(0, 4);
                if (shootDirIndex == 0) player.aiShootUp = true;
                else if (shootDirIndex == 1) player.aiShootDown = true;
                else if (shootDirIndex == 2) player.aiShootLeft = true;
                else player.aiShootRight = true;
            }
        }
    }

    private Vector2 GetBulletDodgeVector()
    {
        // Detect bullets in the room
        GameObject[] bullets = GameObject.FindGameObjectsWithTag("Bullet"); // or MonsterBullet
        Vector2 dodge = Vector2.zero;
        foreach (var b in bullets)
        {
            // Only care about enemy-originating bullets if possible
            if (b.name.Contains("Monster") || b.name.Contains("enemy") || b.name.Contains("tear"))
            {
                float d = Vector2.Distance(transform.position, b.transform.position);
                if (d < 2.0f)
                {
                    // Dodge perpendicular to bullet trajectory
                    Rigidbody2D rb = b.GetComponent<Rigidbody2D>();
                    if (rb != null)
                    {
                        Vector2 bulletVel = rb.velocity;
                        Vector2 perp = Vector3.Cross(bulletVel.normalized, Vector3.forward);
                        dodge += perp;
                    }
                    else
                    {
                        // Run away from position
                        dodge += (Vector2)(transform.position - b.transform.position).normalized;
                    }
                }
            }
        }
        return dodge.normalized;
    }

    private void HandleHatchNavigation(Room room)
    {
        Transform levelUp = FindChildRecursive(room.transform, "LevelUp");
        if (levelUp != null)
        {
            Vector2 toHatch = levelUp.position - transform.position;
            player.aiHorizontal = Mathf.Clamp(toHatch.x, -1f, 1f);
            player.aiVertical = Mathf.Clamp(toHatch.y, -1f, 1f);
            player.aiShootUp = false;
            player.aiShootDown = false;
            player.aiShootLeft = false;
            player.aiShootRight = false;
        }
    }

    private void HandleChallengeRoom(Room room)
    {
        bool isPro = player.controlMode == Player.PlayerControlMode.AI_Pro;
        string challName = room.GetType().Name;

        if (!isPro)
        {
            // NOOB AI: Fails challenges by wandering randomly or standing still
            if (Random.value < 0.05f)
            {
                player.aiHorizontal = Random.Range(-1f, 1f);
                player.aiVertical = Random.Range(-1f, 1f);
            }
            player.aiShootUp = false;
            player.aiShootDown = false;
            player.aiShootLeft = false;
            player.aiShootRight = false;
            return;
        }

        // PRO AI: Solves specific challenges
        player.aiShootUp = false;
        player.aiShootDown = false;
        player.aiShootLeft = false;
        player.aiShootRight = false;

        switch (challName)
        {
            case "ObserverRoom":
                // Stand still if observer eyes are open (simulated by checking if eyes are active/open)
                // Let's check for any Eye objects in the room
                bool eyeOpen = false;
                foreach (Transform t in room.transform)
                {
                    if (t.name.Contains("Eye") || t.name.Contains("eye"))
                    {
                        // Check if its sprite indicates open or animator state
                        Animator anim = t.GetComponent<Animator>();
                        if (anim != null && anim.GetCurrentAnimatorStateInfo(0).IsName("EyeOpen"))
                        {
                            eyeOpen = true;
                            break;
                        }
                    }
                }
                if (eyeOpen)
                {
                    player.aiHorizontal = 0f;
                    player.aiVertical = 0f;
                }
                else
                {
                    // Move to goal or wander safely
                    player.aiHorizontal = 0.5f;
                    player.aiVertical = 0f;
                }
                break;

            case "QuickTileReaction":
            case "SequenceMemoryChallenge":
                // Find active green plate to stand on
                Transform targetPlate = null;
                foreach (Transform child in room.transform)
                {
                    if (child.name.Contains("Plate") || child.name.Contains("tile"))
                    {
                        // Pro looks for green glowing plate
                        SpriteRenderer sr = child.GetComponent<SpriteRenderer>();
                        if (sr != null && sr.color == Color.green)
                        {
                            targetPlate = child;
                            break;
                        }
                    }
                }
                if (targetPlate != null)
                {
                    Vector2 toPlate = targetPlate.position - transform.position;
                    player.aiHorizontal = Mathf.Clamp(toPlate.x, -1f, 1f);
                    player.aiVertical = Mathf.Clamp(toPlate.y, -1f, 1f);
                }
                else
                {
                    player.aiHorizontal = 0f;
                    player.aiVertical = 0f;
                }
                break;

            case "GhostSurvivalChallenge":
            case "SkyTearsSurvivalChallenge":
            case "ChangingSafeZones":
                // Avoid hazards (ghosts/tears) or move to green safe zones
                Transform safeZone = null;
                foreach (Transform child in room.transform)
                {
                    if (child.name.Contains("Safe") || child.name.Contains("Zone"))
                    {
                        safeZone = child;
                        break;
                    }
                }
                if (safeZone != null)
                {
                    Vector2 toSafe = safeZone.position - transform.position;
                    player.aiHorizontal = Mathf.Clamp(toSafe.x, -1f, 1f);
                    player.aiVertical = Mathf.Clamp(toSafe.y, -1f, 1f);
                }
                else
                {
                    // Wander slowly, dodging obstacles
                    player.aiHorizontal = Mathf.Sin(Time.time) * 0.5f;
                    player.aiVertical = Mathf.Cos(Time.time) * 0.5f;
                }
                break;

            case "SacrificeAltar":
            case "RouletteWheel":
                // Walk to the center object
                Transform interactable = room.transform.Find("Altar") ?? room.transform.Find("Roulette") ?? room.transform.Find("wheel");
                if (interactable == null)
                {
                    // Fallback to room center
                    interactable = room.transform;
                }
                Vector2 toInteract = interactable.position - transform.position;
                if (toInteract.magnitude > 0.5f)
                {
                    player.aiHorizontal = Mathf.Clamp(toInteract.x, -1f, 1f);
                    player.aiVertical = Mathf.Clamp(toInteract.y, -1f, 1f);
                }
                else
                {
                    player.aiHorizontal = 0f;
                    player.aiVertical = 0f;
                }
                break;

            default:
                // Default: wander around the room center
                Vector2 toCenter = (Vector2)room.transform.position - (Vector2)transform.position;
                if (toCenter.magnitude > 1.5f)
                {
                    player.aiHorizontal = Mathf.Clamp(toCenter.x, -1f, 1f);
                    player.aiVertical = Mathf.Clamp(toCenter.y, -1f, 1f);
                }
                else
                {
                    player.aiHorizontal = Mathf.Sin(Time.time) * 0.3f;
                    player.aiVertical = Mathf.Cos(Time.time) * 0.3f;
                }
                break;
        }
    }

    private void HandleExploration(Room room)
    {
        doorRecheckTimer -= Time.deltaTime;
        
        // If current door target is invalid or timer expired, pick a new one
        if (currentDoorTarget == null || doorRecheckTimer <= 0f)
        {
            currentDoorTarget = GetExplorationDoor(room);
            doorRecheckTimer = 4f; // Try to walk to this door for 4 seconds before re-evaluating
        }

        if (currentDoorTarget != null)
        {
            Vector2 targetPos = currentDoorTarget.position;
            // Push the target position slightly deeper in the door's direction
            // so the AI player walks completely into the trigger and doesn't get blocked on the edges.
            string doorName = currentDoorTarget.parent != null ? currentDoorTarget.parent.name : currentDoorTarget.name;
            if (doorName.Contains("Up")) targetPos += Vector2.up * 1.5f;
            else if (doorName.Contains("Down")) targetPos += Vector2.down * 1.5f;
            else if (doorName.Contains("Left")) targetPos += Vector2.left * 1.5f;
            else if (doorName.Contains("Right")) targetPos += Vector2.right * 1.5f;

            Vector2 toDoor = targetPos - (Vector2)transform.position;
            player.aiHorizontal = Mathf.Clamp(toDoor.x, -1f, 1f);
            player.aiVertical = Mathf.Clamp(toDoor.y, -1f, 1f);
        }
        else
        {
            // Wander center
            Vector2 toCenter = (Vector2)room.transform.position - (Vector2)transform.position;
            player.aiHorizontal = Mathf.Clamp(toCenter.x + Mathf.Sin(Time.time), -1f, 1f);
            player.aiVertical = Mathf.Clamp(toCenter.y + Mathf.Cos(Time.time), -1f, 1f);
        }

        player.aiShootUp = false;
        player.aiShootDown = false;
        player.aiShootLeft = false;
        player.aiShootRight = false;
    }

    private Transform GetExplorationDoor(Room room)
    {
        if (room.activeDoorList == null || room.activeDoorList.Count == 0) return null;

        // Smart exploration: try to choose doors leading to UNVISITED rooms first!
        Transform fallback = null;
        
        // Match doors to neighbor directions
        // activeDoorList elements correspond to doors.
        for (int i = 0; i < room.activeDoorList.Count; i++)
        {
            GameObject door = room.activeDoorList[i];
            if (door != null && door.activeInHierarchy)
            {
                Transform doorCollider = door.transform.Find("collider") ?? door.transform;
                fallback = doorCollider;

                // Let's check which neighboring room this door corresponds to
                // Neighbor directions: Up(0), Down(1), Left(2), Right(3)
                int x = (int)room.coordinate.x;
                int y = (int)room.coordinate.y;
                Room neighbor = null;

                Level level = GameManager.Instance.level;
                if (level != null)
                {
                    // Check neighbors in array correctly mapped to DirectionType enum
                    if (i == 0 && y + 1 < Level.MAX_SIZE) neighbor = level.roomArray[x, y + 1];
                    else if (i == 1 && y - 1 >= 0) neighbor = level.roomArray[x, y - 1];
                    else if (i == 2 && x - 1 >= 0) neighbor = level.roomArray[x - 1, y];
                    else if (i == 3 && x + 1 < Level.MAX_SIZE) neighbor = level.roomArray[x + 1, y];
                }

                if (neighbor != null)
                {
                    if (!visitedRooms.Contains(neighbor.coordinate))
                    {
                        // Prioritize unvisited neighbors!
                        return doorCollider;
                    }
                }
            }
        }

        // Return first valid door if all neighbors have been visited
        return fallback;
    }
}
