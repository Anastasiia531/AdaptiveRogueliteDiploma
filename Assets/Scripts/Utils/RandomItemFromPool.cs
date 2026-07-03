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

        // DDA / Isaac adjustment: reduce number of passive items on the map
        float skipItemChance = 0.35f; // 35% chance by default to skip spawning a passive item
        if (AdaptiveDifficultyManager.Instance != null)
        {
            // SkillIndex high (plays well) -> higher skip chance (fewer items, up to 55%)
            // SkillIndex low (plays poorly) -> lower skip chance (more items, down to 15%)
            skipItemChance = Mathf.Lerp(0.15f, 0.55f, AdaptiveDifficultyManager.Instance.SkillIndex);
        }

        if (Random.value < skipItemChance)
        {
            GameObject pickupGo = level.pools.GetPickupGoods();
            GameObject firstPickup = null;
            if (pickupGo != null)
            {
                firstPickup = level.manager.GenerateGameObjectInCurrentRoom(pickupGo, transform.position);
            }
            // 50% chance to spawn an extra pickup alongside the first one
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
