﻿using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class OtherGameModesManager : MonoBehaviour
{

    public static OtherGameModesManager Instance;

    public AudioListener audioListener;
    public GameObject StartPage;
    public GameObject PauseMenu;
    public GameObject CountdownPage;
    public GameObject GamePage;
    public GameObject ScoreReview;
    public GameObject RateMePage;
    public Button pauseButton;
    public Text countdownText;
    public Button PlusOneButton;
    public Button DeadeyeButton;
    public Button ClairvoyanceButton;
    public RectTransform pauseButtonRect;
    public Text scoreText;
    public Text highScoreText;
    public Text gameOverScore; //score when you loose for that run
    public GameObject newHighScoreImage;
    public Button skipScoreReviewButton;
    public Button replayButton;
    public Button GoBack2ModeSelectButton;
    public Animator scoreReviewAnimC;
    public Text PlusOneHighScore;
    public Text DeadeyeHighScore;
    public Text ClairvoyanceHighScore;
    public Text UltraHighScore; // just the sum of all the other high scores
    public GameObject TapBlocker; //just blocks taps for when a player leaves a game mode and goes back to the OGM Menu. stops player from tapping another game mode button until the menu is fully loaded

    GameManager game;
    ObstacleSpawner obSpawner;
    PaddleController Paddle;
    BallController ballC;
    SceneChanger sceneChanger;
    TargetController targetC;
    AdManager ads;
    AudioManager audioManager;
    AchievementsAndLeaderboards rankings;
    Ratings rate;

    Coroutine disableReplayButtonC;
    Coroutine pauseCoroutine;
    Coroutine fadeInVolume;
    Text scoreReviewGems;

    bool gemsOnScreen = false;
    bool gameModeRunning = false;
    bool pauseAllCoroutines = false;
    bool paused = false;
    bool firstStart = true;
    bool replaying = false;
    int PlusOneHS;
    int DeadeyeHS;
    int ClairvoyanceHS;
    int UltraScore;
    int score = 0;
    float t = 0;
    float tempGems = 0;
    float gems = 0;
    int newGems;
    int activeHighScore;
    bool noSound;
    float timeSinceStartup;
    int incrementor4Asks;
    int rateAsks;
    bool updateHS = false;
    float volB4Pause;

    public delegate void OtherGameModesManagerDelegate();
    public static event OtherGameModesManagerDelegate GameModeStarted;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        PlusOneHS = ZPlayerPrefs.GetInt("PlusOneHighScore");
        PlusOneHighScore.text = PlusOneHS.ToString();
        DeadeyeHS = ZPlayerPrefs.GetInt("DeadeyeHighScore");
        DeadeyeHighScore.text = DeadeyeHS.ToString();
        ClairvoyanceHS = ZPlayerPrefs.GetInt("ClairvoyanceHighScore");
        ClairvoyanceHighScore.text = ClairvoyanceHS.ToString();

        UltraScore = PlusOneHS + DeadeyeHS + ClairvoyanceHS;
        UltraHighScore.text = UltraScore.ToString();

        gems = ZPlayerPrefs.GetInt("gems");

        scoreReviewGems = ScoreReview.transform.Find("gems").GetComponent<Text>();
        scoreReviewGems.text = gems.ToString();
    }

    private void Start()
    {
        obSpawner = ObstacleSpawner.Instance;
        game = GameManager.Instance;
        ballC = BallController.Instance;
        Paddle = PaddleController.Instance;
        sceneChanger = SceneChanger.Instance;
        targetC = TargetController.Instance;
        ads = AdManager.Instance;
        audioManager = AudioManager.Instance;
        rankings = AchievementsAndLeaderboards.Instance;
        rate = Ratings.Instance;

        audioManager.GetAudioFiltersFromCamera();

        noSound = PlayerPrefsX.GetBool("noSound");
        AudioListener.volume = 0;
        audioListener.enabled = true;
        if (!noSound)
        {
            audioManager.SwitchUpRadioFirstSong();

            fadeInVolume = StartCoroutine(FadeInVolume());
            audioManager.StartOGMMenuMusic();
        }

        Paddle.SetPauseButtonRect(pauseButtonRect);
        DeactivatePaddle();

        SetPageState(pageState.StartPage);
        SetGameMode(gameMode.None);
    }

    IEnumerator FadeInVolume() //fade in the games master volume
    {
        float targetTime = 1;
        float elaspedTime = 0;

        while (elaspedTime < targetTime)
        {
            elaspedTime += Time.deltaTime;

            AudioListener.volume = Mathf.Lerp(0, 1, elaspedTime / targetTime);
            yield return null;
        }
    }

    IEnumerator FadeInVolumeFromPause() //fade in the games master volume
    {
        AudioListener.pause = false;

        float targetTime = 0.2f;
        float elaspedTime = 0;

        while (AudioListener.volume != 1)
        {
            elaspedTime += Time.unscaledDeltaTime;

            AudioListener.volume = Mathf.Lerp(0, 1, elaspedTime / targetTime);
            yield return null;
        }
    }

    IEnumerator FadeOutVolume() //fade out games master volume
    {
        if (fadeInVolume != null) StopCoroutine(fadeInVolume);

        float targetTime = 0.28f;
        float elaspedTime = 0;

        while (elaspedTime < targetTime)
        {
            elaspedTime += Time.unscaledDeltaTime;

            AudioListener.volume = Mathf.Lerp(1, 0, elaspedTime / targetTime);
            yield return null;
        }

        if (paused)
        {
            AudioListener.pause = true;
        }
    }

    public void Scored()
    {
        score++;
        scoreText.text = score.ToString();
    }

    public void DoubleScored()
    {
        score += 2;
        scoreText.text = score.ToString();
    }

    public void Missed()
    {
        DeactivatePaddle();
        GoToScoreReview();
    }

    private void Update()
    {
        if (gemsOnScreen)
        {
            t += 0.1f * Time.deltaTime;
            tempGems = Mathf.Lerp(tempGems, newGems, t);
            if (tempGems == newGems)
            {
                gemsOnScreen = false;
            }
            scoreReviewGems.text = Mathf.RoundToInt(tempGems).ToString();
        }
    }

    public void GoToScoreReview()
    {
        rate.Ask4Rate();

        t = 0.0f;
        tempGems = gems;
        newGems = (int)gems + score;
        scoreReviewGems.text = gems.ToString();
        gems += score;
        if (score > ActiveHighScore())
        {
            ActiveHighScore(score);
            newHighScoreImage.SetActive(true);
            updateHS = true;
        }
        else
        {
            newHighScoreImage.SetActive(false);
        }
        gameOverScore.text = score.ToString();
        highScoreText.text = ActiveHighScore().ToString();

        skipScoreReviewButton.interactable = true;
        disableReplayButtonC = StartCoroutine(DisableReplayButon());
        SetPageState(pageState.ScoreReview);
    }

    IEnumerator DisableReplayButon()
    {
        replayButton.interactable = false;
        GoBack2ModeSelectButton.interactable = false;

        yield return new WaitForSeconds(0.8f);//set this coroutine to be the length of the swipeIn anim
        while (pauseAllCoroutines)
        {
            yield return null;
        }

        gemsOnScreen = true;
        skipScoreReviewButton.interactable = false;
        replayButton.interactable = true;
        GoBack2ModeSelectButton.interactable = true;
    }

    public void skipScoreReviewAnim()
    {
        skipScoreReviewButton.interactable = false;
        StopCoroutine(disableReplayButtonC);
        replayButton.interactable = true;
        GoBack2ModeSelectButton.interactable = true;
        scoreReviewAnimC.SetTrigger("skipAnim");
        gemsOnScreen = true;
    }

    public int ActiveHighScore(int newHS = 0)
    {
        switch (activeHighScore)
        {
            case 1:
                if (newHS > 0)
                {
                    PlusOneHS = newHS;
                    PlusOneHighScore.text = PlusOneHS.ToString();
                    UpdateUltraScore();
                }

                return PlusOneHS;

            case 2:
                if (newHS > 0)
                {
                    DeadeyeHS = newHS;
                    DeadeyeHighScore.text = DeadeyeHS.ToString();
                    UpdateUltraScore();
                }

                return DeadeyeHS;

            case 3:
                if (newHS > 0)
                {
                    ClairvoyanceHS = newHS;
                    ClairvoyanceHighScore.text = ClairvoyanceHS.ToString();
                    UpdateUltraScore();
                }

                return ClairvoyanceHS;
        }

        return 0;
    }

    public enum pageState { Game, StartPage, Paused, CountdownPage, ScoreReview };
    pageState currentPageState;

    public enum gameMode { PlusOne, Deadeye, Clairvoyance, None }
    gameMode currentGameMode;

    public void SetPageState(pageState page)
    {
        switch (page)
        {
            case pageState.Game:
                currentPageState = pageState.Game;
                GamePage.SetActive(true);
                StartPage.SetActive(false);
                PauseMenu.SetActive(false);
                CountdownPage.SetActive(false);
                ScoreReview.SetActive(false);
                break;

            case pageState.StartPage:
                currentPageState = pageState.StartPage;
                GamePage.SetActive(false);
                StartPage.SetActive(true);
                PauseMenu.SetActive(false);
                CountdownPage.SetActive(false);
                ScoreReview.SetActive(false);

                gemsOnScreen = false;

                if (!noSound)
                {
                    audioManager.PlayLvlSound("elevator");
                }
                break;

            case pageState.Paused:
                currentPageState = pageState.Paused;
                GamePage.SetActive(true);
                StartPage.SetActive(false);
                PauseMenu.SetActive(true);
                CountdownPage.SetActive(false);
                ScoreReview.SetActive(false);

                pauseButton.gameObject.SetActive(false);

                break;

            case pageState.CountdownPage:
                currentPageState = pageState.CountdownPage;
                GamePage.SetActive(true);
                StartPage.SetActive(false);
                PauseMenu.SetActive(false);
                CountdownPage.SetActive(true);
                ScoreReview.SetActive(false);

                countdownText.enabled = false;
                pauseButton.gameObject.SetActive(true);

                break;


            case pageState.ScoreReview:
                currentPageState = pageState.ScoreReview;
                GamePage.SetActive(false);
                StartPage.SetActive(false);
                PauseMenu.SetActive(false);
                CountdownPage.SetActive(false);
                ScoreReview.SetActive(true);
                break;
        }
    }

    public void SetGameMode(gameMode gameMode)
    {
        switch (gameMode)
        {
            case gameMode.PlusOne:
                currentGameMode = gameMode.PlusOne;

                obSpawner.SetGameMode(gameMode.PlusOne);
                ballC.SetGameMode(gameMode.PlusOne);

                activeHighScore = 1;
                break;

            case gameMode.Deadeye:
                currentGameMode = gameMode.Deadeye;

                obSpawner.SetGameMode(gameMode.Deadeye);
                ballC.SetGameMode(gameMode.Deadeye);

                activeHighScore = 2;
                break;

            case gameMode.Clairvoyance:
                currentGameMode = gameMode.Clairvoyance;

                obSpawner.SetGameMode(gameMode.Clairvoyance);
                ballC.SetGameMode(gameMode.Clairvoyance);

                activeHighScore = 3;
                break;

            case gameMode.None:
                currentGameMode = gameMode.None;

                obSpawner.SetGameMode(gameMode.None);
                ballC.SetGameMode(gameMode.None);

                targetC.SetTargetColor(Color.yellow);
                break;
        }
    }

    public void Go2PlusOne()
    {
        audioManager.StopLvlSounds();
        audioManager.PlayUISound("plus1");
        ballC.Fade2GameMode(pageState.Game, gameMode.PlusOne);
        SetGameModeSelectButtons(false);

        firstStart = true;
    }

    public void Go2Deadeye()
    {
        audioManager.StopLvlSounds();
        audioManager.PlayUISound("deadeye");
        ballC.Fade2GameMode(pageState.Game, gameMode.Deadeye);
        SetGameModeSelectButtons(false);

        firstStart = true;
    }

    public void Go2Clairvoyance()
    {
        audioManager.StopLvlSounds();
        audioManager.PlayUISound("clairvoyance");
        ballC.Fade2GameMode(pageState.Game, gameMode.Clairvoyance);
        SetGameModeSelectButtons(false);

        firstStart = true;
    }

    public void GoBack2ModeSelect()
    {
        GoBack2ModeSelectButton.interactable = false;

        score = 0;
        scoreText.text = score.ToString();

        ads.ShowInterstitialOrNonSkipAd();

        ballC.Fade2GameMode(pageState.StartPage, gameMode.None);
    }

    public void Replay()
    {
        replayButton.interactable = false;

        ads.ShowInterstitialOrNonSkipAd();

        scoreReviewAnimC.SetTrigger("swipeOut");
        ballC.Fade2GameMode(pageState.Game, currentGameMode, true);

        score = 0;
        scoreText.text = score.ToString();

        replaying = true;
    }

    public void StartGameMode()
    {
        if (currentGameMode != gameMode.None)
        {
            Time.timeScale = 0;
            SetPageState(pageState.CountdownPage);
            pauseCoroutine = StartCoroutine(Countdown());
        }
        else
        {
            SetGameModeSelectButtons(true);
        }

        replayButton.interactable = true;
        GoBack2ModeSelectButton.interactable = true;
    }

    public void PauseGame()
    {
        if (!noSound)
            StartCoroutine(FadeOutVolume());

        SetPageState(pageState.Paused);
        Time.timeScale = 0;
        if (pauseCoroutine != null)
        {
            StopCoroutine(pauseCoroutine);
        }
        DeactivatePaddle();
        paused = true;
    }

    public void ResumeGame()
    {
        SetPageState(pageState.CountdownPage);
        pauseCoroutine = StartCoroutine(Countdown());
    }

    public void SetGameModeSelectButtons(bool enable)
    {
        if (enable)
        {
            PlusOneButton.interactable = true;
            DeadeyeButton.interactable = true;
            ClairvoyanceButton.interactable = true;
        }
        else
        {
            PlusOneButton.interactable = false;
            DeadeyeButton.interactable = false;
            ClairvoyanceButton.interactable = false;
        }
    }

    IEnumerator Countdown()
    {
        yield return new WaitForSecondsRealtime(0.55f);
        countdownText.enabled = true;
        for (int i = 3; i > 0; i--)
        {
            countdownText.text = i.ToString();

            audioManager.PlayMiscSound("countdownPing");
            yield return new WaitForSecondsRealtime(1);
            while (pauseAllCoroutines)
            {
                yield return null;
            }
        }
        CountdownPage.SetActive(false);
        paused = false;
        ActivatePaddle();
        Time.timeScale = 1;
        gameModeRunning = true;
        if (firstStart)
        {
            ActivatePaddle();

            if (!noSound)
                audioManager.StartPlayingOGMMusicRadio();
        }
        if (firstStart || replaying)
        {
            if (firstStart) firstStart = false;
            if (replaying) replaying = false;

            GameModeStarted();
        }

        if (!noSound)
            StartCoroutine(FadeInVolumeFromPause());
    }

    public void GoBackHome()
    {
        audioManager.PlayUISound("computerSelect2");
        if (!noSound)
        {
            StartCoroutine(FadeOutVolume());
        }
        sceneChanger.Fade2Scene(0);
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            volB4Pause = AudioListener.volume;
            AudioListener.volume = 0;

            ZPlayerPrefs.SetInt("PlusOneHighScore", PlusOneHS);
            ZPlayerPrefs.SetInt("DeadeyeHighScore", DeadeyeHS);
            ZPlayerPrefs.SetInt("ClairvoyanceHighScore", ClairvoyanceHS);
            ZPlayerPrefs.SetInt("gems", (int)gems);

            if (updateHS)
            {
                rankings.AddScore2LeaderBoard(AchievementsAndLeaderboards.leaderBoards.ultraHighScores, PlusOneHS + DeadeyeHS + ClairvoyanceHS);
            }

            pauseAllCoroutines = true;
        }
        else
        {
            if (!noSound)
            {
                AudioListener.volume = volB4Pause;
            }
            pauseAllCoroutines = false;
        }
    }

    //private void OnApplicationQuit()
    //{
    //    ZPlayerPrefs.SetInt("PlusOneHighScore", PlusOneHS);
    //    ZPlayerPrefs.SetInt("DeadeyeHighScore", DeadeyeHS);
    //    ZPlayerPrefs.SetInt("ClairvoyanceHighScore", ClairvoyanceHS);
    //    ZPlayerPrefs.SetInt("gems", (int)gems);

    //    if (updateHS)
    //    {
    //        rankings.AddScore2LeaderBoard(AchievementsAndLeaderboards.leaderBoards.ultraHighScores, PlusOneHS + DeadeyeHS + ClairvoyanceHS);
    //    }
    //}

    public void DeactivatePaddle()
    {
        Paddle.DeactivatePaddle(); // deactivatePaddle also sets othergamerunning bool to false;
        Paddle.gameObject.SetActive(false);
    }

    public void ActivatePaddle()
    {
        Paddle.gameObject.SetActive(true);
        Paddle.IsOtherGameModeRunning = true;
    }

    private void OnDisable()
    {
        ZPlayerPrefs.SetInt("PlusOneHighScore", PlusOneHS);
        ZPlayerPrefs.SetInt("DeadeyeHighScore", DeadeyeHS);
        ZPlayerPrefs.SetInt("ClairvoyanceHighScore", ClairvoyanceHS);
        ZPlayerPrefs.SetInt("gems", (int)gems);
    }

    void UpdateUltraScore()
    {
        UltraScore = PlusOneHS + DeadeyeHS + ClairvoyanceHS;
        UltraHighScore.text = UltraScore.ToString();
    }
}
