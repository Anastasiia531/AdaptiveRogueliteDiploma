using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Room : MonoBehaviour
{
    #region 房间属性
    [Header("房间属性")]

    [HideInInspector]
    public RoomType roomType;//房间类型

    [HideInInspector]
    public Vector2 coordinate;//坐标

    protected RoomLayout roomLayout;//布局文件

    public bool isArrived = false;//是否已到达
    public bool isCleared = false;//是否已清理过

    //房间宽高
    public static float RoomWidth = 24f;
    public static float RoomHeight = 14f;

    //单位数量和大小
    public static int HorizomtalUnit { get { return 13; } }
    public static int VerticalUnit { get { return 7; } }
    public static float UnitSize { get { return 0.28f; } }


    #endregion

    #region 房间组成
    [Header("房间组成")]

    public List<GameObject> doorList;//门列表
    public int ActiveDoorCount { get { return activeDoorList.Count; } }
    public List<GameObject> activeDoorList = new List<GameObject>();
    public List<GameObject> neighboringDoorList = new List<GameObject>();

    public List<Room> neighboringRoomList = new List<Room>();//相邻的房间
    public GameObject preRoom;
    #endregion


    #region 房间下属节点
    [Header("房间下属节点")]
    public Transform monsterContainer;
    public Transform itemContainer;
    public Transform defaultContainer;
    #endregion

    #region 其他
    [HideInInspector]
    public Level level;//关卡
    #endregion

    private void Awake()
    {
    }

    public virtual void Initialize()
    {
        roomLayout = level.pools != null ? level.pools.GetRoomLayout(roomType) : null;
        if (roomLayout == null || roomLayout.room == null)
        {
            Debug.LogError($"[Room] Failed to load RoomLayout for roomType: {roomType} at coordinate: {coordinate}. Falling back to StartRoom layout.");
            if (level.pools != null)
            {
                roomLayout = level.pools.GetRoomLayout(RoomType.Start);
            }
        }

        if (roomLayout != null && roomLayout.room != null)
        {
            preRoom = roomLayout.room;
            preRoom = Instantiate(preRoom);
            preRoom.transform.parent = transform;
            preRoom.transform.position = transform.position;
        }
        else
        {
            Debug.LogError($"[Room] Fallback StartRoom layout also failed for coordinate: {coordinate}!");
        }
        ChangeDoorOutWard();
    }

    /// <summary>
    /// 激活对应方向的门
    /// </summary>
    /// <param name="directionType"></param>
    public void ActivateDoor(DirectionType directionType, Room neighboringRoom, GameObject neighboringDoor)
    {
        GameObject door = doorList[(int)directionType];

        door.SetActive(true);
        door.transform.Find("Door").gameObject.SetActive(true);
        activeDoorList.Add(door);
        neighboringRoomList.Add(neighboringRoom);
        neighboringDoorList.Add(neighboringDoor);
    }

    /// <summary>
    /// 打开所有已激活的门
    /// </summary>
    public void OpenActivatedDoor()
    {
        List<GameObject> doors = new List<GameObject>();
        doors.AddRange(activeDoorList);
        // doors.AddRange(neighboringDoorList);
        foreach (var door in doors)
        {
            if (door != null && door.activeInHierarchy)
            {
                var colTrans = door.transform.Find("collider");
                if (colTrans != null)
                {
                    var col = colTrans.gameObject.GetComponent<BoxCollider2D>();
                    if (col != null) col.isTrigger = true;
                }
                
                var doorVisual = door.transform.Find("Door");
                if (doorVisual != null && doorVisual.gameObject.activeInHierarchy)
                {
                    var anim = doorVisual.GetComponent<Animator>();
                    if (anim != null) anim.Play("DoorOpen");
                }
            }
        }
    }

    /// <summary>
    /// 改变该房间和相邻房间门的样式
    /// </summary>
    private void ChangeDoorOutWard()
    {
        if (roomType == RoomType.Normal || roomType == RoomType.Start) { return; }

        List<GameObject> doors = new List<GameObject>();
        doors.AddRange(activeDoorList);
        doors.AddRange(neighboringDoorList);

        foreach (var door in doors)
        {
            SpriteRenderer doorColor = door.transform.Find("Door").GetComponent<SpriteRenderer>();
            switch (roomType)
            {
                case RoomType.Boss:
                    doorColor.color = new Color(144, 0, 0, 255);
                    break;
                case RoomType.Treasure:
                    doorColor.color = new Color(255, 255, 0, 255);
                    break;
                case RoomType.Shop:
                    doorColor.color = new Color(120, 255, 240, 255);
                    break;
                default:
                    break;
            }
        }
    }

    private bool IsInDoorSafetyZone(Vector2 coord, GameObject itemObj)
    {
        if (itemObj == null) return false;
        
        string nameLower = itemObj.name.ToLower();

        // 1. Boss Room Center check (to protect the LevelUp ladder/hatch at 0,0)
        if (roomType == RoomType.Boss)
        {
            // The LevelUp hatch is at (0, 0). Do not spawn ANY layout objects within 2.5 units of the center
            if (coord.magnitude < 2.5f)
            {
                return true; // Prevent spawning
            }
        }

        // 2. Center safety zone for normal/combat rooms (prevent spawning obstacles at the player's spawn points, but allow items/chests)
        bool isObstacle = !nameLower.Contains("pedestal") && 
                          !nameLower.Contains("item") && 
                          !nameLower.Contains("goods") && 
                          !nameLower.Contains("chest") && 
                          !nameLower.Contains("heart") && 
                          !nameLower.Contains("key") && 
                          !nameLower.Contains("coin") &&
                          !nameLower.Contains("levelup") &&
                          !nameLower.Contains("door");

        if (isObstacle)
        {
            // Center safety zone (radius of 1.8 units around the center)
            if (Mathf.Abs(coord.x) < 1.8f && Mathf.Abs(coord.y) < 1.8f)
            {
                return true;
            }
        }

        // 3. Door safety zones and corridors: NEVER spawn anything in active door paths!
        for (int i = 0; i < doorList.Count; i++)
        {
            if (activeDoorList.Contains(doorList[i]))
            {
                DirectionType dir = (DirectionType)i;
                // Up Door
                if (dir == DirectionType.Up)
                {
                    // Door area & corridor: x in [-2.0, 2.0], y in [0, 8.0]
                    if (Mathf.Abs(coord.x) < 2.0f && coord.y >= -0.5f && coord.y <= 8.0f) return true;
                }
                // Down Door
                else if (dir == DirectionType.Down)
                {
                    // Door area & corridor: x in [-2.0, 2.0], y in [-8.0, 0]
                    if (Mathf.Abs(coord.x) < 2.0f && coord.y <= 0.5f && coord.y >= -8.0f) return true;
                }
                // Right Door
                else if (dir == DirectionType.Right)
                {
                    // Door area & corridor: x in [0, 13.0], y in [-2.0, 2.0]
                    if (coord.x >= -0.5f && coord.x <= 13.0f && Mathf.Abs(coord.y) < 2.0f) return true;
                }
                // Left Door
                else if (dir == DirectionType.Left)
                {
                    // Door area & corridor: x in [-13.0, 0], y in [-2.0, 2.0]
                    if (coord.x <= 0.5f && coord.x >= -13.0f && Mathf.Abs(coord.y) < 2.0f) return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// 根据房间布局文件roomLayout 生成内容
    /// </summary>
    public virtual void GenerateRoomContent()
    {
        if (roomLayout == null)
        {
            Debug.LogWarning($"[Room] Cannot generate content for roomType: {roomType} at coordinate: {coordinate} because roomLayout is null!");
            CheckOpenDoor();
            return;
        }

        if (roomLayout.itemList == null)
        {
            roomLayout.itemList = new List<SimplePairWithGameObjectVector2>();
        }
        if (roomLayout.monsterList == null)
        {
            roomLayout.monsterList = new List<Vector2>();
        }

        for (int i = 0; i < roomLayout.itemList.Count; i++)
        {
            GameObject itemObj = roomLayout.itemList[i].value1;
            Vector2 finalCoord = roomLayout.itemList[i].value2;
            
            if (itemObj != null && !itemObj.name.Contains("Pedestal") && !itemObj.name.Contains("Item") && !itemObj.name.Contains("Goods"))
            {
                // Add a small random offset to obstacles/decorations so layouts are not identical
                finalCoord += new Vector2(UnityEngine.Random.Range(-0.25f, 0.25f), UnityEngine.Random.Range(-0.25f, 0.25f));
            }

            if (IsInDoorSafetyZone(finalCoord, itemObj))
            {
                continue; // Skip spawning obstacles in active door corridors/areas
            }

            GameObject spawned = GenerateGameObjectWithCoordinate(itemObj, finalCoord, itemContainer);

            // Flip sprites randomly for obstacles/decorations to increase visual diversity
            if (spawned != null && itemObj != null && !itemObj.name.Contains("Pedestal") && !itemObj.name.Contains("Item") && !itemObj.name.Contains("Goods"))
            {
                var spriteRenderer = spawned.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    spriteRenderer.flipX = UnityEngine.Random.value > 0.5f;
                    spriteRenderer.flipY = UnityEngine.Random.value > 0.5f;
                }
            }
        }

        int baseCount = roomLayout.monsterList.Count;
        int monsterSpawnCount = baseCount;

        if (AdaptiveDifficultyManager.Instance != null && roomType != RoomType.Boss)
        {
            // Easy (SkillIndex=0): spawn 50% of monsters.
            // Hard (SkillIndex=1): spawn 150% of monsters.
            float multiplier = Mathf.Lerp(0.5f, 1.5f, AdaptiveDifficultyManager.Instance.SkillIndex);
            monsterSpawnCount = Mathf.RoundToInt(baseCount * multiplier);
            
            // Safety cap: clamp between 1 and 12 to avoid empty combat rooms or infinite buildup.
            if (baseCount > 0)
            {
                monsterSpawnCount = Mathf.Clamp(monsterSpawnCount, 1, 12);
            }
        }

        for (int i = 0; i < monsterSpawnCount; i++)
        {
            GameObject monster = level.pools.GetMonster(roomType == RoomType.Boss ? MonsterType.Boss : MonsterType.Minion);
            if (monster == null) continue;

            Vector2 coordinate;
            if (i < baseCount)
            {
                coordinate = roomLayout.monsterList[i];
            }
            else
            {
                // Generate a nearby spawn coordinate with a small safe offset
                Vector2 baseCoord = roomLayout.monsterList[Random.Range(0, baseCount)];
                coordinate = baseCoord + new Vector2(Random.Range(-0.8f, 0.8f), Random.Range(-0.8f, 0.8f));
                coordinate.x = Mathf.Clamp(coordinate.x, -10f, 10f);
                coordinate.y = Mathf.Clamp(coordinate.y, -5f, 5f);
            }

            GenerateGameObjectWithCoordinate(monster, coordinate, monsterContainer);
        }

        CheckOpenDoor();
    }

    /// <summary>
    /// 生成清理房间的奖励品
    /// </summary>
    private void GenerateRoomClearingReward()
    {
        if (roomLayout == null) return;
        GameObject reward = level.pools.GetRoomClearingReward(roomType);
        GenerateGameObjectWithCoordinate(reward, roomLayout.RewardPosition, itemContainer);
        if (roomType == RoomType.Boss)
        {
            Transform levelUp = FindChildRecursive(transform, "LevelUp");
            if (levelUp != null)
            {
                levelUp.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogWarning("[DDA] LevelUp (hatch) child object not found in Boss room hierarchy!");
            }
        }
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

    /// <summary>
    /// 使用具体位置在房间里生成单个物体
    /// </summary>
    /// <param name="prefab"></param>
    /// <param name="position"></param>
    /// <param name="container"></param>
    /// <returns></returns>
    public GameObject GenerateGameObjectWithPosition(GameObject prefab, Vector2 position, Transform container)
    {
        if (prefab == null) { return null; }

        GameObject go = Instantiate(prefab, container);
        go.transform.position = position;
        return go;
    }
    /// <summary>
    /// 使用坐标在房间里生成单个物体
    /// </summary>
    public GameObject GenerateGameObjectWithCoordinate(GameObject prefab, Vector2 coordinate, Transform container)
    {
        if (prefab == null) { return null; }

        GameObject go = Instantiate(prefab, container);
        //Unit数量为 x:13，y:7,每单位大小为UnitSize
        //coordinate范围为 x:1-25，y:1-13,每单位大小为UnitSize / 2
        //以房间中心为原点，左上角为coordinate起始点，进行位置计算
        // Vector2 postiton = new Vector2(-(HorizomtalUnit - coordinate.x) * UnitSize / 2, (VerticalUnit - coordinate.y) * UnitSize / 2);
        go.transform.localPosition = coordinate;

        return go;
    }


    /// <summary>
    /// 检查开门
    /// </summary>
    public virtual void CheckOpenDoor()
    {
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(DelayCheck());
        }
        else if (level != null && level.gameObject.activeInHierarchy)
        {
            level.StartCoroutine(DelayCheck());
        }
        else if (GameManager.Instance != null && GameManager.Instance.gameObject.activeInHierarchy)
        {
            GameManager.Instance.StartCoroutine(DelayCheck());
        }
    }
    /// <summary>
    /// 延迟检查
    /// </summary>
    /// <returns></returns>
    protected virtual IEnumerator DelayCheck()
    {
        yield return null;
        if (monsterContainer.childCount == 0 && !isCleared)
        {
            OpenActivatedDoor();
            
            bool shouldSpawnReward = true;
            if (roomType != RoomType.Boss)
            {
                float rewardProb = 0.5f; // 50% default
                if (AdaptiveDifficultyManager.Instance != null)
                {
                    // Scale between 75% (poor play) and 30% (good play)
                    rewardProb = Mathf.Lerp(0.75f, 0.30f, AdaptiveDifficultyManager.Instance.SkillIndex);
                }
                shouldSpawnReward = UnityEngine.Random.value <= rewardProb;
            }

            if (roomLayout.isGenerateReward && shouldSpawnReward)
            {
                GenerateRoomClearingReward();
            }
            isCleared = true;
            if (AdaptiveDifficultyManager.Instance != null)
            {
                AdaptiveDifficultyManager.Instance.LogRoomClear();
                AdaptiveDifficultyManager.Instance.OnRoomCleared();
            }
        }
    }
}