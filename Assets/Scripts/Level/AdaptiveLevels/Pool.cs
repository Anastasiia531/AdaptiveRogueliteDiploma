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

        // Load items and filter out Python completely so it is never spawned!
        foreach (var item in TreasureRoomItemPool.itemList)
        {
            if (item != null && !item.name.Contains("Python") && item.GetComponent<Python>() == null)
            {
                TreasureRoomItemList.Add(item);
            }
        }
        foreach (var item in BossRoomItemPool.itemList)
        {
            if (item != null && !item.name.Contains("Python") && item.GetComponent<Python>() == null)
            {
                BossRoomItemList.Add(item);
            }
        }
        foreach (var item in ShopItemPool.itemList)
        {
            if (item != null && !item.name.Contains("Python") && item.GetComponent<Python>() == null)
            {
                ShopItemList.Add(item);
            }
        }
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
                if (normalRoom == null || normalRoom.Count == 0)
                {
                    normalRoom.Clear();
                    normalRoom.AddRange(Resources.LoadAll<RoomLayout>(roomLayoutFileFolderPath[1]));
                }
                // Use false so we do not consume/remove layouts from the normal room pool
                return GetRandomRoomLayout(normalRoom, false);
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
        RoomLayout go;
        int index = Random.Range(0, list.Count);
        go = list[index];
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

        // DDA / Isaac adjustment: make weapons rarer and scale based on player performance (DDA)
        if (Item != null && Item is Weapon)
        {
            float weaponChance = 0.20f; // 20% default chance to keep the weapon
            if (AdaptiveDifficultyManager.Instance != null)
            {
                // plays well (SkillIndex=1) -> weapons are even rarer (10% chance)
                // plays poorly (SkillIndex=0) -> weapons are more common (35% chance)
                weaponChance = Mathf.Lerp(0.35f, 0.10f, AdaptiveDifficultyManager.Instance.SkillIndex);
            }

            if (Random.value > weaponChance)
            {
                // Replace with a passive item from the pool!
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
                    // Put the weapon back so it can be rolled later
                    sourcePool.Add(Item);
                    Item = replacement;
                }
            }
        }
        return Item;
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
