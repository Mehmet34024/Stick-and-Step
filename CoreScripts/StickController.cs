/*using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StickController : MonoBehaviour
{
    [Header("Bağlantılar")]
    [SerializeField] private PlayerController player;
    [SerializeField] private CameraController cameraController;
    [SerializeField] private SpriteRenderer stickRenderer;
    [SerializeField] private ParticleSystem sparkParticle;

    [Header("Uzama Ayarları")]
    [SerializeField] private float growSpeed = 4.5f;
    private float currentMaxHeight = 10f;

    [Header("Devrilme Ayarları")]
    [SerializeField] private float fallDuration = 0.35f;

    private List<Transform> candidatePlatforms = new List<Transform>();

    private enum StickState { Idle, Growing, Falling, Walking, Done }
    private StickState currentState = StickState.Idle;

    private bool canAcceptInput = false;
    private bool isRainbowStick = false;
    private Transform visualTransform;

    private void Awake()
    {
        Transform visual = transform.Find("StickVisual");
        if (visual != null)
        {
            visualTransform = visual;
            if (stickRenderer == null) stickRenderer = visual.GetComponent<SpriteRenderer>();
        }
        else
        {
            visualTransform = transform;
            if (stickRenderer == null) stickRenderer = GetComponent<SpriteRenderer>();
        }
    }

    private void OnEnable()
    {
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.OnShopUpdated += ApplyEquippedStickColor;
        }
    }

    private void OnDisable()
    {
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.OnShopUpdated -= ApplyEquippedStickColor;
        }
    }

    private void Start()
    {
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.OnShopUpdated += ApplyEquippedStickColor;
        }
        ApplyEquippedStickColor();
        StopSparkEffect();
    }

    private void Update()
    {
        if (isRainbowStick && stickRenderer != null)
        {
            float hue = Mathf.Repeat(Time.time * 0.6f, 1f);
            Color rainbowColor = Color.HSVToRGB(hue, 0.9f, 1f);
            stickRenderer.color = rainbowColor;

            if (sparkParticle != null && sparkParticle.isPlaying)
            {
                var main = sparkParticle.main;
                main.startColor = rainbowColor;
            }
        }

        if (GameManager.Instance == null || !GameManager.Instance.IsGameStarted)
        {
            canAcceptInput = false;
            return;
        }

        if (!canAcceptInput)
        {
            if (Input.GetMouseButtonUp(0))
            {
                canAcceptInput = true;
            }
            return;
        }

        if (currentState != StickState.Idle && currentState != StickState.Growing) return;
        if (player != null && player.IsMoving) return;

        HandleInput();
        GrowStick();
    }

    public void ApplyEquippedStickColor()
    {
        if (ShopManager.Instance == null) return;

        ShopItemData stickData = ShopManager.Instance.GetEquippedItem(ShopCategory.StickColor);
        if (stickData != null)
        {
            isRainbowStick = stickData.isRainbow;

            if (stickRenderer != null)
            {
                if (isRainbowStick)
                {
                    stickRenderer.color = Color.HSVToRGB(0f, 0.9f, 1f);
                }
                else
                {
                    stickRenderer.color = stickData.stickColor;
                }
            }
        }
    }

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0) && currentState == StickState.Idle)
        {
            currentState = StickState.Growing;
            StartSparkEffect();
        }
        else if (Input.GetMouseButtonUp(0) && currentState == StickState.Growing)
        {
            StopSparkEffect();
            StartFalling();
        }
    }

    private void GrowStick()
    {
        if (currentState != StickState.Growing) return;

        Vector3 currentScale = transform.localScale;
        currentScale.y += growSpeed * Time.deltaTime;

        if (currentScale.y >= currentMaxHeight)
        {
            currentScale.y = currentMaxHeight;
            transform.localScale = currentScale;
            StopSparkEffect();
            StartFalling();
            return;
        }

        transform.localScale = currentScale;
        UpdateSparkPosition();
    }

    private void StartSparkEffect()
    {
        if (sparkParticle == null) return;

        if (stickRenderer != null)
        {
            var main = sparkParticle.main;
            main.startColor = stickRenderer.color;
        }

        UpdateSparkPosition();
        sparkParticle.Play();
    }

    private void UpdateSparkPosition()
    {
        if (sparkParticle == null) return;

        float currentLength = (visualTransform != null && visualTransform != transform)
            ? visualTransform.lossyScale.y
            : transform.lossyScale.y;

        Vector3 tipPos = transform.position + (transform.up * currentLength);
        sparkParticle.transform.position = tipPos;
    }

    private void StopSparkEffect()
    {
        if (sparkParticle != null)
        {
            sparkParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void StartFalling()
    {
        currentState = StickState.Falling;
        StartCoroutine(FallRoutine());
    }

    private IEnumerator FallRoutine()
    {
        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.Euler(0f, 0f, -90f);

        float elapsed = 0f;
        while (elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fallDuration;
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t * t);
            yield return null;
        }

        transform.rotation = targetRotation;
        currentState = StickState.Walking;

        if (AudioManager.Instance != null) AudioManager.Instance.PlayStickHit();
        if (cameraController != null) cameraController.Shake(0.08f, 0.05f);

        EvaluateFall();
    }

    private void EvaluateFall()
    {
        float stickLength = (visualTransform != null && visualTransform != transform)
            ? visualTransform.lossyScale.y
            : transform.lossyScale.y;

        float stickTipX = transform.position.x + stickLength;

        Transform hitPlatform = null;
        int platformsCrossed = 1;

        for (int i = 0; i < candidatePlatforms.Count; i++)
        {
            Transform plat = candidatePlatforms[i];
            if (plat == null) continue;

            float platWidth = plat.localScale.x;
            float leftEdge = plat.position.x - (platWidth / 2f);
            float rightEdge = plat.position.x + (platWidth / 2f);

            if (stickTipX >= leftEdge && stickTipX <= rightEdge)
            {
                hitPlatform = plat;
                platformsCrossed = i + 1;
                break;
            }
        }

        if (hitPlatform != null)
        {
            float platWidth = hitPlatform.localScale.x;
            float leftEdge = hitPlatform.position.x - (platWidth / 2f);
            float rightEdge = hitPlatform.position.x + (platWidth / 2f);
            float safeDestinationX = rightEdge - 0.15f;

            bool isPerfect = false;
            Transform spot = hitPlatform.Find("PerfectSpot");
            if (spot == null && hitPlatform.childCount > 0)
            {
                spot = hitPlatform.GetChild(0);
            }

            if (spot != null)
            {
                float spotWidth = spot.lossyScale.x;
                float spotLeft = spot.position.x - (spotWidth / 2f);
                float spotRight = spot.position.x + (spotWidth / 2f);

                if (stickTipX >= spotLeft && stickTipX <= spotRight)
                {
                    isPerfect = true;
                }
            }

            Transform targetPlat = hitPlatform;
            int crossedCount = platformsCrossed;

            StartCoroutine(player.WalkRoutine(
                safeDestinationX,
                true,
                leftEdge,
                () =>
                {
                    if (GameManager.Instance != null && targetPlat != null)
                    {
                        GameManager.Instance.OnPlatformTouched(targetPlat, isPerfect, crossedCount);
                    }
                },
                () =>
                {
                    currentState = StickState.Done;
                    if (GameManager.Instance != null && targetPlat != null)
                    {
                        GameManager.Instance.OnCharacterArrived(targetPlat);
                    }
                }
            ));
        }
        else
        {
            // BAŞARISIZLIK DURUMU: Çubuğun ucunun 0.2 birim ötesine kadar yürü ve düş
            float dropTargetX = stickTipX + 0.2f;
            currentState = StickState.Done; // Çubuk durumunu hemen Done yapıyoruz ki inputlar kilitlenmesin

            StartCoroutine(player.WalkRoutine(
                dropTargetX,
                false,
                float.MaxValue,
                null,
                null
            ));
        }
    }

    public void SetCandidatePlatforms(List<Transform> platforms)
    {
        candidatePlatforms = platforms;

        if (candidatePlatforms != null && candidatePlatforms.Count > 0)
        {
            Transform farthestPlat = candidatePlatforms[candidatePlatforms.Count - 1];
            float farthestRightEdge = farthestPlat.position.x + (farthestPlat.localScale.x / 2f);
            float distanceToFarthest = farthestRightEdge - transform.position.x;
            currentMaxHeight = distanceToFarthest + 0.8f;
        }
    }

    public void ResetStick(Vector3 newPosition)
    {
        transform.position = newPosition;
        transform.rotation = Quaternion.identity;
        transform.localScale = new Vector3(1f, 0f, 1f);
        currentState = StickState.Idle;
        StopSparkEffect();
        ApplyEquippedStickColor();
    }
}*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StickController : MonoBehaviour
{
    [Header("Bağlantılar")]
    [SerializeField] private PlayerController player;
    [SerializeField] private CameraController cameraController;
    [SerializeField] private SpriteRenderer stickRenderer;
    [SerializeField] private ParticleSystem sparkParticle;

    [Header("Uzama Ayarları")]
    [SerializeField] private float growSpeed = 4.5f;
    private float currentMaxHeight = 10f;

    [Header("Devrilme Ayarları")]
    [SerializeField] private float fallDuration = 0.3f;

    private List<Transform> candidatePlatforms = new List<Transform>();

    private enum StickState { Idle, Growing, Falling, Walking, Done }
    private StickState currentState = StickState.Idle;

    private bool canAcceptInput = false;
    private bool isRainbowStick = false;

    private void Awake()
    {
        if (stickRenderer == null)
        {
            stickRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }

    private void OnEnable()
    {
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.OnShopUpdated += ApplyEquippedStickColor;
        }
    }

    private void OnDisable()
    {
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.OnShopUpdated -= ApplyEquippedStickColor;
        }
    }

    private void Start()
    {
        ApplyEquippedStickColor();
        StopSparkEffect();
    }

    public void EnableInput()
    {
        currentState = StickState.Idle;
        canAcceptInput = true;
    }

    private void Update()
    {
        if (isRainbowStick && stickRenderer != null)
        {
            float hue = Mathf.Repeat(Time.time * 0.6f, 1f);
            Color rainbowColor = Color.HSVToRGB(hue, 0.9f, 1f);
            stickRenderer.color = rainbowColor;

            if (sparkParticle != null && sparkParticle.isPlaying)
            {
                var main = sparkParticle.main;
                main.startColor = rainbowColor;
            }
        }

        if (GameManager.Instance == null || !GameManager.Instance.IsGameStarted)
        {
            canAcceptInput = false;
            return;
        }

        if (!canAcceptInput) return;
        if (currentState != StickState.Idle && currentState != StickState.Growing) return;
        if (player != null && player.IsMoving) return;

        HandleInput();
        GrowStick();
    }

    public void ApplyEquippedStickColor()
    {
        if (ShopManager.Instance == null) return;

        ShopItemData stickData = ShopManager.Instance.GetEquippedItem(ShopCategory.StickColor);
        if (stickData != null)
        {
            isRainbowStick = stickData.isRainbow;

            if (stickRenderer != null)
            {
                stickRenderer.color = isRainbowStick ? Color.HSVToRGB(0f, 0.9f, 1f) : stickData.stickColor;
            }
        }
    }

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0) && currentState == StickState.Idle)
        {
            currentState = StickState.Growing;
            StartSparkEffect();
        }
        else if (Input.GetMouseButtonUp(0) && currentState == StickState.Growing)
        {
            canAcceptInput = false;
            StopSparkEffect();
            StartFalling();
        }
    }

    private void GrowStick()
    {
        if (currentState != StickState.Growing) return;

        Vector3 currentScale = transform.localScale;
        currentScale.y += growSpeed * Time.deltaTime;

        if (currentScale.y >= currentMaxHeight)
        {
            currentScale.y = currentMaxHeight;
            transform.localScale = currentScale;
            canAcceptInput = false;
            StopSparkEffect();
            StartFalling();
            return;
        }

        transform.localScale = currentScale;
        UpdateSparkPosition();
    }

    private void StartSparkEffect()
    {
        if (sparkParticle == null) return;

        if (stickRenderer != null)
        {
            var main = sparkParticle.main;
            main.startColor = stickRenderer.color;
        }

        UpdateSparkPosition();
        sparkParticle.Play();
    }

    private void UpdateSparkPosition()
    {
        if (sparkParticle == null) return;
        Vector3 tipPos = transform.position + (transform.up * transform.localScale.y);
        sparkParticle.transform.position = tipPos;
    }

    private void StopSparkEffect()
    {
        if (sparkParticle != null)
        {
            sparkParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void StartFalling()
    {
        currentState = StickState.Falling;
        StartCoroutine(FallRoutine());
    }

    private IEnumerator FallRoutine()
    {
        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.Euler(0f, 0f, -90f);

        float elapsed = 0f;
        while (elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fallDuration;
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t * t);
            yield return null;
        }

        transform.rotation = targetRotation;
        currentState = StickState.Walking;

        if (AudioManager.Instance != null) AudioManager.Instance.PlayStickHit();
        if (cameraController != null) cameraController.Shake(0.08f, 0.05f);

        EvaluateFall();
    }

    private void EvaluateFall()
    {
        float stickLength = Mathf.Abs(transform.localScale.y);
        float stickTipX = transform.position.x + stickLength;

        Transform hitPlatform = null;
        int platformsCrossed = 1;

        for (int i = 0; i < candidatePlatforms.Count; i++)
        {
            Transform plat = candidatePlatforms[i];
            if (plat == null) continue;

            float platWidth = plat.localScale.x;
            float leftEdge = plat.position.x - (platWidth / 2f);
            float rightEdge = plat.position.x + (platWidth / 2f);

            if (stickTipX >= leftEdge && stickTipX <= rightEdge)
            {
                hitPlatform = plat;
                platformsCrossed = i + 1;
                break;
            }
        }

        if (hitPlatform != null)
        {
            float platWidth = hitPlatform.localScale.x;
            float leftEdge = hitPlatform.position.x - (platWidth / 2f);
            float destinationX = hitPlatform.position.x;

            bool isPerfect = false;
            Transform spot = hitPlatform.Find("PerfectSpot");
            if (spot == null && hitPlatform.childCount > 0)
            {
                spot = hitPlatform.GetChild(0);
            }

            if (spot != null)
            {
                float spotWidth = spot.lossyScale.x;
                float spotLeft = spot.position.x - (spotWidth / 2f);
                float spotRight = spot.position.x + (spotWidth / 2f);

                if (stickTipX >= spotLeft && stickTipX <= spotRight)
                {
                    isPerfect = true;
                }
            }

            Transform targetPlat = hitPlatform;
            int crossedCount = platformsCrossed;

            StartCoroutine(player.WalkRoutine(
                destinationX,
                true,
                leftEdge,
                () =>
                {
                    if (GameManager.Instance != null && targetPlat != null)
                    {
                        GameManager.Instance.OnPlatformTouched(targetPlat, isPerfect, crossedCount);
                    }
                },
                () =>
                {
                    currentState = StickState.Done;
                }
            ));
        }
        else
        {
            // BAŞARISIZLIK: Çubuğun ucundan hemen düş
            float fallPointX = stickTipX + 0.15f;
            currentState = StickState.Done;

            StartCoroutine(player.WalkRoutine(
                fallPointX,
                false,
                float.MaxValue,
                null,
                null
            ));
        }
    }

    public void SetCandidatePlatforms(List<Transform> platforms)
    {
        candidatePlatforms = platforms;

        if (candidatePlatforms != null && candidatePlatforms.Count > 0)
        {
            Transform farthestPlat = candidatePlatforms[candidatePlatforms.Count - 1];
            float farthestRightEdge = farthestPlat.position.x + (farthestPlat.localScale.x / 2f);
            float distanceToFarthest = farthestRightEdge - transform.position.x;
            currentMaxHeight = distanceToFarthest + 0.8f;
        }
    }

    public void ResetStick(Vector3 newPosition)
    {
        transform.position = newPosition;
        transform.rotation = Quaternion.identity;
        transform.localScale = new Vector3(1f, 0f, 1f);
        currentState = StickState.Idle;
        StopSparkEffect();
        ApplyEquippedStickColor();
    }
}