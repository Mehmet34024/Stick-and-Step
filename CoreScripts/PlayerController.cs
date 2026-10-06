/*using System;
using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Görsel Bileşenler")]
    [SerializeField] private Transform visualTransform;
    [SerializeField] private SpriteRenderer characterRenderer;

    [Header("Hareket Ayarları")]
    [SerializeField] private float walkSpeed = 3.5f;
    [SerializeField] private float fallSpeed = 9f;

    [Header("Prosedürel Yürüme Hissi")]
    [SerializeField] private float tiltAngle = 8f;
    [SerializeField] private float bounceHeight = 0.05f;
    [SerializeField] private float walkCycleSpeed = 16f;

    [Header("Toz / Duman Partikülü")]
    [SerializeField] private ParticleSystem walkDustParticle;

    public bool IsMoving { get; private set; }

    private Vector3 initialVisualLocalPos;
    private Quaternion initialVisualLocalRot;

    private void Awake()
    {
        if (visualTransform == null)
        {
            SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null) visualTransform = sr.transform;
            else visualTransform = transform;
        }

        if (characterRenderer == null && visualTransform != null)
        {
            characterRenderer = visualTransform.GetComponent<SpriteRenderer>();
        }

        initialVisualLocalPos = visualTransform.localPosition;
        initialVisualLocalRot = visualTransform.localRotation;
    }

    private void OnEnable()
    {
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.OnShopUpdated += UpdateCharacterVisual;
        }

        UpdateCharacterVisual();
    }

    private void OnDisable()
    {
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.OnShopUpdated -= UpdateCharacterVisual;
        }
    }

    private void Start()
    {
        UpdateCharacterVisual();
    }

    public void UpdateCharacterVisual()
    {
        if (ShopManager.Instance == null || characterRenderer == null) return;

        ShopItemData equippedCharacter = ShopManager.Instance.GetEquippedItem(ShopCategory.Character);
        if (equippedCharacter != null && equippedCharacter.characterSprite != null)
        {
            characterRenderer.sprite = equippedCharacter.characterSprite;
        }
    }

    public IEnumerator WalkRoutine(
        float targetX,
        bool isSuccess,
        float touchThresholdX,
        Action onTouchPlatform,
        Action onComplete)
    {
        IsMoving = true;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StartWalkingSound();
        }

        if (walkDustParticle != null && !walkDustParticle.isPlaying)
        {
            walkDustParticle.Play();
        }

        bool hasTriggeredTouch = false;
        float walkTimer = 0f;
        float safetyTimeout = 5.0f; // Asla donup kalamaz, 5 saniye aşılırsa zorla kırar

        // Hedefe yaklaşana kadar yürü (tek yönlü < yerine mutlak mesafe kontrolü)
        while (Mathf.Abs(transform.position.x - targetX) > 0.05f && safetyTimeout > 0f)
        {
            safetyTimeout -= Time.deltaTime;
            walkTimer += Time.deltaTime * walkCycleSpeed;

            // 1. Yatay İlerleme
            float newX = Mathf.MoveTowards(transform.position.x, targetX, walkSpeed * Time.deltaTime);
            transform.position = new Vector3(newX, transform.position.y, transform.position.z);

            // 2. Prosedürel Salınım
            if (visualTransform != null)
            {
                float currentTilt = Mathf.Sin(walkTimer) * tiltAngle;
                float currentBounce = Mathf.Abs(Mathf.Cos(walkTimer)) * bounceHeight;

                visualTransform.localRotation = Quaternion.Euler(0f, 0f, -currentTilt);
                visualTransform.localPosition = initialVisualLocalPos + new Vector3(0f, currentBounce, 0f);
            }

            // Platform temas eşiği (başarılı adımlarda)
            if (isSuccess && !hasTriggeredTouch && transform.position.x >= touchThresholdX)
            {
                hasTriggeredTouch = true;
                onTouchPlatform?.Invoke();
            }

            yield return null;
        }

        // Hedefe kesin sabitle
        transform.position = new Vector3(targetX, transform.position.y, transform.position.z);
        ResetVisualTransform();
        StopWalkingEffects();

        if (!isSuccess)
        {
            // Çubuk ucundan aşağı düşüş animasyonu
            yield return StartCoroutine(FallDownRoutine());
            IsMoving = false;
            // Düşüş bittiğinde onComplete çağrılmaz, doğrudan GameOver'a gider
        }
        else
        {
            // Eğer temas tetiklenmediyse son anda tetikle
            if (!hasTriggeredTouch)
            {
                onTouchPlatform?.Invoke();
            }

            IsMoving = false;
            onComplete?.Invoke();
        }
    }

    private void ResetVisualTransform()
    {
        if (visualTransform != null)
        {
            visualTransform.localPosition = initialVisualLocalPos;
            visualTransform.localRotation = initialVisualLocalRot;
        }
    }

    private void StopWalkingEffects()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopWalkingSound();
        }

        if (walkDustParticle != null && walkDustParticle.isPlaying)
        {
            walkDustParticle.Stop();
        }
    }

    private IEnumerator FallDownRoutine()
    {
        StopWalkingEffects();
        ResetVisualTransform();

        if (HapticManager.Instance != null)
        {
            HapticManager.Instance.PlayFail();
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCharacterFall();
        }

        float bottomLimit = transform.position.y - 8f;
        while (transform.position.y > bottomLimit)
        {
            transform.position += Vector3.down * fallSpeed * Time.deltaTime;
            yield return null;
        }

        CameraController mainCam = Camera.main != null ? Camera.main.GetComponent<CameraController>() : null;
        if (mainCam != null)
        {
            mainCam.Shake(0.2f, 0.12f);
        }

        // Düşüş tamamlandı, Revive / Game Over panelini aç
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }
    }
}*/

