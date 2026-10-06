/* using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [Header("Tüm Maðaza Kataloðu")]
    [SerializeField] private List<ShopItemData> allShopItems = new List<ShopItemData>();

    [Header("Sahne Görsel Referanslarý (Ýsteðe Baðlý)")]
    [Tooltip("Eðer arkaplan 2D Sprite ise sürükleyin")]
    [SerializeField] private SpriteRenderer backgroundRenderer;
    [Tooltip("Eðer arkaplan Canvas içinde UI Image ise sürükleyin")]
    [SerializeField] private Image backgroundImage;

    public event Action OnShopUpdated;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Oyun açýldýðýnda son seçili karakter, çubuk ve arka planý otomatik sahneye uygula
        ApplyAllEquippedSkins();
    }

    // --- SATIN ALMA & ÝZLEME ÝÞLEMLERÝ ---

    public bool TryBuyWithCoins(ShopItemData item)
    {
        if (item == null || item.IsUnlocked()) return false;

        if (CoinManager.Instance != null && CoinManager.Instance.SpendCoins(item.coinPrice))
        {
            item.SetUnlocked();
            EquipItem(item);
            OnShopUpdated?.Invoke();
            return true;
        }

        return false;
    }

    public void WatchAdForUnlock(ShopItemData item)
    {
        if (item == null || item.IsUnlocked()) return;

        item.AddWatchedAd();
        if (item.IsUnlocked())
        {
            EquipItem(item);
        }

        OnShopUpdated?.Invoke();
    }

    // --- KUÞANMA / SEÇÝM (EQUIP) ---

    public void EquipItem(ShopItemData item)
    {
        if (item == null || !item.IsUnlocked()) return;

        string key = GetPrefKeyForCategory(item.category);
        PlayerPrefs.SetString(key, item.id);
        PlayerPrefs.Save();

        ApplyEquippedSkin(item);
        OnShopUpdated?.Invoke();
    }

    public bool IsEquipped(ShopItemData item)
    {
        if (item == null) return false;

        ShopItemData currentEquipped = GetEquippedItem(item.category);
        return currentEquipped != null && currentEquipped.id == item.id;
    }

    /// <summary>
    /// Aktif/kuþanýlmýþ olan eþyayý getirir
    /// </summary>
    public ShopItemData GetEquippedItem(ShopCategory category)
    {
        string key = GetPrefKeyForCategory(category);
        string defaultId = GetDefaultIdForCategory(category);
        string equippedId = PlayerPrefs.GetString(key, defaultId);

        // 1. Kayýtlý ID ile eþleþen öðeyi bul
        foreach (var item in allShopItems)
        {
            if (item != null && item.category == category && item.id == equippedId)
            {
                return item;
            }
        }

        // 2. Bulunamazsa o kategorideki ilk ücretsiz (Free) öðeyi getir
        foreach (var item in allShopItems)
        {
            if (item != null && item.category == category && item.unlockType == UnlockType.Free)
            {
                return item;
            }
        }

        return null;
    }

    // --- GÖRSEL UYGULAMA (APPLY) ---

    public void ApplyAllEquippedSkins()
    {
        ShopItemData charItem = GetEquippedItem(ShopCategory.Character);
        if (charItem != null) ApplyEquippedSkin(charItem);

        ShopItemData stickItem = GetEquippedItem(ShopCategory.StickColor);
        if (stickItem != null) ApplyEquippedSkin(stickItem);

        ShopItemData bgItem = GetEquippedItem(ShopCategory.Background);
        if (bgItem != null) ApplyEquippedSkin(bgItem);
    }

    public void ApplyEquippedSkin(ShopItemData item)
    {
        if (item == null) return;

        // 1. Karakter Sprite Güncellemesi
        if (item.category == ShopCategory.Character && item.characterSprite != null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                SpriteRenderer sr = playerObj.GetComponentInChildren<SpriteRenderer>();
                if (sr != null) sr.sprite = item.characterSprite;
            }
        }

        // 2. Çubuk Rengi Güncellemesi
        if (item.category == ShopCategory.StickColor)
        {
            GameObject stickObj = GameObject.Find("Stick");
            if (stickObj != null)
            {
                SpriteRenderer stickSr = stickObj.GetComponentInChildren<SpriteRenderer>();
                if (stickSr != null) stickSr.color = item.stickColor;
            }
        }

        // 3. Arka Plan Güncellemesi
        if (item.category == ShopCategory.Background && item.backgroundSprite != null)
        {
            // A) Inspector'dan doðrudan baðlanan SpriteRenderer varsa
            if (backgroundRenderer != null)
            {
                backgroundRenderer.sprite = item.backgroundSprite;
                return;
            }

            // B) Inspector'dan doðrudan baðlanan Canvas Image varsa
            if (backgroundImage != null)
            {
                backgroundImage.sprite = item.backgroundSprite;
                return;
            }

            // C) Inspector boþsa sahnede dinamik ara (Fallback)
            GameObject bgObj = GameObject.Find("Background");
            if (bgObj != null)
            {
                SpriteRenderer bgSr = bgObj.GetComponent<SpriteRenderer>();
                if (bgSr != null)
                {
                    bgSr.sprite = item.backgroundSprite;
                    backgroundRenderer = bgSr; // Bir sonraki sefere önbelleðe al
                    return;
                }

                Image bgImg = bgObj.GetComponent<Image>();
                if (bgImg != null)
                {
                    bgImg.sprite = item.backgroundSprite;
                    backgroundImage = bgImg;
                    return;
                }
            }

            Debug.LogWarning("ShopManager: Sahnedeki arka plan nesnesi (SpriteRenderer veya Image) bulunamadý!");
        }
    }

    // --- FÝLTRELEME & YARDIMCILAR ---

    public List<ShopItemData> GetItemsByCategory(ShopCategory category)
    {
        List<ShopItemData> filtered = new List<ShopItemData>();
        foreach (var item in allShopItems)
        {
            if (item != null && item.category == category)
            {
                filtered.Add(item);
            }
        }
        return filtered;
    }

    private string GetPrefKeyForCategory(ShopCategory category)
    {
        return "Equipped_Skin_" + category.ToString();
    }

    private string GetDefaultIdForCategory(ShopCategory category)
    {
        switch (category)
        {
            case ShopCategory.Character: return "skin_male_default";
            case ShopCategory.StickColor: return "stick_default";
            case ShopCategory.Background: return "bg_purple_default";
            default: return string.Empty;
        }
    }
} */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [Header("Tüm Maðaza Kataloðu")]
    [SerializeField] private List<ShopItemData> allShopItems = new List<ShopItemData>();

    [Header("Sahne Görsel Referanslarý (Ýsteðe Baðlý)")]
    [Tooltip("Eðer arkaplan 2D Sprite ise sürükleyin")]
    [SerializeField] private SpriteRenderer backgroundRenderer;
    [Tooltip("Eðer arkaplan Canvas içinde UI Image ise sürükleyin")]
    [SerializeField] private Image backgroundImage;

    public event Action OnShopUpdated;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Oyun açýldýðýnda son seçili karakter, çubuk ve arka planý otomatik sahneye uygula
        ApplyAllEquippedSkins();
    }

    // --- SATIN ALMA & ÝZLEME ÝÞLEMLERÝ ---

    public bool TryBuyWithCoins(ShopItemData item)
    {
        if (item == null || item.IsUnlocked()) return false;

        if (CoinManager.Instance != null && CoinManager.Instance.SpendCoins(item.coinPrice))
        {
            item.SetUnlocked();
            EquipItem(item);
            OnShopUpdated?.Invoke();
            return true;
        }

        return false;
    }

    public void WatchAdForUnlock(ShopItemData item)
    {
        if (item == null || item.IsUnlocked()) return;

        if (AdManager.Instance != null)
        {
            AdManager.Instance.ShowRewardedAd(() =>
            {
                item.AddWatchedAd();
                if (item.IsUnlocked())
                {
                    EquipItem(item);
                }
                OnShopUpdated?.Invoke();
            });
        }
        else
        {
            item.AddWatchedAd();
            if (item.IsUnlocked())
            {
                EquipItem(item);
            }
            OnShopUpdated?.Invoke();
        }
    }

    // --- KUÞANMA / SEÇÝM (EQUIP) ---

    public void EquipItem(ShopItemData item)
    {
        if (item == null || !item.IsUnlocked()) return;

        string key = GetPrefKeyForCategory(item.category);
        PlayerPrefs.SetString(key, item.id);
        PlayerPrefs.Save();

        ApplyEquippedSkin(item);
        OnShopUpdated?.Invoke();
    }

    public bool IsEquipped(ShopItemData item)
    {
        if (item == null) return false;

        ShopItemData currentEquipped = GetEquippedItem(item.category);
        return currentEquipped != null && currentEquipped.id == item.id;
    }

    /// <summary>
    /// Aktif/kuþanýlmýþ olan eþyayý getirir
    /// </summary>
    public ShopItemData GetEquippedItem(ShopCategory category)
    {
        string key = GetPrefKeyForCategory(category);
        string defaultId = GetDefaultIdForCategory(category);
        string equippedId = PlayerPrefs.GetString(key, defaultId);

        // 1. Kayýtlý ID ile eþleþen öðeyi bul
        foreach (var item in allShopItems)
        {
            if (item != null && item.category == category && item.id == equippedId)
            {
                return item;
            }
        }

        // 2. Bulunamazsa o kategorideki ilk ücretsiz (Free) öðeyi getir
        foreach (var item in allShopItems)
        {
            if (item != null && item.category == category && item.unlockType == UnlockType.Free)
            {
                return item;
            }
        }

        return null;
    }

    // --- GÖRSEL UYGULAMA (APPLY) ---

    public void ApplyAllEquippedSkins()
    {
        ShopItemData charItem = GetEquippedItem(ShopCategory.Character);
        if (charItem != null) ApplyEquippedSkin(charItem);

        ShopItemData stickItem = GetEquippedItem(ShopCategory.StickColor);
        if (stickItem != null) ApplyEquippedSkin(stickItem);

        ShopItemData bgItem = GetEquippedItem(ShopCategory.Background);
        if (bgItem != null) ApplyEquippedSkin(bgItem);
    }

    public void ApplyEquippedSkin(ShopItemData item)
    {
        if (item == null) return;

        // 1. Karakter Sprite Güncellemesi
        if (item.category == ShopCategory.Character && item.characterSprite != null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                SpriteRenderer sr = playerObj.GetComponentInChildren<SpriteRenderer>();
                if (sr != null) sr.sprite = item.characterSprite;
            }
        }

        // 2. Çubuk Rengi Güncellemesi
        if (item.category == ShopCategory.StickColor)
        {
            GameObject stickObj = GameObject.Find("Stick");
            if (stickObj != null)
            {
                StickController stickCtrl = stickObj.GetComponent<StickController>();
                if (stickCtrl != null)
                {
                    stickCtrl.ApplyEquippedStickColor();
                }
                else
                {
                    SpriteRenderer stickSr = stickObj.GetComponentInChildren<SpriteRenderer>();
                    if (stickSr != null) stickSr.color = item.stickColor;
                }
            }
        }

        // 3. Arka Plan & Tematik Platform Rengi Güncellemesi
        if (item.category == ShopCategory.Background && item.backgroundSprite != null)
        {
            // Arka Plan Görselini Deðiþtir
            if (backgroundRenderer != null)
            {
                backgroundRenderer.sprite = item.backgroundSprite;
            }
            else if (backgroundImage != null)
            {
                backgroundImage.sprite = item.backgroundSprite;
            }
            else
            {
                GameObject bgObj = GameObject.Find("Background");
                if (bgObj != null)
                {
                    SpriteRenderer bgSr = bgObj.GetComponent<SpriteRenderer>();
                    if (bgSr != null)
                    {
                        bgSr.sprite = item.backgroundSprite;
                        backgroundRenderer = bgSr;
                    }
                    else
                    {
                        Image bgImg = bgObj.GetComponent<Image>();
                        if (bgImg != null)
                        {
                            bgImg.sprite = item.backgroundSprite;
                            backgroundImage = bgImg;
                        }
                    }
                }
            }

            // Sahnedeki Mevcut Platformlarý Yeni Renge Boya
            ApplyPlatformColorToScene(item.platformColor);
        }
    }

    /// <summary>
    /// Sahnede þu an var olan tüm platformlarýn rengini canlý günceller
    /// </summary>
    public void ApplyPlatformColorToScene(Color targetColor)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateAllActivePlatformColors(targetColor);
        }
    }

    // --- FÝLTRELEME & YARDIMCILAR ---

    public List<ShopItemData> GetItemsByCategory(ShopCategory category)
    {
        List<ShopItemData> filtered = new List<ShopItemData>();
        foreach (var item in allShopItems)
        {
            if (item != null && item.category == category)
            {
                filtered.Add(item);
            }
        }
        return filtered;
    }

    private string GetPrefKeyForCategory(ShopCategory category)
    {
        return "Equipped_Skin_" + category.ToString();
    }

    private string GetDefaultIdForCategory(ShopCategory category)
    {
        switch (category)
        {
            case ShopCategory.Character: return "skin_male_default";
            case ShopCategory.StickColor: return "stick_default";
            case ShopCategory.Background: return "bg_purple_default";
            default: return string.Empty;
        }
    }
}