using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RouletteWheel : ChallengeRoom
{
    private List<GameObject> buttons = new List<GameObject>();
    private GameObject machineVisual;
    private int coinsCost = 5;
    private bool machineExploded = false;

    protected override void StartChallenge()
    {
        Debug.Log("Starting RouletteWheel Challenge.");

        // Spawn central machine visual
        machineVisual = new GameObject("RouletteMachine");
        machineVisual.transform.parent = itemContainer;
        machineVisual.transform.localPosition = new Vector2(0f, 0f);
        var sr = machineVisual.AddComponent<SpriteRenderer>();
        sr.color = Color.magenta * 0.8f;
        sr.sortingOrder = 2;
        machineVisual.transform.localScale = new Vector3(1.4f, 1.4f, 1f);

        // Spin button and Exit button
        Vector2[] positions = {
            new Vector2(-2f, -1.5f), // Spin
            new Vector2(2f, -1.5f)   // Exit
        };

        string[] names = { "Spin_Button", "Exit_Button" };
        Color[] colors = { Color.cyan, Color.gray };

        for (int i = 0; i < 2; i++)
        {
            GameObject btn = new GameObject(names[i]);
            btn.transform.parent = itemContainer;
            btn.transform.localPosition = positions[i];

            var btnSr = btn.AddComponent<SpriteRenderer>();
            btnSr.color = colors[i];
            btnSr.sortingOrder = 1;
            btn.transform.localScale = new Vector3(1.2f, 0.8f, 1f);

            var col = btn.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.2f, 0.8f);
            col.isTrigger = true;

            var trigger = btn.AddComponent<RouletteButtonTrigger>();
            trigger.isSpin = (i == 0);
            trigger.challengeRoom = this;

            buttons.Add(btn);
        }
    }

    public void OnButtonStepped(bool isSpin)
    {
        if (!challengeActive || machineExploded) return;

        Player player = GameManager.Instance.player;
        if (player == null || !player.isLive) return;

        if (!isSpin) // Exit
        {
            Debug.Log("Player exited the roulette wheel.");
            CleanupRoulette();
            CompleteChallenge(true); // Neutral exit
            return;
        }

        // Spin requested
        if (player.coins >= coinsCost)
        {
            player.coins -= coinsCost;
            if (UIManager.Instance != null) UIManager.Instance.UpdateStatus();

            StartCoroutine(SpinRoutine(player));
        }
        else
        {
            Debug.LogWarning("Not enough coins to spin!");
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(150f, 0.3f, 0.1f);
            }
        }
    }

    private IEnumerator SpinRoutine(Player player)
    {
        // Play spinning sound
        if (AudioManager.Instance != null)
        {
            for (int i = 0; i < 5; i++)
            {
                AudioManager.Instance.PlaySound(400f + i * 100f, 0.05f, 0.1f);
                yield return new WaitForSeconds(0.1f);
            }
        }

        float roll = Random.value;
        if (roll < 0.4f) // Spawn pickups
        {
            Debug.Log("Roulette: Spawn Pickups!");
            if (level != null && level.pools != null)
            {
                GameObject pickup = level.pools.GetPickupGoods();
                if (pickup != null)
                {
                    GenerateGameObjectWithCoordinate(pickup, new Vector2(0f, -0.8f), itemContainer);
                }
            }
        }
        else if (roll < 0.7f) // Spawn item
        {
            Debug.Log("Roulette: Spawn Item!");
            if (level != null && level.pools != null)
            {
                Item randomItem = level.pools.GetItem(ItemPoolType.TreasureRoom);
                if (randomItem != null)
                {
                    level.manager.GenerateGameObjectInCurrentRoom(randomItem.gameObject, new Vector2(0f, -0.8f));
                }
            }
        }
        else if (roll < 0.9f) // Explode
        {
            Debug.Log("Roulette: EXPLOSION!");
            machineExploded = true;
            
            // Spawn explosion particles and deal damage
            player.BeAttacked(1, Vector2.zero);
            
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(100f, 0.5f, 0.4f, true); // Loud explosion
            }

            // Dim machine visual to black/burned
            var sr = machineVisual.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = Color.black;

            CleanupRoulette();
            CompleteChallenge(false); // Fail due to explosion
        }
        else // Spawn a bomb
        {
            Debug.Log("Roulette: Spawn bomb!");
            // Spawning a pickup item as fallback
            if (level != null && level.pools != null)
            {
                GameObject pickup = level.pools.GetPickupGoods();
                if (pickup != null)
                {
                    GenerateGameObjectWithCoordinate(pickup, new Vector2(0f, -0.8f), itemContainer);
                }
            }
        }
    }

    private void CleanupRoulette()
    {
        foreach (var btn in buttons)
        {
            if (btn != null) Destroy(btn);
        }
        buttons.Clear();
    }
}

public class RouletteButtonTrigger : MonoBehaviour
{
    public bool isSpin;
    public RouletteWheel challengeRoom;
    private bool inside = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!inside && other.CompareTag("Player"))
        {
            inside = true;
            if (challengeRoom != null)
            {
                challengeRoom.OnButtonStepped(isSpin);
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