using System;
using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Görsel Bileşenler")]
    [SerializeField] private Transform visualTransform;
    [SerializeField] private SpriteRenderer characterRenderer;

    [Header("Hareket Ayarları")]
    [SerializeField] private float walkSpeed = 4.0f;
    [SerializeField] private float fallSpeed = 14f;

    [Header("Prosedürel Yürüme Hissi")]
    [SerializeField] private float tiltAngle = 8f;
    [SerializeField] private float bounceHeight = 0.05f;
    [SerializeField] private float walkCycleSpeed = 16f;

    [Header("Toz / Duman Partikülü")]
    [SerializeField] private ParticleSystem walkDustParticle;

    public bool IsMoving { get; private set; }

    private Vector3 initialVisualLocalPos;
    private Quaternion initialVisualLocalRot;

    private void Awake()
    {
        if (visualTransform == null)
        {
            SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null) visualTransform = sr.transform;
            else visualTransform = transform;
        }

        if (characterRenderer == null && visualTransform != null)
        {
            characterRenderer = visualTransform.GetComponent<SpriteRenderer>();
        }

        initialVisualLocalPos = visualTransform.localPosition;
        initialVisualLocalRot = visualTransform.localRotation;
    }

    private void OnEnable()
    {
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.OnShopUpdated += UpdateCharacterVisual;
        }
        UpdateCharacterVisual();
    }

    private void OnDisable()
    {
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.OnShopUpdated -= UpdateCharacterVisual;
        }
    }

    private void Start()
    {
        UpdateCharacterVisual();
    }

    public void UpdateCharacterVisual()
    {
        if (ShopManager.Instance == null || characterRenderer == null) return;

        ShopItemData equippedCharacter = ShopManager.Instance.GetEquippedItem(ShopCategory.Character);
        if (equippedCharacter != null && equippedCharacter.characterSprite != null)
        {
            characterRenderer.sprite = equippedCharacter.characterSprite;
        }
    }

    public IEnumerator WalkRoutine(
        float targetX,
        bool isSuccess,
        float touchThresholdX,
        Action onTouchPlatform,
        Action onComplete)
    {
        IsMoving = true;

        if (AudioManager.Instance != null) AudioManager.Instance.StartWalkingSound();
        if (walkDustParticle != null && !walkDustParticle.isPlaying) walkDustParticle.Play();

        bool hasTriggeredTouch = false;
        float walkTimer = 0f;
        float timeout = 2.0f; // Mobilde sonsuz döngüde kalmayı önleyen güvenlik kilidi

        while (transform.position.x < targetX && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            walkTimer += Time.deltaTime * walkCycleSpeed;

            float step = walkSpeed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(
                transform.position,
                new Vector3(targetX, transform.position.y, transform.position.z),
                step
            );

            if (visualTransform != null)
            {
                float currentTilt = Mathf.Sin(walkTimer) * tiltAngle;
                float currentBounce = Mathf.Abs(Mathf.Cos(walkTimer)) * bounceHeight;

                visualTransform.localRotation = Quaternion.Euler(0f, 0f, -currentTilt);
                visualTransform.localPosition = initialVisualLocalPos + new Vector3(0f, currentBounce, 0f);
            }

            // Platforma ayak basma anı
            if (isSuccess && !hasTriggeredTouch && transform.position.x >= touchThresholdX)
            {
                hasTriggeredTouch = true;
                onTouchPlatform?.Invoke();
            }

            if (Mathf.Abs(targetX - transform.position.x) <= 0.05f)
            {
                break;
            }

            yield return null;
        }

        transform.position = new Vector3(targetX, transform.position.y, transform.position.z);
        ResetVisualTransform();
        StopWalkingEffects();

        if (!isSuccess)
        {
            // Çubuğun ucundan kesin düşüş
            yield return StartCoroutine(FallDownRoutine());
            IsMoving = false;
        }
        else
        {
            if (!hasTriggeredTouch)
            {
                onTouchPlatform?.Invoke();
            }

            IsMoving = false;
            onComplete?.Invoke();
        }
    }

    private void ResetVisualTransform()
    {
        if (visualTransform != null)
        {
            visualTransform.localPosition = initialVisualLocalPos;
            visualTransform.localRotation = initialVisualLocalRot;
        }
    }

    private void StopWalkingEffects()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.StopWalkingSound();
        if (walkDustParticle != null && walkDustParticle.isPlaying) walkDustParticle.Stop();
    }

    private IEnumerator FallDownRoutine()
    {
        StopWalkingEffects();
        ResetVisualTransform();

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        try
        {
            if (HapticManager.Instance != null) HapticManager.Instance.PlayFail();
        }
        catch { }

        if (AudioManager.Instance != null) AudioManager.Instance.PlayCharacterFall();

        float bottomLimit = transform.position.y - 12f;
        while (transform.position.y > bottomLimit)
        {
            transform.position += Vector3.down * fallSpeed * Time.deltaTime;
            yield return null;
        }

        CameraController mainCam = Camera.main != null ? Camera.main.GetComponent<CameraController>() : null;
        if (mainCam != null) mainCam.Shake(0.2f, 0.12f);

        if (col != null) col.enabled = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }
    }
}