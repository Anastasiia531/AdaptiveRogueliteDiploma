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
}
