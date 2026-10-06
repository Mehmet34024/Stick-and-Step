/*using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Referanslar")]
    [SerializeField] private GameObject platformPrefab;
    [SerializeField] private Transform currentPlatform;
    [SerializeField] private StickController stick;
    [SerializeField] private PlayerController player;
    [SerializeField] private CameraController cameraController;
    [SerializeField] private GameObject perfectParticlePrefab;

    [Header("UI Panelleri")]
    [SerializeField] private GameObject mainMenuPanel;

    [Header("Karakter Hizalama")]
    [Tooltip("Gerekirse karakterin ayak hizasını yukarı/aşağı kaydırmak için ince ayar payı")]
    [SerializeField] private float playerYOffset = 0f;

    [Header("Dinamik Üretim Limitleri")]
    [SerializeField] private float platformY = -6.5f;
    [SerializeField] private float rightEdgePadding = 0.35f;

    [SerializeField] private float minWidth = 0.35f;
    [SerializeField] private float maxWidth = 0.85f;
    [SerializeField] private float minGap = 0.5f;
    [SerializeField] private float maxGap = 1.6f;

    public enum GameState { WaitingToStart, Playing, Reviving, GameOver }
    public GameState CurrentState { get; private set; } = GameState.WaitingToStart;

    public bool IsGameStarted => CurrentState == GameState.Playing;
    public bool IsGameOver => CurrentState == GameState.GameOver;

    public int Score { get; private set; } = 0;
    public int HighScore { get; private set; } = 0;

    private int currentCombo = 0;
    private bool hasUsedRevive = false;
    private const string HighScoreKey = "StickHero_HighScore";
    private List<Transform> activePlatforms = new List<Transform>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Mobilde 60 FPS akıcılığı zorla ve ekranın uykuya geçmesini engelle
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        HighScore = PlayerPrefs.GetInt(HighScoreKey, 0);
    }

    private void Start()
    {
        cameraController.transform.position = new Vector3(0f, 0f, -10f);
        InitializeStartingSetup();
        activePlatforms.Add(currentPlatform);

        ApplyCurrentThemeColor(currentPlatform.gameObject);
        SpawnPlatformsInView(currentPlatform, false);

        CurrentState = GameState.WaitingToStart;
    }

    public void StartGame()
    {
        if (CurrentState != GameState.WaitingToStart) return;

        CurrentState = GameState.Playing;

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.OnGameStarted();
        }
    }

    private void InitializeStartingSetup()
    {
        float screenLeftEdge = cameraController.GetScreenLeftEdge();
        float leftPadding = cameraController.LeftPadding;

        float platWidth = currentPlatform.localScale.x;
        float platCenterX = screenLeftEdge + leftPadding + (platWidth / 2f);

        currentPlatform.position = new Vector3(platCenterX, platformY, 0f);

        float stickX = currentPlatform.position.x + (platWidth / 2f);
        float stickY = currentPlatform.position.y + (currentPlatform.localScale.y / 2f);
        stick.ResetStick(new Vector3(stickX, stickY, 0f));

        PositionPlayerOnPlatform(currentPlatform);
    }

    private void PositionPlayerOnPlatform(Transform plat)
    {
        float playerX = plat.position.x;
        float platformTopY = plat.position.y + (plat.localScale.y / 2f);

        float playerY = platformTopY + playerYOffset;
        player.transform.position = new Vector3(playerX, playerY, 0f);
        player.transform.rotation = Quaternion.identity;
    }

    public void SpawnPlatformsInView(Transform basePlatform, bool useFutureEdge = true)
    {
        if (basePlatform == null) return;

        List<Transform> candidateTargets = new List<Transform>();

        float screenRightEdge = (useFutureEdge
            ? cameraController.GetFutureScreenRightEdge(basePlatform)
            : cameraController.GetScreenRightEdge()) - rightEdgePadding;

        float lastRightEdge = basePlatform.position.x + (basePlatform.localScale.x / 2f);

        int targetSpawnCount = Random.Range(1, 4);

        for (int i = 0; i < targetSpawnCount; i++)
        {
            float remainingSpace = screenRightEdge - lastRightEdge;
            if (remainingSpace < (minGap + minWidth)) break;

            float gap = Random.Range(minGap, Mathf.Min(maxGap, remainingSpace - minWidth));
            float candidateLeft = lastRightEdge + gap;

            float maxPossibleWidth = screenRightEdge - candidateLeft;
            if (maxPossibleWidth < minWidth) break;

            float width = Random.Range(minWidth, Mathf.Min(maxWidth, maxPossibleWidth));
            float candidateRight = candidateLeft + width;

            float spawnCenterX = candidateLeft + (width / 2f);
            Vector3 spawnPos = new Vector3(spawnCenterX, platformY, 0f);

            GameObject newPlatObj = Instantiate(platformPrefab, spawnPos, Quaternion.identity);
            newPlatObj.transform.localScale = new Vector3(width, basePlatform.localScale.y, 1f);

            ApplyCurrentThemeColor(newPlatObj);

            Transform platTransform = newPlatObj.transform;
            activePlatforms.Add(platTransform);
            candidateTargets.Add(platTransform);

            lastRightEdge = candidateRight;
        }

        if (candidateTargets.Count == 0)
        {
            float safeGap = minGap;
            float safeWidth = minWidth;
            float spawnCenterX = lastRightEdge + safeGap + (safeWidth / 2f);
            Vector3 spawnPos = new Vector3(spawnCenterX, platformY, 0f);

            GameObject fallbackPlat = Instantiate(platformPrefab, spawnPos, Quaternion.identity);
            fallbackPlat.transform.localScale = new Vector3(safeWidth, basePlatform.localScale.y, 1f);

            ApplyCurrentThemeColor(fallbackPlat);

            activePlatforms.Add(fallbackPlat.transform);
            candidateTargets.Add(fallbackPlat.transform);
        }

        stick.SetCandidatePlatforms(candidateTargets);
    }

    public void ApplyCurrentThemeColor(GameObject platObj)
    {
        if (platObj == null || ShopManager.Instance == null) return;

        ShopItemData currentBg = ShopManager.Instance.GetEquippedItem(ShopCategory.Background);
        if (currentBg != null)
        {
            SpriteRenderer sr = platObj.GetComponent<SpriteRenderer>();
            if (sr == null) sr = platObj.GetComponentInChildren<SpriteRenderer>();

            if (sr != null)
            {
                sr.color = currentBg.platformColor;
            }
        }
    }

    public void UpdateAllActivePlatformColors(Color newColor)
    {
        foreach (Transform plat in activePlatforms)
        {
            if (plat != null)
            {
                SpriteRenderer sr = plat.GetComponent<SpriteRenderer>();
                if (sr == null) sr = plat.GetComponentInChildren<SpriteRenderer>();

                if (sr != null)
                {
                    sr.color = newColor;
                }
            }
        }

        if (currentPlatform != null)
        {
            SpriteRenderer sr = currentPlatform.GetComponent<SpriteRenderer>();
            if (sr == null) sr = currentPlatform.GetComponentInChildren<SpriteRenderer>();

            if (sr != null)
            {
                sr.color = newColor;
            }
        }
    }

    public void OnPlatformTouched(Transform reachedPlatform, bool isPerfect, int platformsCrossed = 1)
    {
        if (IsGameOver || reachedPlatform == null) return;

        int basePoints = Mathf.Max(1, platformsCrossed);
        int earnedPoints = basePoints;

        if (isPerfect)
        {
            currentCombo++;

            if (HapticManager.Instance != null) HapticManager.Instance.PlayPerfect();
            if (AudioManager.Instance != null) AudioManager.Instance.PlayPerfect();

            Transform spot = reachedPlatform.Find("PerfectSpot");
            if (spot == null && reachedPlatform.childCount > 0)
            {
                spot = reachedPlatform.GetChild(0);
            }

            Vector3 spawnPos = spot != null ? spot.position : reachedPlatform.position;
            if (perfectParticlePrefab != null)
            {
                GameObject p = Instantiate(perfectParticlePrefab, spawnPos, Quaternion.identity);
                Destroy(p, 1f);
            }

            if (spot != null)
            {
                Destroy(spot.gameObject);
            }

            earnedPoints = basePoints + currentCombo;

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowPerfectFeedback(earnedPoints);
            }
        }
        else
        {
            currentCombo = 0;
            earnedPoints = basePoints;
        }

        Score += earnedPoints;

        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.AddCoins(earnedPoints);
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateScore(Score);
        }

        // BURADAKİ ERKEN GEÇİŞ COROUTINE'İ KALDIRILDI!
        // Geçiş artık karakter platforma tam ulaştığında (OnCharacterArrived) tetiklenecek.
    }

    public void OnCharacterArrived(Transform reachedPlatform)
    {
        if (IsGameOver) return;

        Transform target = reachedPlatform != null ? reachedPlatform : currentPlatform;
        if (target == null) return;

        // Karakter platformun üstüne tam vardığında geçişi güvenle başlatıyoruz
        StartCoroutine(TransitionRoutine(target));
    }

    private IEnumerator TransitionRoutine(Transform reachedPlatform)
    {
        currentPlatform = reachedPlatform;

        for (int i = activePlatforms.Count - 1; i >= 0; i--)
        {
            Transform p = activePlatforms[i];
            if (p != null && p != reachedPlatform)
            {
                Destroy(p.gameObject);
            }
        }

        activePlatforms.Clear();
        activePlatforms.Add(reachedPlatform);

        SpawnPlatformsInView(currentPlatform, true);

        if (cameraController != null)
        {
            yield return StartCoroutine(cameraController.AlignPlatformToLeftMarginRoutine(currentPlatform, null));
        }

        // Kamera hizalaması bittiğinde yeni çubuğu hazırla
        float stickX = currentPlatform.position.x + (currentPlatform.localScale.x / 2f);
        float stickY = currentPlatform.position.y + (currentPlatform.localScale.y / 2f);
        stick.ResetStick(new Vector3(stickX, stickY, 0f));
    }

    public void GameOver()
    {
        if (IsGameOver || CurrentState == GameState.Reviving) return;

        // Tur başına 1 defaya mahsus Revive hakkı
        if (!hasUsedRevive && UIManager.Instance != null)
        {
            CurrentState = GameState.Reviving;
            UIManager.Instance.ShowRevivePanel(
                onAdWatched: RevivePlayer,
                onDeclined: FinalizeGameOver
            );
            return;
        }

        FinalizeGameOver();
    }

    private void RevivePlayer()
    {
        hasUsedRevive = true;
        CurrentState = GameState.Playing;

        PositionPlayerOnPlatform(currentPlatform);

        float stickX = currentPlatform.position.x + (currentPlatform.localScale.x / 2f);
        float stickY = currentPlatform.position.y + (currentPlatform.localScale.y / 2f);
        stick.ResetStick(new Vector3(stickX, stickY, 0f));
    }

    private void FinalizeGameOver()
    {
        CurrentState = GameState.GameOver;

        if (Score > HighScore)
        {
            HighScore = Score;
            PlayerPrefs.SetInt(HighScoreKey, HighScore);
            PlayerPrefs.Save();
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowGameOver(Score, HighScore);
        }

        // 3 oyunda bir geçiş reklamı
        AdManager.Instance?.TriggerGameOverInterstitial();
    }
}*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Referanslar")]
    [SerializeField] private GameObject platformPrefab;
    [SerializeField] private Transform currentPlatform;
    [SerializeField] private StickController stick;
    [SerializeField] private PlayerController player;
    [SerializeField] private CameraController cameraController;
    [SerializeField] private GameObject perfectParticlePrefab;

    [Header("UI Panelleri")]
    [SerializeField] private GameObject mainMenuPanel;

    [Header("Karakter Hizalama")]
    [SerializeField] private float playerYOffset = 0f;

    [Header("Dinamik Üretim Limitleri")]
    [SerializeField] private float platformY = -6.5f;
    [SerializeField] private float rightEdgePadding = 0.35f;

    [SerializeField] private float minWidth = 0.35f;
    [SerializeField] private float maxWidth = 0.85f;
    [SerializeField] private float minGap = 0.5f;
    [SerializeField] private float maxGap = 1.6f;

    public enum GameState { WaitingToStart, Playing, Reviving, GameOver }
    public GameState CurrentState { get; private set; } = GameState.WaitingToStart;

    public bool IsGameStarted => CurrentState == GameState.Playing;
    public bool IsGameOver => CurrentState == GameState.GameOver;

    public int Score { get; private set; } = 0;
    public int HighScore { get; private set; } = 0;

    private int currentCombo = 0;
    private bool hasUsedRevive = false;
    private const string HighScoreKey = "StickHero_HighScore";
    private List<Transform> activePlatforms = new List<Transform>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Mobilde yağ gibi 60 FPS akması için:
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        HighScore = PlayerPrefs.GetInt(HighScoreKey, 0);
    }

    private void Start()
    {
        cameraController.transform.position = new Vector3(0f, 0f, -10f);
        InitializeStartingSetup();
        activePlatforms.Add(currentPlatform);

        ApplyCurrentThemeColor(currentPlatform.gameObject);
        SpawnPlatformsInView(currentPlatform, false);

        CurrentState = GameState.WaitingToStart;
    }

    public void StartGame()
    {
        if (CurrentState != GameState.WaitingToStart) return;

        CurrentState = GameState.Playing;

        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (UIManager.Instance != null) UIManager.Instance.OnGameStarted();

        // İlk dokunmaya hazırla
        stick.EnableInput();
    }

    private void InitializeStartingSetup()
    {
        float screenLeftEdge = cameraController.GetScreenLeftEdge();
        float leftPadding = cameraController.LeftPadding;

        float platWidth = currentPlatform.localScale.x;
        float platCenterX = screenLeftEdge + leftPadding + (platWidth / 2f);

        currentPlatform.position = new Vector3(platCenterX, platformY, 0f);

        float stickX = currentPlatform.position.x + (platWidth / 2f);
        float stickY = currentPlatform.position.y + (currentPlatform.localScale.y / 2f);
        stick.ResetStick(new Vector3(stickX, stickY, 0f));

        PositionPlayerOnPlatform(currentPlatform);
    }

    public void PositionPlayerOnPlatform(Transform plat)
    {
        float playerX = plat.position.x;
        float platformTopY = plat.position.y + (plat.localScale.y / 2f);

        float playerY = platformTopY + playerYOffset;
        player.transform.position = new Vector3(playerX, playerY, 0f);
        player.transform.rotation = Quaternion.identity;
    }

    public void SpawnPlatformsInView(Transform basePlatform, bool useFutureEdge = true)
    {
        if (basePlatform == null) return;

        List<Transform> candidateTargets = new List<Transform>();

        float screenRightEdge = (useFutureEdge
            ? cameraController.GetFutureScreenRightEdge(basePlatform)
            : cameraController.GetScreenRightEdge()) - rightEdgePadding;

        float lastRightEdge = basePlatform.position.x + (basePlatform.localScale.x / 2f);

        int targetSpawnCount = Random.Range(1, 4);

        for (int i = 0; i < targetSpawnCount; i++)
        {
            float remainingSpace = screenRightEdge - lastRightEdge;
            if (remainingSpace < (minGap + minWidth)) break;

            float gap = Random.Range(minGap, Mathf.Min(maxGap, remainingSpace - minWidth));
            float candidateLeft = lastRightEdge + gap;

            float maxPossibleWidth = screenRightEdge - candidateLeft;
            if (maxPossibleWidth < minWidth) break;

            float width = Random.Range(minWidth, Mathf.Min(maxWidth, maxPossibleWidth));
            float candidateRight = candidateLeft + width;

            float spawnCenterX = candidateLeft + (width / 2f);
            Vector3 spawnPos = new Vector3(spawnCenterX, platformY, 0f);

            GameObject newPlatObj = Instantiate(platformPrefab, spawnPos, Quaternion.identity);
            newPlatObj.transform.localScale = new Vector3(width, basePlatform.localScale.y, 1f);

            ApplyCurrentThemeColor(newPlatObj);

            Transform platTransform = newPlatObj.transform;
            activePlatforms.Add(platTransform);
            candidateTargets.Add(platTransform);

            lastRightEdge = candidateRight;
        }

        if (candidateTargets.Count == 0)
        {
            float safeGap = minGap;
            float safeWidth = minWidth;
            float spawnCenterX = lastRightEdge + safeGap + (safeWidth / 2f);
            Vector3 spawnPos = new Vector3(spawnCenterX, platformY, 0f);

            GameObject fallbackPlat = Instantiate(platformPrefab, spawnPos, Quaternion.identity);
            fallbackPlat.transform.localScale = new Vector3(safeWidth, basePlatform.localScale.y, 1f);

            ApplyCurrentThemeColor(fallbackPlat);

            activePlatforms.Add(fallbackPlat.transform);
            candidateTargets.Add(fallbackPlat.transform);
        }

        stick.SetCandidatePlatforms(candidateTargets);
    }

    public void ApplyCurrentThemeColor(GameObject platObj)
    {
        if (platObj == null || ShopManager.Instance == null) return;

        ShopItemData currentBg = ShopManager.Instance.GetEquippedItem(ShopCategory.Background);
        if (currentBg != null)
        {
            SpriteRenderer sr = platObj.GetComponent<SpriteRenderer>();
            if (sr == null) sr = platObj.GetComponentInChildren<SpriteRenderer>();

            if (sr != null) sr.color = currentBg.platformColor;
        }
    }

    public void UpdateAllActivePlatformColors(Color newColor)
    {
        foreach (Transform plat in activePlatforms)
        {
            if (plat != null)
            {
                SpriteRenderer sr = plat.GetComponent<SpriteRenderer>();
                if (sr == null) sr = plat.GetComponentInChildren<SpriteRenderer>();
                if (sr != null) sr.color = newColor;
            }
        }

        if (currentPlatform != null)
        {
            SpriteRenderer sr = currentPlatform.GetComponent<SpriteRenderer>();
            if (sr == null) sr = currentPlatform.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) sr.color = newColor;
        }
    }

    public void OnPlatformTouched(Transform reachedPlatform, bool isPerfect, int platformsCrossed = 1)
    {
        if (IsGameOver || reachedPlatform == null) return;

        int basePoints = Mathf.Max(1, platformsCrossed);
        int earnedPoints = basePoints;

        if (isPerfect)
        {
            currentCombo++;

            try
            {
                if (HapticManager.Instance != null) HapticManager.Instance.PlayPerfect();
            }
            catch { }

            if (AudioManager.Instance != null) AudioManager.Instance.PlayPerfect();

            Transform spot = reachedPlatform.Find("PerfectSpot");
            if (spot == null && reachedPlatform.childCount > 0)
            {
                spot = reachedPlatform.GetChild(0);
            }

            if (spot != null)
            {
                if (perfectParticlePrefab != null)
                {
                    GameObject p = Instantiate(perfectParticlePrefab, spot.position, Quaternion.identity);
                    Destroy(p, 1f);
                }
                SpriteRenderer spotSr = spot.GetComponent<SpriteRenderer>();
                if (spotSr != null) spotSr.enabled = false;
            }

            earnedPoints = basePoints + currentCombo;

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowPerfectFeedback(earnedPoints);
            }
        }
        else
        {
            currentCombo = 0;
            earnedPoints = basePoints;
        }

        Score += earnedPoints;

        if (CoinManager.Instance != null) CoinManager.Instance.AddCoins(earnedPoints);
        if (UIManager.Instance != null) UIManager.Instance.UpdateScore(Score);

        StartCoroutine(TransitionRoutine(reachedPlatform));
    }

    private IEnumerator TransitionRoutine(Transform reachedPlatform)
    {
        currentPlatform = reachedPlatform;

        // Eski platformları temizle
        for (int i = activePlatforms.Count - 1; i >= 0; i--)
        {
            Transform p = activePlatforms[i];
            if (p != null && p != reachedPlatform)
            {
                Destroy(p.gameObject);
            }
        }

        activePlatforms.Clear();
        activePlatforms.Add(reachedPlatform);

        SpawnPlatformsInView(currentPlatform, true);

        // Yeni çubuğu hemen yeni platformun ucuna kur
        float stickX = currentPlatform.position.x + (currentPlatform.localScale.x / 2f);
        float stickY = currentPlatform.position.y + (currentPlatform.localScale.y / 2f);
        stick.ResetStick(new Vector3(stickX, stickY, 0f));

        // Kamera yumuşakça kaydıktan sonra yeni dokunmaya izin ver
        if (cameraController != null)
        {
            yield return StartCoroutine(cameraController.AlignPlatformToLeftMarginRoutine(currentPlatform, null));
        }

        stick.EnableInput();
    }

    public void GameOver()
    {
        if (IsGameOver || CurrentState == GameState.Reviving) return;

        if (!hasUsedRevive && UIManager.Instance != null)
        {
            CurrentState = GameState.Reviving;
            UIManager.Instance.ShowRevivePanel(
                onAdWatched: RevivePlayer,
                onDeclined: FinalizeGameOver
            );
            return;
        }

        FinalizeGameOver();
    }

    private void RevivePlayer()
    {
        hasUsedRevive = true;
        CurrentState = GameState.Playing;

        PositionPlayerOnPlatform(currentPlatform);

        float stickX = currentPlatform.position.x + (currentPlatform.localScale.x / 2f);
        float stickY = currentPlatform.position.y + (currentPlatform.localScale.y / 2f);
        stick.ResetStick(new Vector3(stickX, stickY, 0f));
        stick.EnableInput();
    }

    private void FinalizeGameOver()
    {
        CurrentState = GameState.GameOver;

        if (Score > HighScore)
        {
            HighScore = Score;
            PlayerPrefs.SetInt(HighScoreKey, HighScore);
            PlayerPrefs.Save();
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowGameOver(Score, HighScore);
        }

        AdManager.Instance?.TriggerGameOverInterstitial();
    }
}