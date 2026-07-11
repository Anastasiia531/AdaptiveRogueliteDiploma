using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomItemFromPool : MonoBehaviour, IsRandomGameObject
{
    public ItemPoolType type;

    void Start()
    {
        Generate();
    }

    public GameObject Generate()
    {
        Level level = GameManager.Instance.level;

        // Find parent room to check its type
        Room parentRoom = GetComponentInParent<Room>();
        RoomType roomType = (parentRoom != null) ? parentRoom.roomType : RoomType.Normal;

        // In The Binding of Isaac, item pedestals NEVER spawn in Normal or Start rooms.
        // They only appear in Treasure Rooms, Shops, Boss Rooms, or Challenge Rooms.
        if (roomType == RoomType.Normal || roomType == RoomType.Start)
        {
            // Always replace with random pickups (coins, hearts, keys, bombs)
            GameObject pickupGo = level.pools.GetPickupGoods();
            GameObject firstPickup = null;
            if (pickupGo != null)
            {
                firstPickup = level.manager.GenerateGameObjectInCurrentRoom(pickupGo, transform.position);
            }
            if (Random.value > 0.5f)
            {
                GameObject extraPickup = level.pools.GetPickupGoods();
                if (extraPickup != null)
                {
                    level.manager.GenerateGameObjectInCurrentRoom(extraPickup, transform.position + new Vector3(0.8f, 0f, 0f));
                }
            }
            Destroy(gameObject);
            return firstPickup;
        }

        // DDA / Isaac adjustment: reduce number of items in other rooms (Shops, Challenges)
        float skipItemChance = 0.35f; // 35% chance by default to skip spawning an item
        if (AdaptiveDifficultyManager.Instance != null)
        {
            // SkillIndex high (plays well) -> higher skip chance (fewer items, up to 0.60f)
            // SkillIndex low (plays poorly) -> lower skip chance (more items, down to 0.10f)
            skipItemChance = Mathf.Lerp(0.10f, 0.60f, AdaptiveDifficultyManager.Instance.SkillIndex);
        }

        // For Treasure Rooms and Boss Rooms, we always want to spawn an item (since they are floor milestones),
        // but for Shops or Challenges, we apply the skip chance.
        bool isMilestoneRoom = (roomType == RoomType.Treasure || roomType == RoomType.Boss);
        if (!isMilestoneRoom && Random.value < skipItemChance)
        {
            GameObject pickupGo = level.pools.GetPickupGoods();
            GameObject firstPickup = null;
            if (pickupGo != null)
            {
                firstPickup = level.manager.GenerateGameObjectInCurrentRoom(pickupGo, transform.position);
            }
            if (Random.value > 0.5f)
            {
                GameObject extraPickup = level.pools.GetPickupGoods();
                if (extraPickup != null)
                {
                    level.manager.GenerateGameObjectInCurrentRoom(extraPickup, transform.position + new Vector3(0.8f, 0f, 0f));
                }
            }
            Destroy(gameObject);
            return firstPickup;
        }

        Item item = level.pools.GetItem(type);
        if (item == null)
        {
            Destroy(gameObject);
            return null;
        }
        GameObject go = level.manager.GenerateGameObjectInCurrentRoom(item.gameObject, transform.position);
        Destroy(gameObject);
        return go;
    }
}
