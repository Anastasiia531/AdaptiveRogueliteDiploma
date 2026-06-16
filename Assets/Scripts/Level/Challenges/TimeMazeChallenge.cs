using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeMazeChallenge : ChallengeRoom
{
    private List<GameObject> mazeWalls = new List<GameObject>();
    private GameObject finishZone;
    private float timeLimit;
    private float timeLeft;
    private bool destinationReached = false;

    protected override void StartChallenge()
    {
        // DDA variables
        timeLimit = (skillIndexAtStart < 0.4f) ? 20.0f : 10.0f;
        int wallCount = (skillIndexAtStart < 0.4f) ? 3 : 8;
        timeLeft = timeLimit;
        destinationReached = false;

        Debug.LogFormat("Starting TimeMazeChallenge: timeLimit={0:F1}s, walls={1}", timeLimit, wallCount);

        // Move player to the left side of the room
        Player player = GameManager.Instance.player;
        if (player != null)
        {
            player.transform.position = transform.position + new Vector3(-8f, 0f, 0f);
        }

        // Spawn walls (simple color blocks with box colliders)
        // Set positions based on wallCount to create either simple obstacles or maze
        Vector2[] wallPositions;
        if (skillIndexAtStart < 0.4f)
        {
            wallPositions = new Vector2[] {
                new Vector2(-3f, 2f),
                new Vector2(0f, -2f),
                new Vector2(3f, 2f)
            };
        }
        else
        {
            // More walls, creating tighter channels and dead ends
            wallPositions = new Vector2[] {
                new Vector2(-4f, 2f),
                new Vector2(-4f, -2f),
                new Vector2(-2f, 0f),
                new Vector2(0f, 3f),
                new Vector2(0f, -3f),
                new Vector2(2f, 1f),
                new Vector2(4f, -2f),
                new Vector2(4f, 2f)
            };
        }

        for (int i = 0; i < Mathf.Min(wallCount, wallPositions.Length); i++)
        {
            GameObject wall = new GameObject("MazeWall_" + i);
            wall.transform.parent = defaultContainer;
            wall.transform.localPosition = wallPositions[i];

            var sr = wall.AddComponent<SpriteRenderer>();
            sr.color = new Color(0.3f, 0.3f, 0.4f, 1f); // Dark stone color
            sr.sortingOrder = 3;

            wall.transform.localScale = new Vector3(1.2f, 2.5f, 1f);

            var col = wall.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1f, 1f);

            mazeWalls.Add(wall);
        }

        // Spawn finish zone on the right side of the room
        finishZone = new GameObject("MazeFinish");
        finishZone.transform.parent = itemContainer;
        finishZone.transform.localPosition = new Vector2(8f, 0f);

        var finishSr = finishZone.AddComponent<SpriteRenderer>();
        finishSr.color = Color.green * 0.8f;
        finishSr.sortingOrder = 1;
        finishZone.transform.localScale = new Vector3(1.5f, 1.5f, 1f);

        var finishCol = finishZone.AddComponent<BoxCollider2D>();
        finishCol.size = new Vector2(1f, 1f);
        finishCol.isTrigger = true;

        var trigger = finishZone.AddComponent<MazeFinishTrigger>();
        trigger.challengeRoom = this;

        StartCoroutine(MazeTimerRoutine());
    }

    private IEnumerator MazeTimerRoutine()
    {
        while (timeLeft > 0 && !destinationReached && GameManager.Instance.player != null && GameManager.Instance.player.isLive)
        {
            timeLeft -= Time.deltaTime;
            yield return null;
        }

        CleanupMaze();

        if (destinationReached)
        {
            CompleteChallenge(true);
        }
        else
        {
            Debug.Log("Time limit reached for maze! Challenge failed.");
            // Deal damage
            if (GameManager.Instance.player != null)
            {
                GameManager.Instance.player.BeAttacked(1, Vector2.zero);
            }
            CompleteChallenge(false);
        }
    }

    public void OnDestinationReached()
    {
        if (!challengeActive) return;
        destinationReached = true;
        Debug.Log("Player reached the maze destination!");
    }

    private void CleanupMaze()
    {
        foreach (var wall in mazeWalls)
        {
            if (wall != null) Destroy(wall);
        }
        mazeWalls.Clear();

        if (finishZone != null) Destroy(finishZone);
    }
}

public class MazeFinishTrigger : MonoBehaviour
{
    public TimeMazeChallenge challengeRoom;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (challengeRoom != null)
            {
                challengeRoom.OnDestinationReached();
            }
        }
    }
}
