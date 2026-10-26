using System.Collections;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance;
    public bool pause;
    [HideInInspector] public float deltaTime;
    [HideInInspector] public float realTime;
    [HideInInspector] public float gameTime;
    private bool gamePlaying;
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
        pause = false;
        DontDestroyOnLoad(gameObject);
    }
    private void Update()
    {
        if (!pause)
        {
            float lastTime = realTime;
            realTime = Time.realtimeSinceStartup;
            deltaTime = realTime - lastTime;
        }
        else
            deltaTime = 0f;

        if (gamePlaying)
            gameTime += Time.deltaTime;
    }

    public void PlayerHurt()
    {
        if (pause) return;
        Time.timeScale = 0f;
        StartCoroutine(hurt());
    }

    IEnumerator hurt()
    {
        yield return new WaitForSecondsRealtime(0.2f);
        if (!pause) Time.timeScale = 1f;
    }

    public void GameStart()
    {
        //gameTime = GameDataManager.instant.data.gameTime;
        gamePlaying = true;
    }

    public void GameOver()
    {
        gamePlaying = false;
        gameTime = 0f;
    }


}
