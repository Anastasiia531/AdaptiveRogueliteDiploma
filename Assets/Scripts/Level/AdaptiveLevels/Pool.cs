using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pool : MonoBehaviour
{
    [Header("房间池")]
    List<RoomLayout> startRoom = new List<RoomLayout>();
    List<RoomLayout> normalRoom = new List<RoomLayout>();
    List<RoomLayout> bossRoom = new List<RoomLayout>();
    List<RoomLayout> treasureRoom = new List<RoomLayout>();
    List<RoomLayout> shopRoom = new List<RoomLayout>();

    string[] roomLayoutFileFolderPath = new[]
{
        "RoomLayout/StartRoom",
        "RoomLayout/NormalRoom",
        "RoomLayout/BossRoom",
        "RoomLayout/TreasureRoom",
        "RoomLayout/ShopRoom",
    };

    [Header("道具池")]
    [SerializeField]
    private Item defaultItem;//默认道具，道具池为空时返回该道具

    [Space(10)]
    [SerializeField]
    private ItemPool TreasureRoomItemPool;
    private List<Item> TreasureRoomItemList = new List<Item>();
    [SerializeField]
    private ItemPool BossRoomItemPool;
    private List<Item> BossRoomItemList = new List<Item>();
    [SerializeField]
    private ItemPool ShopItemPool;
    private List<Item> ShopItemList = new List<Item>();

    [Header("房间清空奖励")]
    [SerializeField]
    private GameObject bossRoomClearingReward;
    [SerializeField]
    private GameObject normalRoomClearingReward;

    [Header("拾取物商品列表")]
    [SerializeField]
    private RandomGameObjectTable pickupGoodsTable;

    [Header("怪物列表")]
    [SerializeField]
    private RandomGameObjectTable minionTable;
    [SerializeField]
    private RandomGameObjectTable bossTable;

    private void Awake()
    {
        startRoom.AddRange(Resources.LoadAll<RoomLayout>(roomLayoutFileFolderPath[0]));
        normalRoom.AddRange(Resources.LoadAll<RoomLayout>(roomLayoutFileFolderPath[1]));
        bossRoom.AddRange(Resources.LoadAll<RoomLayout>(roomLayoutFileFolderPath[2]));
        treasureRoom.AddRange(Resources.LoadAll<RoomLayout>(roomLayoutFileFolderPath[3]));
        shopRoom.AddRange(Resources.LoadAll<RoomLayout>(roomLayoutFileFolderPath[4]));

        Item pythonItem = null;
        Item passiveTemplate = null;

        // Load items and find Python + template item
        foreach (var item in TreasureRoomItemPool.itemList)
        {
            if (item != null)
            {
                if (item.name.Contains("Python") || item.GetComponent<Python>() != null)
                {
                    pythonItem = item;
                }
                else
                {
                    TreasureRoomItemList.Add(item);
                    if (passiveTemplate == null && !(item is Weapon) && item.GetComponent<Weapon>() == null)
                    {
                        passiveTemplate = item;
                    }
                }
            }
        }

        foreach (var item in BossRoomItemPool.itemList)
        {
            if (item != null && !item.name.Contains("Python") && item.GetComponent<Python>() == null)
            {
                // In The Binding of Isaac, boss drops are ALWAYS stat/HP upgrades, NEVER weapon replacements
                if (!(item is Weapon) && item.GetComponent<Weapon>() == null)
                {
                    BossRoomItemList.Add(item);
                }
            }
        }

        foreach (var item in ShopItemPool.itemList)
        {
            if (item != null && !item.name.Contains("Python") && item.GetComponent<Python>() == null)
            {
                ShopItemList.Add(item);
            }
        }

        // Add Python to shop!
        if (pythonItem != null && !ShopItemList.Contains(pythonItem))
        {
            ShopItemList.Add(pythonItem);
        }

        // Generate and add temporary/new passive items programmatically
        if (passiveTemplate != null)
        {
            // 1. BookOfBelial: red color. Added to TreasureRoom and Shop pools.
            BookOfBelial belial = CreateRuntimeItemPrefab<BookOfBelial>(passiveTemplate, "BookOfBelial", new Color(1f, 0.2f, 0.2f, 1f));
            if (belial != null)
            {
                TreasureRoomItemList.Add(belial);
                ShopItemList.Add(belial);
            }

            // 2. Gamekid: bright green color. Added to TreasureRoom and Shop pools.
            Gamekid gamekid = CreateRuntimeItemPrefab<Gamekid>(passiveTemplate, "Gamekid", new Color(0.2f, 1f, 0.2f, 1f));
            if (gamekid != null)
            {
                TreasureRoomItemList.Add(gamekid);
                ShopItemList.Add(gamekid);
            }

            // 3. Compass: bright gold/yellow color. Added to Shop pool.
            TheCompass compass = CreateRuntimeItemPrefab<TheCompass>(passiveTemplate, "Compass", new Color(1f, 0.85f, 0f, 1f));
            if (compass != null)
            {
                ShopItemList.Add(compass);
            }
        }
    }

    private T CreateRuntimeItemPrefab<T>(Item template, string name, Color color) where T : Item
    {
        if (template == null) return null;
        GameObject go = Instantiate(template.gameObject);
        go.name = name;
        go.SetActive(false);
        DontDestroyOnLoad(go);

        // Remove the template component
        Component comp = go.GetComponent(template.GetType());
        if (comp != null) DestroyImmediate(comp);

        // Add the new component
        T newItem = go.AddComponent<T>();

        // Set distinctive color on the SpriteRenderer
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = color;
        }

        return newItem;
    }

    public RoomLayout GetRoomLayout(RoomType type)
    {
        switch (type)
        {
            case RoomType.Start:
                if (startRoom == null || startRoom.Count == 0)
                {
                    startRoom.Clear();
                    startRoom.AddRange(Resources.LoadAll<RoomLayout>(roomLayoutFileFolderPath[0]));
                }
                return GetRandomRoomLayout(startRoom, false);
            case RoomType.Normal:
                if (normalRoom == null || normalRoom.Count == 0)
                {
                    normalRoom.Clear();
                    normalRoom.AddRange(Resources.LoadAll<RoomLayout>(roomLayoutFileFolderPath[1]));
                }
                return GetRandomRoomLayout(normalRoom, true);
            case RoomType.Challenge:
            case RoomType.SafeRoom:
            case RoomType.Curse:
            case RoomType.Secret:
                if (startRoom == null || startRoom.Count == 0)
                {
                    startRoom.Clear();
                    startRoom.AddRange(Resources.LoadAll<RoomLayout>(roomLayoutFileFolderPath[0]));
                }
                // Use false so we do not consume/remove layouts from the start room pool
                return GetRandomRoomLayout(startRoom, false);
            case RoomType.Boss:
                if (bossRoom == null || bossRoom.Count == 0)
                {
                    bossRoom.Clear();
                    bossRoom.AddRange(Resources.LoadAll<RoomLayout>(roomLayoutFileFolderPath[2]));
                }
                return GetRandomRoomLayout(bossRoom, false);
            case RoomType.Treasure:
                if (treasureRoom == null || treasureRoom.Count == 0)
                {
                    treasureRoom.Clear();
                    treasureRoom.AddRange(Resources.LoadAll<RoomLayout>(roomLayoutFileFolderPath[3]));
                }
                return GetRandomRoomLayout(treasureRoom, false);
            case RoomType.Shop:
                if (shopRoom == null || shopRoom.Count == 0)
                {
                    shopRoom.Clear();
                    shopRoom.AddRange(Resources.LoadAll<RoomLayout>(roomLayoutFileFolderPath[4]));
                }
                return GetRandomRoomLayout(shopRoom, false);
            default:
                return null;
        }
    }
    RoomLayout GetRandomRoomLayout(List<RoomLayout> list, bool isRemove)
    {
        if (list == null) return null;
        list.RemoveAll(item => item == null);
        if (list.Count == 0) return null;

        int index = Random.Range(0, list.Count);
        RoomLayout go = list[index];
        if (isRemove) { list.RemoveAt(index); }
        return go;
    }

    /// <summary>
    /// 从各个道具池中获取道具
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public Item GetItem(ItemPoolType type)
    {
        Item Item;
        switch (type)
        {
            case ItemPoolType.TreasureRoom:
                Item = GetRamdomItem(TreasureRoomItemList);
                break;
            case ItemPoolType.BossRoom:
                Item = GetRamdomItem(BossRoomItemList);
                break;
            case ItemPoolType.Shop:
                Item = GetRamdomItem(ShopItemList);
                break;
            default:
                Item = null;
                break;
        }

        if (Item != null)
        {
            // Reduce Broken Heart frequency by 80% by replacing it with another passive
            if (Item.name.Contains("TheBrokenHeart") || Item.GetComponent<TheBrokenHeart>() != null)
            {
                if (Random.value < 0.80f)
                {
                    Item replacement = GetPassiveReplacement(type, Item);
                    if (replacement != null)
                    {
                        Item = replacement;
                    }
                }
            }

            Level activeLevel = GameManager.Instance != null ? GameManager.Instance.level : null;
            bool cannotHaveWeapon = false;

            // Rule 1: Weapons can ONLY be obtained in Shop or BossRoom (NOT in TreasureRoom or other rooms)
            if (type == ItemPoolType.TreasureRoom)
            {
                cannotHaveWeapon = true;
            }

            // Rule 2: No more than 1 weapon per floor
            if (activeLevel != null && activeLevel.weaponsSpawnedOnThisFloor >= 1)
            {
                cannotHaveWeapon = true;
            }

            if (Item is Weapon)
            {
                if (cannotHaveWeapon)
                {
                    // Force replace with a passive item
                    Item replacement = GetPassiveReplacement(type, Item);
                    if (replacement != null)
                    {
                        Item = replacement;
                    }
                    else if (defaultItem != null)
                    {
                        Item = defaultItem;
                    }
                }
                else
                {
                    // Weapon allowed! We apply the DDA / Isaac rarity chances
                    float weaponChance = 0.12f; // 12% default chance
                    if (AdaptiveDifficultyManager.Instance != null)
                    {
                        weaponChance = Mathf.Lerp(0.20f, 0.05f, AdaptiveDifficultyManager.Instance.SkillIndex);
                    }

                    if (Random.value > weaponChance)
                    {
                        // Replace with passive
                        Item replacement = GetPassiveReplacement(type, Item);
                        if (replacement != null)
                        {
                            Item = replacement;
                        }
                    }
                    else
                    {
                        // Weapon successfully spawned! Increment count
                        if (activeLevel != null)
                        {
                            activeLevel.weaponsSpawnedOnThisFloor++;
                        }
                    }
                }
            }
        }

        return Item;
    }

    private Item GetPassiveReplacement(ItemPoolType type, Item originalWeapon)
    {
        List<Item> passives = new List<Item>();
        List<Item> sourcePool = null;
        if (type == ItemPoolType.TreasureRoom) sourcePool = TreasureRoomItemList;
        else if (type == ItemPoolType.BossRoom) sourcePool = BossRoomItemList;
        else if (type == ItemPoolType.Shop) sourcePool = ShopItemList;

        if (sourcePool != null)
        {
            foreach (Item candidate in sourcePool)
            {
                if (candidate != null && !(candidate is Weapon))
                {
                    passives.Add(candidate);
                }
            }
        }

        if (passives.Count > 0)
        {
            Item replacement = passives[Random.Range(0, passives.Count)];
            sourcePool.Remove(replacement);
            // Put the weapon back so it can be rolled later on other floors
            if (sourcePool != null)
            {
                sourcePool.Add(originalWeapon);
            }
            return replacement;
        }

        return null;
    }
    private Item GetRamdomItem(List<Item> list)
    {
        if (list.Count == 0)
        {
            return defaultItem;
        }

        int index = Random.Range(0, list.Count);
        Item go = list[index];
        list.RemoveAt(index);
        return go;
    }

    /// <summary>
    /// 获取清空房间的奖励
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public GameObject GetRoomClearingReward(RoomType type)
    {
        GameObject reward = null;
        switch (type)
        {
            case RoomType.Normal:
                reward = normalRoomClearingReward;
                break;
            case RoomType.Boss:
                reward = bossRoomClearingReward;
                break;
            default:
                break;
        }
        return reward;
    }

    /// <summary>
    /// 获取拾取物商品
    /// </summary>
    /// <returns></returns>
    public GameObject GetPickupGoods()
    {
        return pickupGoodsTable.GetGameObject();
    }

    /// <summary>
    /// 获取怪物
    /// </summary>
    /// <returns></returns>
    public GameObject GetMonster(MonsterType monster)
    {
        if (monster == MonsterType.Minion)
            return minionTable.GetGameObject();
        if (monster == MonsterType.Boss)
            return bossTable.GetGameObject();
        else return null;
    }
}
