using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Goods : Item
{
    public enum Goodstype { Item, Pickup }
    public Goodstype goodstype;

    private GameObject newItem;
    private int price;

    private void Start()
    {
        GameObject prototypeItem = null;
        float ddaFactor = 1f;
        if (AdaptiveDifficultyManager.Instance != null)
        {
            ddaFactor = 0.5f + AdaptiveDifficultyManager.Instance.SkillIndex; // scale from 0.5x to 1.5x
        }

        //获取道具
        switch (goodstype)
        {
            case Goodstype.Item:
                prototypeItem = pools.GetItem(ItemPoolType.Shop).gameObject;
                price = Mathf.Max(1, Mathf.RoundToInt(15 * ddaFactor));
                break;
            case Goodstype.Pickup:
                prototypeItem = pools.GetPickupGoods();
                price = Mathf.Max(1, Mathf.RoundToInt(3 * ddaFactor));
                break;
            default:
                break;
        }


        //生成道具
        Transform itemContainer = level.currentRoom.itemContainer;

        newItem = level.currentRoom.GenerateGameObjectWithPosition(prototypeItem, transform.position, itemContainer);

        //设置价格，UI
        GetComponentInChildren<TextMesh>().text = price.ToString();
        switch (goodstype)
        {
            case Goodstype.Item:
                GetComponent<SpriteRenderer>().enabled = false;
                newItem.gameObject.GetComponent<Collider2D>().enabled = false;
                break;
            case Goodstype.Pickup:
                GetComponent<SpriteRenderer>().sprite = newItem.gameObject.GetComponent<SpriteRenderer>().sprite;
                newItem.gameObject.SetActive(false);
                break;
            default:
                break;
        }

    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.transform.CompareTag("Player") && IsTrigger())
        {
            Effect();
            After();
        }
    }

    protected override bool IsTrigger()
    {
        return player.coins >= price;
    }

    protected override void Effect()
    {
        player.coins -= price;
        UI.UpdateStatus();

        switch (goodstype)
        {
            case Goodstype.Item:
                newItem.gameObject.GetComponent<Collider2D>().enabled = true;
                if (AdaptiveDifficultyManager.Instance != null && AdaptiveDifficultyManager.Instance.SkillIndex < 0.45f)
                {
                    player.EnableSuperWeapon(30f);
                }
                break;
            case Goodstype.Pickup:
                newItem.gameObject.SetActive(true);
                break;
            default:
                break;
        }

    }

    protected override void After()
    {
        Destroy(gameObject);
    }
}
