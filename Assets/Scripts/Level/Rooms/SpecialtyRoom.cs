using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpecialtyRoom : Room
{
    public override void Initialize()
    {
        base.Initialize();
    }

    public override void GenerateRoomContent()
    {
        // Load default layout content (pedestals, shop items if any)
        base.GenerateRoomContent();
        
        isCleared = true;
        OpenActivatedDoor();

        if (roomType == RoomType.SafeRoom)
        {
            SpawnSafeRoomContent();
        }
        else if (roomType == RoomType.Curse)
        {
            SpawnCurseRoomContent();
        }
        else if (roomType == RoomType.Secret)
        {
            SpawnSecretRoomContent();
        }
    }

    private void SpawnSafeRoomContent()
    {
        if (level != null && level.pools != null)
        {
            // Heal player by 2 HP upon entering the SafeRoom
            if (GameManager.Instance != null && GameManager.Instance.player != null)
            {
                GameManager.Instance.player.AddHealth(2);
            }

            // Spawn 1-2 helpful pickup items (hearts, coins, etc.) near the center
            GameObject pickup1 = level.pools.GetPickupGoods();
            if (pickup1 != null)
            {
                GenerateGameObjectWithCoordinate(pickup1, new Vector2(0f, 0f), itemContainer);
            }

            if (Random.value > 0.5f)
            {
                GameObject pickup2 = level.pools.GetPickupGoods();
                if (pickup2 != null)
                {
                    GenerateGameObjectWithCoordinate(pickup2, new Vector2(1.5f, 0f), itemContainer);
                }
            }
        }
    }

    private void SpawnCurseRoomContent()
    {
        if (level != null && level.pools != null)
        {
            // Spawn an item pedestal in the center of the Curse Room
            Item item = level.pools.GetItem(ItemPoolType.TreasureRoom);
            if (item != null)
            {
                GenerateGameObjectWithCoordinate(item.gameObject, new Vector2(0f, 0f), itemContainer);
            }
            // Spawn 1-2 spikes or negative pickups/chests
            if (Random.value > 0.5f)
            {
                GameObject pickup = level.pools.GetPickupGoods();
                if (pickup != null)
                {
                    GenerateGameObjectWithCoordinate(pickup, new Vector2(-2f, -1f), itemContainer);
                }
            }
        }
    }

    private void SpawnSecretRoomContent()
    {
        if (level != null && level.pools != null)
        {
            // Secret Room contains high reward: 1 item pedestal + multiple pickups (coins/bombs)
            Item item = level.pools.GetItem(ItemPoolType.TreasureRoom);
            if (item != null)
            {
                GenerateGameObjectWithCoordinate(item.gameObject, new Vector2(0f, 0f), itemContainer);
            }
            
            // Spawn 2-3 pickups (gold/bombs/chests)
            float[] offsetsX = { -2f, 2f, 0f };
            float[] offsetsY = { 2f, 2f, -2f };
            int pickupCount = Random.Range(2, 4);
            for (int i = 0; i < pickupCount; i++)
            {
                GameObject pickup = level.pools.GetPickupGoods();
                if (pickup != null)
                {
                    GenerateGameObjectWithCoordinate(pickup, new Vector2(offsetsX[i], offsetsY[i]), itemContainer);
                }
            }
        }
    }
}
