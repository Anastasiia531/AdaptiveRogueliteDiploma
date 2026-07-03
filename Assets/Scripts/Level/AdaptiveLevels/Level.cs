using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Level : MonoBehaviour
{
    [Header("房间属性")]
    public const int MAX_SIZE = 20;
    [SerializeField]
    private Room roomPrefab;

    public int roomNum;
    [HideInInspector]
    public Room[,] roomArray = new Room[MAX_SIZE, MAX_SIZE];
    [HideInInspector]
    public Room currentRoom;

    #region 功能类
    [HideInInspector]
    public Pool pools;
    public GameItemManager manager;
    #endregion

    #region 其他
    public Player player;
    private UIManager UI;
    private bool roomsGenerated = false;
    #endregion

    private void Awake()
    {
        pools = GetComponent<Pool>();
        manager = GetComponent<GameItemManager>();
    }
    private void OnEnable()
    {
        InitializeLevel();
    }
    private void OnDisable()
    {
        roomsGenerated = false;
    }
    private void Start()
    {
        InitializeLevel();
    }

    private void InitializeLevel()
    {
        if (GameManager.Instance != null && GameManager.Instance.level != null && GameManager.Instance.level != this)
        {
            Destroy(gameObject);
            return;
        }

        if (roomsGenerated) return;

        if (GameManager.Instance == null || GameManager.Instance.player == null || UIManager.Instance == null)
        {
            return;
        }

        roomsGenerated = true;
        player = GameManager.Instance.player;
        UI = UIManager.Instance;
        CreateRooms();
        UI.miniMap.level = this;
        UI.miniMap.CreateMiniMap();
        MoveToStartRoom();

        // Diagnostic Logging
        Debug.LogFormat("[DDA DIAGNOSTIC] Level Initialized. Player: controllable={0}, speed={1}, live={2}. Active rooms={3}.", 
            player.isControllable, player.speed, player.isLive, transform.childCount);
        foreach (Transform child in transform)
        {
            Room r = child.GetComponent<Room>();
            if (r != null)
            {
                Debug.LogFormat("[DDA DIAGNOSTIC] Room: name={0}, coord={1}, type={2}, activeDoors={3}, totalDoors={4}, preRoom={5}", 
                    child.name, r.coordinate, r.roomType, r.activeDoorList.Count, r.doorList.Count, r.preRoom != null ? r.preRoom.name : "null");
            }
        }
    }

    /// <summary>
    /// 创建所有的房间
    /// </summary>
    private void CreateRooms()
    {
        if (AdaptiveDifficultyManager.Instance != null)
        {
            AdaptiveDifficultyManager.Instance.EvaluateSkillBeforeGeneration();
        }

        // DDA / Isaac adjustment: Ensure we always have at least 7 rooms
        if (roomNum < 7 && !(GameManager.Instance != null && GameManager.Instance.isTutorialMode))
        {
            roomNum = 7;
        }

        if (GameManager.Instance != null && GameManager.Instance.isTutorialMode)
        {
            GenerateTutorialRooms();
            return;
        }

        float skill = (AdaptiveDifficultyManager.Instance != null) ? AdaptiveDifficultyManager.Instance.SkillIndex : 0.5f;

        bool success = false;
        int attempts = 0;
        int outsetX = roomArray.GetLength(0) / 2;
        int outsetY = roomArray.GetLength(1) / 2;

        while (!success && attempts < 150)
        {
            attempts++;
            // Clear old rooms
            Array.Clear(roomArray, 0, roomArray.Length);
            for (int i = 0; i < transform.childCount; i++)
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }

            if (skill < 0.4f)
            {
                success = GenerateLinearTopology(outsetX, outsetY);
            }
            else if (skill > 0.7f)
            {
                success = GenerateMazeTopology(outsetX, outsetY);
            }
            else
            {
                success = GenerateStandardTopology(outsetX, outsetY);
            }

            if (success)
            {
                LinkDoors();
                
                // Rebuild neighbor links temporarily to compute BFS distances
                RebuildNeighboringRooms();

                // Verify we have enough single-door rooms (dead ends) for Boss, Shop, Treasure
                List<Room> singleDoorRoomList = new List<Room>();
                foreach (Room room in roomArray)
                {
                    if (room != null && room.ActiveDoorCount == 1 && room.coordinate != new Vector2(outsetX, outsetY))
                    {
                        singleDoorRoomList.Add(room);
                    }
                }

                if (singleDoorRoomList.Count < 3)
                {
                    success = false; // Retry
                }
                else
                {
                    SetRoomsTypeAdaptive(singleDoorRoomList, skill, outsetX, outsetY);
                }
            }
        }

        if (!success)
        {
            Debug.LogError("[DDA Level Generation] Failed to generate adaptive layout after 150 attempts. Generating fallback standard layout.");
            GenerateStandardFallback(outsetX, outsetY);
        }
    }

    private void GenerateTutorialRooms()
    {
        // Clear old rooms
        Array.Clear(roomArray, 0, roomArray.Length);
        for (int i = 0; i < transform.childCount; i++)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        int outsetX = roomArray.GetLength(0) / 2;
        int outsetY = roomArray.GetLength(1) / 2;

        // Spawn 5 fixed rooms
        Room sRoom = CreateRoom(new Vector2(outsetX, outsetY));
        Room bRoom = CreateRoom(new Vector2(outsetX + 1, outsetY));
        Room tRoom = CreateRoom(new Vector2(outsetX, outsetY - 1));
        Room shRoom = CreateRoom(new Vector2(outsetX, outsetY + 1));
        Room cRoom = CreateRoom(new Vector2(outsetX - 1, outsetY));

        roomArray[outsetX, outsetY] = sRoom;
        roomArray[outsetX + 1, outsetY] = bRoom;
        roomArray[outsetX, outsetY - 1] = tRoom;
        roomArray[outsetX, outsetY + 1] = shRoom;
        roomArray[outsetX - 1, outsetY] = cRoom;

        LinkDoors();

        // Set fixed types
        sRoom.roomType = RoomType.Start;
        bRoom.roomType = RoomType.Boss;
        tRoom.roomType = RoomType.Treasure;
        shRoom.roomType = RoomType.Shop;
        cRoom.roomType = RoomType.Challenge;

        // Convert components
        roomArray[outsetX, outsetY] = ConvertRoomComponent<CombatRoom>(sRoom);
        currentRoom = roomArray[outsetX, outsetY];

        roomArray[outsetX + 1, outsetY] = ConvertRoomComponent<CombatRoom>(bRoom);
        roomArray[outsetX, outsetY - 1] = ConvertRoomComponent<SpecialtyRoom>(tRoom);
        roomArray[outsetX, outsetY + 1] = ConvertRoomComponent<SpecialtyRoom>(shRoom);
        
        // Convert challenge room to a random concrete challenge subclass (only from the 6 allowed ones!)
        int tutorialChallengeIndex = UnityEngine.Random.Range(0, 6);
        switch (tutorialChallengeIndex)
        {
            case 0: roomArray[outsetX - 1, outsetY] = ConvertRoomComponent<ThreeCardsMonte>(cRoom); break;
            case 1: roomArray[outsetX - 1, outsetY] = ConvertRoomComponent<SequenceMemoryChallenge>(cRoom); break;
            case 2: roomArray[outsetX - 1, outsetY] = ConvertRoomComponent<RouletteWheel>(cRoom); break;
            case 3: roomArray[outsetX - 1, outsetY] = ConvertRoomComponent<TimeMazeChallenge>(cRoom); break;
            case 4: roomArray[outsetX - 1, outsetY] = ConvertRoomComponent<ObserverRoom>(cRoom); break;
            default: roomArray[outsetX - 1, outsetY] = ConvertRoomComponent<ChangingSafeZones>(cRoom); break;
        }

        RebuildNeighboringRooms();

        foreach (Room room in roomArray)
        {
            if (room != null)
            {
                room.Initialize();
            }
        }
    }

    private Room CreateRoom(Vector2 coordinate)
    {
        Room newRoom = Instantiate(roomPrefab, transform);
        newRoom.coordinate = coordinate;

        int x = (int)coordinate.x - roomArray.GetLength(0) / 2;
        int y = (int)coordinate.y - roomArray.GetLength(1) / 2;
        newRoom.transform.position =
            new Vector2(y * Room.RoomWidth, x * Room.RoomHeight);
        newRoom.level = gameObject.GetComponent<Level>();
        return newRoom;
    }

    private void LinkDoors()
    {
        foreach (Room room in roomArray)
        {
            if (room != null)
            {
                int x = (int)room.coordinate.x;
                int y = (int)room.coordinate.y;
                if (roomArray[x + 1, y] != null)
                {
                    Room neighboringRoom = roomArray[x + 1, y];
                    GameObject neighboringDoor = neighboringRoom.doorList[1];
                    room
                        .ActivateDoor(DirectionType.Up,
                        neighboringRoom,
                        neighboringDoor);
                }
                if (roomArray[x - 1, y] != null)
                {
                    room
                        .ActivateDoor(DirectionType.Down,
                        roomArray[x - 1, y],
                        (roomArray[x - 1, y].doorList[0]));
                }
                if (roomArray[x, y - 1] != null)
                {
                    room
                        .ActivateDoor(DirectionType.Left,
                        roomArray[x, y - 1],
                        roomArray[x, y - 1].doorList[3]);
                }
                if (roomArray[x, y + 1] != null)
                {
                    room
                        .ActivateDoor(DirectionType.Right,
                        roomArray[x, y + 1],
                        roomArray[x, y + 1].doorList[2]);
                }
            }
        }
    }

    private bool GenerateLinearTopology(int startX, int startY)
    {
        List<Vector2> path = new List<Vector2>();
        path.Add(new Vector2(startX, startY));
        roomArray[startX, startY] = CreateRoom(new Vector2(startX, startY));

        Vector2 current = new Vector2(startX, startY);
        Vector2[] dirs = new Vector2[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        Vector2 currentDir = dirs[UnityEngine.Random.Range(0, 4)];

        int mainChainLength = roomNum - 2; // Reserve 2 rooms for close dead ends
        for (int i = 1; i < mainChainLength; i++)
        {
            Vector2 next = current + currentDir;
            if (UnityEngine.Random.value > 0.85f || !IsValidForPath(next))
            {
                List<Vector2> validDirs = new List<Vector2>();
                foreach (Vector2 d in dirs)
                {
                    if (IsValidForPath(current + d)) validDirs.Add(d);
                }
                if (validDirs.Count == 0) return false; // Fail and retry
                currentDir = validDirs[UnityEngine.Random.Range(0, validDirs.Count)];
                next = current + currentDir;
            }

            roomArray[(int)next.x, (int)next.y] = CreateRoom(next);
            path.Add(next);
            current = next;
        }

        // Attach remaining rooms as dead ends directly off the main path
        int remaining = roomNum - path.Count;
        for (int r = 0; r < remaining; r++)
        {
            bool attached = false;
            for (int attempt = 0; attempt < 50; attempt++)
            {
                Vector2 parent = path[UnityEngine.Random.Range(1, path.Count - 1)]; // Don't attach to start/end
                Vector2 d = dirs[UnityEngine.Random.Range(0, 4)];
                Vector2 leaf = parent + d;
                if (IsWithinBounds((int)leaf.x, (int)leaf.y) && roomArray[(int)leaf.x, (int)leaf.y] == null)
                {
                    int neighbors = 0;
                    foreach (Vector2 dir in dirs)
                    {
                        Vector2 neighbor = leaf + dir;
                        if (IsWithinBounds((int)neighbor.x, (int)neighbor.y) && roomArray[(int)neighbor.x, (int)neighbor.y] != null)
                        {
                            neighbors++;
                        }
                    }
                    if (neighbors == 1)
                    {
                        roomArray[(int)leaf.x, (int)leaf.y] = CreateRoom(leaf);
                        attached = true;
                        break;
                    }
                }
            }
            if (!attached) return false;
        }

        return true;
    }

    private bool GenerateMazeTopology(int startX, int startY)
    {
        List<Vector2> mazeRooms = new List<Vector2>();
        mazeRooms.Add(new Vector2(startX, startY));
        roomArray[startX, startY] = CreateRoom(new Vector2(startX, startY));

        Vector2[] dirs = new Vector2[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right };

        int targetRooms = roomNum;
        int attempts = 0;
        while (mazeRooms.Count < targetRooms && attempts < 1000)
        {
            attempts++;
            Vector2 parent = mazeRooms[UnityEngine.Random.Range(0, mazeRooms.Count)];
            Vector2 dir = dirs[UnityEngine.Random.Range(0, 4)];
            Vector2 child = parent + dir;

            if (IsWithinBounds((int)child.x, (int)child.y) && roomArray[(int)child.x, (int)child.y] == null)
            {
                roomArray[(int)child.x, (int)child.y] = CreateRoom(child);
                mazeRooms.Add(child);
            }
        }

        return mazeRooms.Count == targetRooms;
    }

    private bool GenerateStandardTopology(int startX, int startY)
    {
        List<Vector2> rooms = new List<Vector2>();
        rooms.Add(new Vector2(startX, startY));
        roomArray[startX, startY] = CreateRoom(new Vector2(startX, startY));

        List<Vector2> alternativeRoomList = new List<Vector2>();
        List<Vector2> hasBeenRemoveRoomList = new List<Vector2>();
        Vector2 lastPos = new Vector2(startX, startY);

        Action<int, int> action = (newX, newY) =>
        {
            Vector2 coordinate = new Vector2(newX, newY);
            if (roomArray[newX, newY] == null)
            {
                if (alternativeRoomList.Contains(coordinate))
                {
                    alternativeRoomList.Remove(coordinate);
                    hasBeenRemoveRoomList.Add(coordinate);
                }
                else if (!hasBeenRemoveRoomList.Contains(coordinate))
                {
                    alternativeRoomList.Add(coordinate);
                }
            }
        };

        for (int i = 1; i < roomNum; i++)
        {
            int x = (int)lastPos.x;
            int y = (int)lastPos.y;

            action(x + 1, y);
            action(x - 1, y);
            action(x, y - 1);
            action(x, y + 1);

            if (alternativeRoomList.Count == 0) return false;
            Vector2 newRoomCoordinate = alternativeRoomList[UnityEngine.Random.Range(0, alternativeRoomList.Count)];
            roomArray[(int)newRoomCoordinate.x, (int)newRoomCoordinate.y] = CreateRoom(newRoomCoordinate);
            rooms.Add(newRoomCoordinate);
            alternativeRoomList.Remove(newRoomCoordinate);
            lastPos = newRoomCoordinate;
        }

        return true;
    }

    private void GenerateStandardFallback(int startX, int startY)
    {
        // Simple linear fallback to make sure the game ALWAYS launches without infinite loops
        Array.Clear(roomArray, 0, roomArray.Length);
        for (int i = 0; i < transform.childCount; i++)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        List<Room> fallbackRooms = new List<Room>();
        for (int i = 0; i < roomNum; i++)
        {
            Room r = CreateRoom(new Vector2(startX + i, startY));
            roomArray[startX + i, startY] = r;
            fallbackRooms.Add(r);
        }

        LinkDoors();
        RebuildNeighboringRooms();

        fallbackRooms[0].roomType = RoomType.Start;
        fallbackRooms[fallbackRooms.Count - 1].roomType = RoomType.Boss;
        fallbackRooms[1].roomType = RoomType.Treasure;
        fallbackRooms[2].roomType = RoomType.Shop;
        if (fallbackRooms.Count > 4)
        {
            fallbackRooms[3].roomType = RoomType.Challenge;
        }

        for (int i = 0; i < fallbackRooms.Count; i++)
        {
            Room r = fallbackRooms[i];
            int x = (int)r.coordinate.x;
            int y = (int)r.coordinate.y;

            if (i == 0 || i == fallbackRooms.Count - 1)
            {
                roomArray[x, y] = ConvertRoomComponent<CombatRoom>(r);
            }
            else if (r.roomType == RoomType.Treasure || r.roomType == RoomType.Shop)
            {
                roomArray[x, y] = ConvertRoomComponent<SpecialtyRoom>(r);
            }
            else if (r.roomType == RoomType.Challenge)
            {
                // Convert Challenge room to a random concrete challenge subclass
                string[] allowedChallenges = new string[] {
                    "ThreeCardsMonte",
                    "SequenceMemoryChallenge",
                    "RouletteWheel",
                    "TimeMazeChallenge",
                    "ObserverRoom",
                    "ChangingSafeZones"
                };
                string challClass = allowedChallenges[UnityEngine.Random.Range(0, allowedChallenges.Length)];
                switch (challClass)
                {
                    case "ThreeCardsMonte": roomArray[x, y] = ConvertRoomComponent<ThreeCardsMonte>(r); break;
                    case "SequenceMemoryChallenge": roomArray[x, y] = ConvertRoomComponent<SequenceMemoryChallenge>(r); break;
                    case "RouletteWheel": roomArray[x, y] = ConvertRoomComponent<RouletteWheel>(r); break;
                    case "TimeMazeChallenge": roomArray[x, y] = ConvertRoomComponent<TimeMazeChallenge>(r); break;
                    case "ObserverRoom": roomArray[x, y] = ConvertRoomComponent<ObserverRoom>(r); break;
                    case "ChangingSafeZones": roomArray[x, y] = ConvertRoomComponent<ChangingSafeZones>(r); break;
                }
            }
            else
            {
                roomArray[x, y] = ConvertRoomComponent<CombatRoom>(r);
            }
        }

        RebuildNeighboringRooms();

        foreach (Room room in roomArray)
        {
            if (room != null)
            {
                room.Initialize();
            }
        }
    }

    private bool IsValidForPath(Vector2 pos)
    {
        if (!IsWithinBounds((int)pos.x, (int)pos.y)) return false;
        if (roomArray[(int)pos.x, (int)pos.y] != null) return false;
        
        int neighbors = 0;
        Vector2[] dirs = new Vector2[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        foreach (Vector2 d in dirs)
        {
            Vector2 neighbor = pos + d;
            if (IsWithinBounds((int)neighbor.x, (int)neighbor.y) && roomArray[(int)neighbor.x, (int)neighbor.y] != null)
            {
                neighbors++;
            }
        }
        return neighbors <= 1;
    }

    private bool IsWithinBounds(int x, int y)
    {
        return x >= 0 && x < roomArray.GetLength(0) && y >= 0 && y < roomArray.GetLength(1);
    }

    private Dictionary<Room, int> GetRoomDistances(Vector2 start)
    {
        Dictionary<Room, int> distances = new Dictionary<Room, int>();
        Queue<Room> queue = new Queue<Room>();
        Room sRoom = roomArray[(int)start.x, (int)start.y];
        
        queue.Enqueue(sRoom);
        distances[sRoom] = 0;

        while (queue.Count > 0)
        {
            Room current = queue.Dequeue();
            int dist = distances[current];

            foreach (Room neighbor in current.neighboringRoomList)
            {
                if (neighbor != null && !distances.ContainsKey(neighbor))
                {
                    distances[neighbor] = dist + 1;
                    queue.Enqueue(neighbor);
                }
            }
        }
        return distances;
    }

    private void SetRoomsTypeAdaptive(List<Room> singleDoorRoomList, float skill, int startX, int startY)
    {
        foreach (Room room in roomArray)
        {
            if (room != null)
            {
                room.roomType = RoomType.Normal;
            }
        }
        roomArray[startX, startY].roomType = RoomType.Start;

        Dictionary<Room, int> distances = GetRoomDistances(new Vector2(startX, startY));

        List<Room> deadEnds = new List<Room>();
        foreach (Room r in singleDoorRoomList)
        {
            if (r != null && r.coordinate != new Vector2(startX, startY))
            {
                deadEnds.Add(r);
            }
        }

        deadEnds.Sort((a, b) => {
            int distA = distances.ContainsKey(a) ? distances[a] : 0;
            int distB = distances.ContainsKey(b) ? distances[b] : 0;
            return distA.CompareTo(distB);
        });

        Room bossRoom = null;
        Room shopRoom = null;
        Room treasureRoom = null;

        if (skill < 0.4f)
        {
            bossRoom = deadEnds[deadEnds.Count - 1];
            treasureRoom = deadEnds[0];
            shopRoom = deadEnds[1];
        }
        else if (skill > 0.7f)
        {
            bossRoom = deadEnds[deadEnds.Count - 1];
            shopRoom = deadEnds[deadEnds.Count - 2];
            treasureRoom = deadEnds[deadEnds.Count - 3];
        }
        else
        {
            bossRoom = deadEnds[deadEnds.Count - 1];
            shopRoom = deadEnds[UnityEngine.Random.Range(0, deadEnds.Count - 1)];
            
            List<Room> remaining = new List<Room>(deadEnds);
            remaining.Remove(bossRoom);
            remaining.Remove(shopRoom);
            treasureRoom = remaining[UnityEngine.Random.Range(0, remaining.Count)];
        }

        bossRoom.roomType = RoomType.Boss;
        shopRoom.roomType = RoomType.Shop;
        treasureRoom.roomType = RoomType.Treasure;

        // Determine if we spawn a SafeRoom (DDA helper)
        bool spawnSafeRoom = false;
        if (skill < 0.4f)
        {
            spawnSafeRoom = true;
        }
        else if (skill <= 0.7f)
        {
            spawnSafeRoom = UnityEngine.Random.value > 0.5f;
        }

        // Build list of normal rooms for Challenge and SafeRoom assignment
        List<Room> normalRooms = new List<Room>();
        foreach (Room room in roomArray)
        {
            if (room != null && room.roomType == RoomType.Normal)
            {
                normalRooms.Add(room);
            }
        }

        // Assign SafeRoom if needed
        if (spawnSafeRoom && normalRooms.Count > 0)
        {
            Room safeRoomChoice = normalRooms[UnityEngine.Random.Range(0, normalRooms.Count)];
            safeRoomChoice.roomType = RoomType.SafeRoom;
            normalRooms.Remove(safeRoomChoice);
        }

        // Determine Challenge room count (always at least 1 challenge room per floor)
        int targetChallengeCount = 1;
        if (skill > 0.7f)
        {
            targetChallengeCount = UnityEngine.Random.Range(2, 4); // 2 or 3
        }
        else if (skill >= 0.4f)
        {
            targetChallengeCount = UnityEngine.Random.Range(1, 3); // 1 or 2
        }

        int challengesSpawned = 0;

        // High skill: Challenge room blocks Treasure room
        if (skill > 0.7f && targetChallengeCount > 0 && treasureRoom != null)
        {
            Room gateway = treasureRoom.neighboringRoomList.Count > 0 ? treasureRoom.neighboringRoomList[0] : null;
            if (gateway != null && gateway.roomType == RoomType.Normal)
            {
                gateway.roomType = RoomType.Challenge;
                normalRooms.Remove(gateway);
                challengesSpawned++;
            }
        }

        if (skill > 0.7f)
        {
            normalRooms.Sort((a, b) => {
                int distA = distances.ContainsKey(a) ? distances[a] : 0;
                int distB = distances.ContainsKey(b) ? distances[b] : 0;
                return distB.CompareTo(distA);
            });
        }

        while (challengesSpawned < targetChallengeCount && normalRooms.Count > 0)
        {
            Room selected = normalRooms[0];
            if (skill <= 0.7f)
            {
                selected = normalRooms[UnityEngine.Random.Range(0, normalRooms.Count)];
            }
            selected.roomType = RoomType.Challenge;
            normalRooms.Remove(selected);
            challengesSpawned++;
        }

        // Convert all base Room component references to their specialized scripts
        for (int x = 0; x < roomArray.GetLength(0); x++)
        {
            for (int y = 0; y < roomArray.GetLength(1); y++)
            {
                Room r = roomArray[x, y];
                if (r == null) continue;

                if (r.coordinate == new Vector2(startX, startY))
                {
                    roomArray[x, y] = ConvertRoomComponent<CombatRoom>(r);
                    currentRoom = roomArray[x, y];
                }
                else if (r.roomType == RoomType.Boss || r.roomType == RoomType.Normal)
                {
                    roomArray[x, y] = ConvertRoomComponent<CombatRoom>(r);
                }
                else if (r.roomType == RoomType.Treasure || r.roomType == RoomType.Shop || r.roomType == RoomType.SafeRoom)
                {
                    roomArray[x, y] = ConvertRoomComponent<SpecialtyRoom>(r);
                }
            }
        }

        RebuildNeighboringRooms();

        // Convert the Challenge rooms to concrete challenge subclasses
        string[] allowedChallenges = new string[] {
            "ThreeCardsMonte",
            "SequenceMemoryChallenge",
            "RouletteWheel",
            "TimeMazeChallenge",
            "ObserverRoom",
            "ChangingSafeZones"
        };

        for (int x = 0; x < roomArray.GetLength(0); x++)
        {
            for (int y = 0; y < roomArray.GetLength(1); y++)
            {
                Room r = roomArray[x, y];
                if (r != null && r.roomType == RoomType.Challenge)
                {
                    string challClass = allowedChallenges[UnityEngine.Random.Range(0, allowedChallenges.Length)];
                    switch (challClass)
                    {
                        case "ThreeCardsMonte": roomArray[x, y] = ConvertRoomComponent<ThreeCardsMonte>(r); break;
                        case "SequenceMemoryChallenge": roomArray[x, y] = ConvertRoomComponent<SequenceMemoryChallenge>(r); break;
                        case "RouletteWheel": roomArray[x, y] = ConvertRoomComponent<RouletteWheel>(r); break;
                        case "TimeMazeChallenge": roomArray[x, y] = ConvertRoomComponent<TimeMazeChallenge>(r); break;
                        case "ObserverRoom": roomArray[x, y] = ConvertRoomComponent<ObserverRoom>(r); break;
                        case "ChangingSafeZones": roomArray[x, y] = ConvertRoomComponent<ChangingSafeZones>(r); break;
                    }
                }
            }
        }

        RebuildNeighboringRooms();

        foreach (Room room in roomArray)
        {
            if (room != null)
            {
                room.Initialize();
            }
        }
    }

    private T ConvertRoomComponent<T>(Room oldRoom) where T : Room
    {
        GameObject go = oldRoom.gameObject;

        // Зберігаємо посилання на важливі змінні кімнати
        Vector2 coord = oldRoom.coordinate;
        RoomType type = oldRoom.roomType;
        Level lvl = oldRoom.level;
        List<GameObject> dList = oldRoom.doorList;
        List<GameObject> actDList = oldRoom.activeDoorList;
        List<GameObject> neighDList = oldRoom.neighboringDoorList;
        List<Room> neighRList = oldRoom.neighboringRoomList;

        Transform itemC = oldRoom.itemContainer;
        Transform monsterC = oldRoom.monsterContainer;
        Transform defaultC = oldRoom.defaultContainer;

        // Видаляємо старий базовий компонент Room
        DestroyImmediate(oldRoom);

        // Додаємо новий компонент підкласу
        T newRoom = go.AddComponent<T>();

        // Переконуємось, що об'єкт активний (оскільки OnDestroy старого компонента міг деактивувати його)
        go.SetActive(true);

        // Відновлюємо посилання у новому компоненті
        newRoom.coordinate = coord;
        newRoom.roomType = type;
        newRoom.level = lvl;
        newRoom.doorList = dList;
        newRoom.activeDoorList = actDList;
        newRoom.neighboringDoorList = neighDList;
        newRoom.neighboringRoomList = neighRList;

        newRoom.itemContainer = itemC;
        newRoom.monsterContainer = monsterC;
        newRoom.defaultContainer = defaultC;

        return newRoom;
    }

    private void RebuildNeighboringRooms()
    {
        foreach (Room room in roomArray)
        {
            if (room != null)
            {
                room.neighboringRoomList.Clear();
                int x = (int)room.coordinate.x;
                int y = (int)room.coordinate.y;
                if (x + 1 < MAX_SIZE && roomArray[x + 1, y] != null) room.neighboringRoomList.Add(roomArray[x + 1, y]);
                if (x - 1 >= 0 && roomArray[x - 1, y] != null) room.neighboringRoomList.Add(roomArray[x - 1, y]);
                if (y - 1 >= 0 && roomArray[x, y - 1] != null) room.neighboringRoomList.Add(roomArray[x, y - 1]);
                if (y + 1 < MAX_SIZE && roomArray[x, y + 1] != null) room.neighboringRoomList.Add(roomArray[x, y + 1]);
            }
        }
    }

    /// <summary>
    /// 进入初始房间
    /// </summary>
    private void MoveToStartRoom()
    {
        StartCoroutine(MoveToDesignateRoom(Vector2.zero));
    }


    /// <summary>
    /// 移动到下一个房间
    /// </summary>
    /// <param name="MoveDirection"></param>
    public void MoveToNextRoom(Vector2 MoveDirection)
    {
        if (currentRoom.isCleared)
        {
            StartCoroutine(MoveToDesignateRoom(MoveDirection));
        }
    }

    private IEnumerator MoveToDesignateRoom(Vector2 MoveDirection)
    {
        Camera mainCamera = GameManager.Instance.myCamera;
        float delaySeconds = 0.3f;

        int x = (int)currentRoom.coordinate.x + (int)MoveDirection.y;
        int y = (int)currentRoom.coordinate.y + (int)MoveDirection.x;
        currentRoom = roomArray[x, y];

        //如果没去过该房间便生成房间内容
        if (!currentRoom.isArrived)
        {
            currentRoom.GenerateRoomContent();
            currentRoom.isArrived = true;
        }

        if (AdaptiveDifficultyManager.Instance != null)
        {
            AdaptiveDifficultyManager.Instance.OnRoomEntered(currentRoom);
        }

        // Set top-left room task text
        if (UIManager.Instance != null)
        {
            string taskStr = "КІМНАТА: Знайдіть вихід!";
            switch (currentRoom.roomType)
            {
                case RoomType.Start:
                    taskStr = "СТАРТОВА КІМНАТА: Знайдіть кімнату боса!";
                    break;
                case RoomType.Normal:
                    taskStr = "БІЙ: Знищіть усіх ворогів!";
                    break;
                case RoomType.Boss:
                    taskStr = "БОС: Переможіть головного ворога та знайдіть люк!";
                    break;
                case RoomType.Treasure:
                    taskStr = "СКАРБНИЦЯ: Візьміть безкоштовний артефакт!";
                    break;
                case RoomType.Shop:
                    taskStr = "МАГАЗИН: Купіть спорядження за монети!";
                    break;
                case RoomType.SafeRoom:
                    taskStr = "СПОКІЙНА КІМНАТА: Відновіть сили та здоров'я!";
                    break;
                case RoomType.Challenge:
                    string challName = currentRoom.GetType().Name;
                    switch (challName)
                    {
                        case "ThreeCardsMonte":
                            taskStr = "ГРА: Знайдіть правильну карту з трьох!";
                            break;
                        case "SequenceMemoryChallenge":
                            taskStr = "ПАМ'ЯТЬ: Повторіть послідовність активації плит!";
                            break;
                        case "RouletteWheel":
                            taskStr = "РУЛЕТКА: Крутіть рулетку та випробуйте вдачу!";
                            break;
                        case "TimeMazeChallenge":
                            taskStr = "ЛАБІРИНТ: Пройдіть лабіринт за обмежений час!";
                            break;
                        case "ObserverRoom":
                            taskStr = "ВИЖИВАННЯ: Не рухайтеся, коли очі спостерігача відчинені!";
                            break;
                        case "ChangingSafeZones":
                            taskStr = "ТАЙМІНГ: Встигніть стати у зелені безпечні зони!";
                            break;
                        default:
                            taskStr = "ВИПРОБУВАННЯ: Виконайте завдання кімнати!";
                            break;
                    }
                    break;
            }
            UIManager.Instance.SetRoomTaskText(taskStr);
        }

        //更新小地图
        UI.miniMap.UpdateMiniMap(MoveDirection);
        // UpdateGridGraph();

        //暂停并移动玩家
        player.PlayerPause();
        player.transform.position += (Vector3)MoveDirection * 6.5f;

        //移动镜头
        Vector3 originPos = mainCamera.transform.position;
        Vector3 targetPos = currentRoom.transform.position;
        targetPos.y += 1;
        targetPos.z += mainCamera.transform.position.z;
        float time = 0;
        while (time <= delaySeconds)
        {
            mainCamera.transform.position =
                Vector3
                    .Lerp(originPos,
                    targetPos,
                    (1 / delaySeconds) * (time += Time.deltaTime));
            yield return null;
        }

        //恢复玩家暂停
        if (MoveDirection == Vector2.zero)
        {
            int currentDepth = GameManager.Instance != null ? GameManager.Instance.depth : 0;
            bool isTutorial = GameManager.Instance != null && GameManager.Instance.isTutorialMode;
            if ((currentDepth == 0 || isTutorial) && StoryManager.Instance != null && currentDepth >= 0 && currentDepth <= 4)
            {
                StoryManager.Instance.ShowLevelTutorial(currentDepth, () => {
                    if (player != null) player.PlayerResume();
                });
            }
            else
            {
                player.PlayerResume();
            }
        }
        else
        {
            player.PlayerResume();
        }
    }

}