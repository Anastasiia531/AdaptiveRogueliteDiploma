using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThreeCardsMonte : ChallengeRoom
{
    private List<GameObject> pedestals = new List<GameObject>();

    protected override void StartChallenge()
    {
        Debug.Log("Starting ThreeCardsMonte Challenge.");

        // Define card outcomes
        int[] outcomes = { 0, 1, 2 }; // 0 = Reward, 1 = Curse, 2 = Empty
        // Shuffle outcomes
        for (int i = 0; i < outcomes.Length; i++)
        {
            int temp = outcomes[i];
            int randomIndex = Random.Range(i, outcomes.Length);
            outcomes[i] = outcomes[randomIndex];
            outcomes[randomIndex] = temp;
        }

        // Spawn 3 pedestals horizontally
        float startX = -3f;
        for (int i = 0; i < 3; i++)
        {
            GameObject ped = new GameObject("MonteCard_" + i);
            ped.transform.parent = itemContainer;
            ped.transform.localPosition = new Vector2(startX + i * 3f, 0f);

            var sr = ped.AddComponent<SpriteRenderer>();
            sr.sprite = CreateCardSprite(Color.blue);
            sr.sortingOrder = 4;

            // Trigger for player interaction
            var col = ped.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.8f, 0.8f);
            col.isTrigger = true;

            var monteCard = ped.AddComponent<MonteCard>();
            monteCard.outcome = outcomes[i];
            monteCard.challengeRoom = this;

            pedestals.Add(ped);
        }
    }

    public void OnCardPicked(int outcome)
    {
        // Cleanup pedestals
        foreach (var ped in pedestals)
        {
            if (ped != null) Destroy(ped);
        }
        pedestals.Clear();

        // Handle outcomes
        if (outcome == 0) // Reward
        {
            Debug.Log("Monte Card: REWARD!");
            if (level != null && level.pools != null)
            {
                GameObject rewardItem = level.pools.GetPickupGoods();
                if (rewardItem != null)
                {
                    GenerateGameObjectWithCoordinate(rewardItem, new Vector2(0f, 0f), itemContainer);
                }
            }
            CompleteChallenge(true);
        }
        else if (outcome == 1) // Curse
        {
            Debug.Log("Monte Card: CURSE!");
            if (level != null && level.pools != null)
            {
                GameObject monster = level.pools.GetMonster(MonsterType.Minion);
                if (monster != null)
                {
                    GenerateGameObjectWithCoordinate(monster, new Vector2(0f, 0f), monsterContainer);
                }
            }
            // Curse counts as failure/harder path
            CompleteChallenge(false);
        }
        else // Empty
        {
            Debug.Log("Monte Card: EMPTY!");
            CompleteChallenge(true); // Neutral success
        }
    }
}

public class MonteCard : MonoBehaviour
{
    public int outcome;
    public ThreeCardsMonte challengeRoom;
    private bool activated = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!activated && other.CompareTag("Player"))
        {
            activated = true;
            if (challengeRoom != null)
            {
                challengeRoom.OnCardPicked(outcome);
            }
        }
    }
}
