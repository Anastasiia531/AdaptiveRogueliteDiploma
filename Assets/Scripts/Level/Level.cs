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

        if (GameManager.Instance != null && GameManager.Instance.isTutorialMode)
        {
            // Clear old rooms
            Array.Clear(roomArray, 0, roomArray.Length);
            for (int i = 0; i < transform.childCount; i++)
            {
                Destroy(transform.GetChild(i).gameObject);
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
            roomArray[outsetX - 1, outsetY] = ConvertRoomComponent<ChallengeRoom>(cRoom);

            RebuildNeighboringRooms();

            foreach (Room room in roomArray)
            {
                if (room != null)
                {
                    room.Initialize();
                }
            }
            return;
        }

        //储存备选生成房间的位置列表
        List<Vector2> alternativeRoomList = new List<Vector2>();
        List<Vector2> hasBeenRemoveRoomList = new List<Vector2>();

        //单门房间列表
        List<Room> singleDoorRoomList = new List<Room>();

        while (singleDoorRoomList.Count < 3)
        {
            //清空已生成的房间
            Array.Clear(roomArray, 0, roomArray.Length);
            for (int i = 0; i < transform.childCount; i++)
            {
                Destroy(transform.GetChild(i).gameObject);
            }

            //清空相关数据
            alternativeRoomList.Clear();
            hasBeenRemoveRoomList.Clear();
            singleDoorRoomList.Clear();

            //创建起始房间
            int outsetX = roomArray.GetLength(0) / 2;
            int outsetY = roomArray.GetLength(1) / 2;
            Room lastRoom =
                roomArray[outsetX, outsetY] =
                    CreateRoom(new Vector2(outsetX, outsetY));
            currentRoom = lastRoom;

            //创建其他房间
            Action<int, int> action =
                (newX, newY) =>
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
                int x = (int)lastRoom.coordinate.x;
                int y = (int)lastRoom.coordinate.y;

                action(x + 1, y);
                action(x - 1, y);
                action(x, y - 1);
                action(x, y + 1);

                Vector2 newRoomCoordinate =
                    alternativeRoomList[UnityEngine
                        .Random
                        .Range(0, alternativeRoomList.Count)];
                lastRoom =
                    roomArray[(int)newRoomCoordinate.x,
                    (int)newRoomCoordinate.y] = CreateRoom(newRoomCoordinate);
                alternativeRoomList.Remove(newRoomCoordinate);
            }
            LinkDoors();
            //计算单门房间数量
            foreach (Room room in roomArray)
            {
                if (
                    room != null &&
                    room.ActiveDoorCount == 1 &&
                    room != currentRoom
                )
                {
                    singleDoorRoomList.Add(room);
                }
            }
        }
        SetRoomsType(singleDoorRoomList);
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

    /// <summary>
    /// 打通各个房间相连的门，并记录相连信息
    /// </summary>
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

    /// <summary>
    /// 设置各个房间的类型并动态转换组件
    /// </summary>
    private void SetRoomsType(List<Room> singleDoorRoomList)
    {
        // 1. Спершу всі кімнати робимо звичайними (Normal)
        foreach (Room room in roomArray)
        {
            if (room != null)
            {
                room.roomType = RoomType.Normal;
            }
        }

        // Скарбниця
        singleDoorRoomList[singleDoorRoomList.Count - 3].roomType = RoomType.Treasure;
        // Boss
        singleDoorRoomList[singleDoorRoomList.Count - 1].roomType = RoomType.Boss;
        // Магазин
        singleDoorRoomList[singleDoorRoomList.Count - 2].roomType = RoomType.Shop;
        // Стартова
        currentRoom.roomType = RoomType.Start;

        // Збираємо список усіх звичайних кімнат (не спеціальних з 1 дверима)
        List<Room> normalRooms = new List<Room>();
        foreach (Room room in roomArray)
        {
            if (room != null && room.roomType == RoomType.Normal)
            {
                normalRooms.Add(room);
            }
        }

        // Призначаємо хоча б 1 ChallengeRoom (випробування/міні-гру)
        if (normalRooms.Count > 0)
        {
            Room challengeChoice = normalRooms[UnityEngine.Random.Range(0, normalRooms.Count)];
            challengeChoice.roomType = RoomType.Challenge;
            normalRooms.Remove(challengeChoice);
        }

        // З ймовірністю 50% призначаємо SafeRoom (Спокійна кімната)
        if (normalRooms.Count > 0 && UnityEngine.Random.value > 0.5f)
        {
            Room safeChoice = normalRooms[UnityEngine.Random.Range(0, normalRooms.Count)];
            safeChoice.roomType = RoomType.SafeRoom;
            normalRooms.Remove(safeChoice);
        }

        // 2. Конвертуємо компоненти кімнат у відповідні підкласи
        for (int x = 0; x < MAX_SIZE; x++)
        {
            for (int y = 0; y < MAX_SIZE; y++)
            {
                Room oldRoom = roomArray[x, y];
                if (oldRoom == null) continue;

                Room newRoom = null;
                switch (oldRoom.roomType)
                {
                    case RoomType.Start:
                    case RoomType.Normal:
                    case RoomType.Boss:
                        newRoom = ConvertRoomComponent<CombatRoom>(oldRoom);
                        break;

                    case RoomType.Treasure:
                    case RoomType.Shop:
                    case RoomType.SafeRoom:
                        newRoom = ConvertRoomComponent<SpecialtyRoom>(oldRoom);
                        break;

                    case RoomType.Challenge:
                        // Вибираємо випадкову міні-гру з 12 доступних
                        int challengeIndex = UnityEngine.Random.Range(0, 12);
                        switch (challengeIndex)
                        {
                            case 0: newRoom = ConvertRoomComponent<GhostSurvivalChallenge>(oldRoom); break;
                            case 1: newRoom = ConvertRoomComponent<ThreeCardsMonte>(oldRoom); break;
                            case 2: newRoom = ConvertRoomComponent<QuickTileReaction>(oldRoom); break;
                            case 3: newRoom = ConvertRoomComponent<SequenceMemoryChallenge>(oldRoom); break;
                            case 4: newRoom = ConvertRoomComponent<SacrificeAltar>(oldRoom); break;
                            case 5: newRoom = ConvertRoomComponent<RouletteWheel>(oldRoom); break;
                            case 6: newRoom = ConvertRoomComponent<TimeMazeChallenge>(oldRoom); break;
                            case 7: newRoom = ConvertRoomComponent<CobwebDodgeChallenge>(oldRoom); break;
                            case 8: newRoom = ConvertRoomComponent<ObserverRoom>(oldRoom); break;
                            case 9: newRoom = ConvertRoomComponent<BombPushChallenge>(oldRoom); break;
                            case 10: newRoom = ConvertRoomComponent<SkyTearsSurvivalChallenge>(oldRoom); break;
                            default: newRoom = ConvertRoomComponent<ChangingSafeZones>(oldRoom); break;
                        }
                        break;
                }

                roomArray[x, y] = newRoom;

                if (oldRoom == currentRoom)
                {
                    currentRoom = newRoom;
                }
            }
        }

        // 3. Відновлюємо списки сусідів для нових компонентів кімнат
        RebuildNeighboringRooms();

        // 4. Ініціалізуємо всі кімнати
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
                        case "GhostSurvivalChallenge":
                            taskStr = "ВИПРОБУВАННЯ: Ухиляйтеся від привидів!";
                            break;
                        case "ThreeCardsMonte":
                            taskStr = "ГРА: Знайдіть правильну карту з трьох!";
                            break;
                        case "QuickTileReaction":
                            taskStr = "РЕАКЦІЯ: Наступайте лише на зелені плитки!";
                            break;
                        case "SequenceMemoryChallenge":
                            taskStr = "ПАМ'ЯТЬ: Повторіть послідовність активації плит!";
                            break;
                        case "SacrificeAltar":
                            taskStr = "ВІВТАР: Пожертвуйте HP заради цінних предметів!";
                            break;
                        case "RouletteWheel":
                            taskStr = "РУЛЕТКА: Крутіть рулетку та випробуйте вдачу!";
                            break;
                        case "TimeMazeChallenge":
                            taskStr = "ЛАБІРИНТ: Пройдіть лабіринт за обмежений час!";
                            break;
                        case "CobwebDodgeChallenge":
                            taskStr = "СПРИТНІСТЬ: Ухиляйтеся від павутини та дійдіть до виходу!";
                            break;
                        case "ObserverRoom":
                            taskStr = "ВИЖИВАННЯ: Не рухайтеся, коли очі спостерігача відчинені!";
                            break;
                        case "BombPushChallenge":
                            taskStr = "ГОЛОВОЛОМКА: Штовхайте бомби, щоб розчистити шлях!";
                            break;
                        case "SkyTearsSurvivalChallenge":
                            taskStr = "УХИЛЯННЯ: Уникайте сліз, що падають зі стелі!";
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