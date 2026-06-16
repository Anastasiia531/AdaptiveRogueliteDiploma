using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SacrificeAltar : ChallengeRoom
{
    private List<GameObject> buttons = new List<GameObject>();
    private GameObject altarVisual;

    protected override void StartChallenge()
    {
        Debug.Log("Starting SacrificeAltar Challenge.");

        // Spawn central altar visual
        altarVisual = new GameObject("AltarVisual");
        altarVisual.transform.parent = itemContainer;
        altarVisual.transform.localPosition = new Vector2(0f, 0f);
        var sr = altarVisual.AddComponent<SpriteRenderer>();
        sr.color = new Color(0.5f, 0.2f, 0.2f, 1f); // Dark red altar color
        sr.sortingOrder = 2;
        altarVisual.transform.localScale = new Vector3(1.5f, 1.5f, 1f);

        // Spawn 3 sacrifice buttons (1, 2, 3 hearts) and 1 Exit button
        Vector2[] positions = {
            new Vector2(-2.5f, -1f), // 1 Heart
            new Vector2(0f, -2f),    // 2 Hearts
            new Vector2(2.5f, -1f),  // 3 Hearts
            new Vector2(0f, 1.5f)    // Exit
        };

        string[] names = { "Sacrifice_1", "Sacrifice_2", "Sacrifice_3", "Exit" };
        Color[] colors = { Color.green * 0.7f, Color.yellow * 0.7f, Color.red * 0.7f, Color.gray };

        for (int i = 0; i < 4; i++)
        {
            GameObject btn = new GameObject(names[i]);
            btn.transform.parent = itemContainer;
            btn.transform.localPosition = positions[i];

            var btnSr = btn.AddComponent<SpriteRenderer>();
            btnSr.color = colors[i];
            btnSr.sortingOrder = 1;
            btn.transform.localScale = new Vector3(1f, 1f, 1f);

            var col = btn.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.8f, 0.8f);
            col.isTrigger = true;

            var trigger = btn.AddComponent<AltarButtonTrigger>();
            trigger.typeIndex = i; // 0=1H, 1=2H, 2=3H, 3=Exit
            trigger.challengeRoom = this;

            buttons.Add(btn);
        }
    }

    public void OnButtonStepped(int typeIndex)
    {
        if (!challengeActive) return;

        Player player = GameManager.Instance.player;
        if (player == null || !player.isLive) return;

        if (typeIndex == 3) // Exit
        {
            Debug.Log("Player chose to exit sacrifice altar without sacrifice.");
            CleanupAltar();
            CompleteChallenge(false); // Fail means no extra chest, but doors open
            return;
        }

        int sacrificeAmount = typeIndex + 1;

        // Check if player has enough health to survive the sacrifice (must have more health than the sacrifice amount)
        if (player.Health > sacrificeAmount)
        {
            player.ReduceHealth(sacrificeAmount);
            Debug.LogFormat("Sacrificed {0} hearts.", sacrificeAmount);

            // Spawn reward depending on sacrifice size
            if (level != null && level.pools != null)
            {
                if (sacrificeAmount == 1)
                {
                    // Spawn common pickup
                    GameObject pickup = level.pools.GetPickupGoods();
                    if (pickup != null)
                    {
                        GenerateGameObjectWithCoordinate(pickup, new Vector2(0f, 0.5f), itemContainer);
                    }
                }
                else if (sacrificeAmount == 2)
                {
                    // Spawn rare item
                    Item rareItem = level.pools.GetItem(ItemPoolType.TreasureRoom);
                    if (rareItem != null)
                    {
                        level.manager.GenerateGameObjectInCurrentRoom(rareItem.gameObject, altarVisual.transform.position);
                    }
                }
                else if (sacrificeAmount == 3)
                {
                    // Spawn unique/boss item
                    Item bossItem = level.pools.GetItem(ItemPoolType.BossRoom);
                    if (bossItem != null)
                    {
                        level.manager.GenerateGameObjectInCurrentRoom(bossItem.gameObject, altarVisual.transform.position);
                    }
                }
            }

            CleanupAltar();
            CompleteChallenge(true); // Success!
        }
        else
        {
            Debug.LogWarning("Not enough health to sacrifice!");
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(150f, 0.3f, 0.1f);
            }
        }
    }

    private void CleanupAltar()
    {
        foreach (var btn in buttons)
        {
            if (btn != null) Destroy(btn);
        }
        buttons.Clear();
    }
}

public class AltarButtonTrigger : MonoBehaviour
{
    public int typeIndex;
    public SacrificeAltar challengeRoom;
    private bool inside = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!inside && other.CompareTag("Player"))
        {
            inside = true;
            if (challengeRoom != null)
            {
                challengeRoom.OnButtonStepped(typeIndex);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            inside = false;
        }
    }
}
