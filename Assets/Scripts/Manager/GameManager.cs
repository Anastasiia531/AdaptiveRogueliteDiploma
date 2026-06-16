using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager>
{
    public Player playerPrefab;
    public Level levelPrefab;
    public Camera myCamera;

    [HideInInspector]
    public Player player;
    [HideInInspector]
    public Level level;

    public int depth = 0;

    private void Start()
    {
        LoadNewGame();
    }

    void LoadNewGame()
    {
        player = Instantiate(playerPrefab);
        level = Instantiate(levelPrefab);
    }

    public void LevelUp()
    {
        depth += 1;
        
        // Show story narration for the level just completed (depth - 1)
        if (StoryManager.Instance != null && depth - 1 >= 1 && depth - 1 <= 5)
        {
            StoryManager.Instance.ShowStory(depth - 1, ContinueLevelUp);
        }
        else
        {
            ContinueLevelUp();
        }
    }

    private void ContinueLevelUp()
    {
        levelPrefab.roomNum += 2 * depth;
        levelPrefab.roomNum = Mathf.Clamp(levelPrefab.roomNum, 0, 12);
        level.gameObject.SetActive(false);
        myCamera.transform.position = new Vector3(0, 0, -10);
        player.transform.position = new Vector3(0, 0, 0);
        level.gameObject.SetActive(true);
        if (UIManager.Instance != null)
        {
            UIManager.Instance.initialize();
        }
    }

    public void SwitchScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
    public void SwitchScene(int sceneNum)
    {
        SceneManager.LoadScene(sceneNum);
    }
    public void OverloadScene()
    {
        Resume();
        Scene scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.name);
    }

    public void PauseGame()
    {
        Time.timeScale = 0;
        Time.fixedDeltaTime = 0;
    }
    public void Resume()
    {
        Time.timeScale = 1;
        Time.fixedDeltaTime = 0.02f;
    }
}
