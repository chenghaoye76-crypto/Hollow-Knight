using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instant;
    public bool pause;
    [HideInInspector] public float deltaTime;
    [HideInInspector] public float realTime;
    [HideInInspector] public float gameTime;
    private bool gamePlaying;

    private void Awake()
    {
        if (Instant == null)
            Instant = this;
        else
            Destroy(gameObject);
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
        {
            deltaTime = 0;
        }

        if (gamePlaying)
        {
            gameTime += Time.deltaTime;
        }
    }

    public void PlayerHurt()
    {
        Time.timeScale = 0f;
        StartCoroutine(hurt());
    }

    IEnumerator hurt()
    {
        yield return new WaitForSecondsRealtime(0.2f);
        Time.timeScale = 1f;
    }

    public void GameStart()
    {
        gameTime = GameDataManager.instant.data.gameTime;
        gamePlaying = true;
    }

    public void GameOver()
    {
        gamePlaying = false;
        gameTime = 0;
    }
}
