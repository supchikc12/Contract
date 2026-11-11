using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Окно модификации оружия: показывает слоты и позволяет ставить/снимать моды
public class WeaponModdingUI : MonoBehaviour
{
    public static WeaponModdingUI Instance { get; private set; }

    private ItemInstance currentWeapon;
    private GameObject panel;
    private Image weaponIcon;
    private TMP_Text weaponName;
    private readonly List<WeaponModSlotUI> slots = new List<WeaponModSlotUI>();
    private bool built;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (panel != null && panel.activeSelf && currentWeapon == null)
            Close();
    }

    public void Open(ItemInstance weapon)
    {
        if (weapon == null) return;

        ItemState state = weapon.GetItemState();
        if (state == null || state.data == null) return;
        if ((state.data.category & ItemCategory.Weapon) == 0) return;

        BuildUI();
        currentWeapon = weapon;
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
        Refresh();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        currentWeapon = null;
    }

    public void Install(WeaponModSlotType type, ItemInstance modItem)
    {
        if (currentWeapon == null || modItem == null || type == WeaponModSlotType.None) return;

        ItemState weaponState = currentWeapon.GetItemState();
        ItemState modState = modItem.GetItemState();
        if (weaponState == null || modState == null || modState.data == null) return;

        WeaponModData modData = modState.data as WeaponModData;
        if (modData == null || modData.slotType != type)
        {
            Debug.LogWarning("Модификация не подходит к этому слоту");
            return;
        }

        if (!modData.CanInstallOn(weaponState.data))
        {
            Debug.LogWarning("Модификация не совместима с этим оружием");
            return;
        }

        // Снимаем старую модификацию из этого слота
        RemoveInstalled(type, true);

        weaponState.installedMods.Add(modItem.GetStateData());
        modItem.MarkDropHandled();

        InventoryManager manager = InventoryManager.Instance;
        if (manager != null)
        {
                manager.ClearDragState();
        }

        modItem.ReleaseCells();
        Destroy(modItem.gameObject);
        Refresh();
    }

    public void Uninstall(WeaponModSlotType type)
    {
        if (RemoveInstalled(type, true)) Refresh();
    }

    private bool RemoveInstalled(WeaponModSlotType type, bool returnToInventory)
    {
        if (currentWeapon == null) return false;

        ItemState weaponState = currentWeapon.GetItemState();
        if (weaponState == null || weaponState.installedMods == null) return false;

        for (int i = 0; i < weaponState.installedMods.Count; i++)
        {
            ItemStateData modData = weaponState.installedMods[i];
            WeaponModData weaponModData = modData.data as WeaponModData;
            if (weaponModData == null || weaponModData.slotType != type) continue;

            weaponState.installedMods.RemoveAt(i);
            if (returnToInventory) ReturnModToInventory(weaponModData, modData);
            return true;
        }

        return false;
    }

    private void ReturnModToInventory(WeaponModData modData, ItemStateData modState)
    {
        if (modData == null || modData.modelPrefab2D == null) return;

        InventoryManager manager = InventoryManager.Instance;
        if (manager == null) return;

        GameObject modObj = Instantiate(modData.modelPrefab2D, manager.transform);
        modObj.name = modData.itemName + "_2D";
        ItemInstance modItem = modObj.GetComponent<ItemInstance>();
        if (modItem == null)
        {
            Destroy(modObj);
            return;
        }

        modObj.SetActive(true);
        ItemState state = modItem.GetItemState();
        if (state != null) state.SetStateData(modState);
        modItem.Init();

        if (!manager.TryAddItemToInventory(modItem))
            manager.DropItem(modItem);
    }

    private void Refresh()
    {
        if (currentWeapon == null) return;

        ItemState state = currentWeapon.GetItemState();
        if (state == null || state.data == null)
        {
            Close();
            return;
        }

        if (weaponIcon != null)
        {
            weaponIcon.sprite = state.data.icon;
            weaponIcon.enabled = state.data.icon != null;
            weaponIcon.preserveAspect = true;
        }
        if (weaponName != null) weaponName.text = state.data.itemName;

        foreach (WeaponModSlotUI slot in slots)
        {
            WeaponModData installed = FindInstalled(state, slot.SlotType);
            slot.SetInstalled(installed);
        }
    }

    private static WeaponModData FindInstalled(ItemState state, WeaponModSlotType type)
    {
        if (state == null || state.installedMods == null) return null;

        foreach (ItemStateData mod in state.installedMods)
        {
            WeaponModData data = mod.data as WeaponModData;
            if (data != null && data.slotType == type) return data;
        }

        return null;
    }

    private void BuildUI()
    {
        if (built) return;
        built = true;

        panel = new GameObject("WeaponModdingPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(transform, false);

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(980f, 820f);
        panelRect.anchoredPosition = Vector2.zero;

        Image background = panel.GetComponent<Image>();
        background.color = new Color(0.07f, 0.08f, 0.1f, 0.98f);
        background.raycastTarget = true;

        CreateText(panel.transform, "Title", "МОДИФИКАЦИЯ ОРУЖИЯ", 28, new Vector2(0f, 370f), new Vector2(600f, 50f), Color.white);

        weaponIcon = CreateImage(panel.transform, "WeaponIcon", new Vector2(0f, 235f), new Vector2(180f, 180f), Color.white);
        weaponIcon.preserveAspect = true;

        weaponName = CreateText(panel.transform, "WeaponName", "", 22, new Vector2(0f, 120f), new Vector2(600f, 40f), Color.white);

        GameObject closeGO = new GameObject("CloseButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        closeGO.transform.SetParent(panel.transform, false);
        RectTransform closeRect = closeGO.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.5f, 0.5f);
        closeRect.anchorMax = new Vector2(0.5f, 0.5f);
        closeRect.pivot = new Vector2(0.5f, 0.5f);
        closeRect.sizeDelta = new Vector2(140f, 44f);
        closeRect.anchoredPosition = new Vector2(390f, 370f);

        Image closeImage = closeGO.GetComponent<Image>();
        closeImage.color = new Color(0.35f, 0.12f, 0.12f, 1f);
        Button closeButton = closeGO.GetComponent<Button>();
        closeButton.onClick.AddListener(Close);
        CreateText(closeGO.transform, "Label", "Закрыть", 20, Vector2.zero, new Vector2(140f, 44f), Color.white);

        CreateText(panel.transform, "Hint", "Перетащите модификацию в подходящий слот. ПКМ по слоту — снять.", 16, new Vector2(0f, -370f), new Vector2(900f, 30f), new Color(0.75f, 0.78f, 0.82f, 1f));

        slots.Clear();
        AddSlot(WeaponModSlotType.Scope, "Прицел", new Vector2(-240f, 30f));
        AddSlot(WeaponModSlotType.Muzzle, "Дульное устройство", new Vector2(240f, 30f));
        AddSlot(WeaponModSlotType.Stock, "Приклад", new Vector2(-240f, -100f));
        AddSlot(WeaponModSlotType.Grip, "Рукоятка", new Vector2(240f, -100f));
        AddSlot(WeaponModSlotType.Underbarrel, "Подствольник", new Vector2(-240f, -230f));
        AddSlot(WeaponModSlotType.SideLeft, "ЛЦУ слева", new Vector2(240f, -230f));
        AddSlot(WeaponModSlotType.SideRight, "ЛЦУ справа", new Vector2(0f, -230f));

        panel.SetActive(false);
    }

    private void AddSlot(WeaponModSlotType type, string label, Vector2 position)
    {
        GameObject slotGO = new GameObject("Slot_" + type, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(WeaponModSlotUI));
        slotGO.transform.SetParent(panel.transform, false);

        RectTransform slotRect = slotGO.GetComponent<RectTransform>();
        slotRect.anchorMin = new Vector2(0.5f, 0.5f);
        slotRect.anchorMax = new Vector2(0.5f, 0.5f);
        slotRect.pivot = new Vector2(0.5f, 0.5f);
        slotRect.sizeDelta = new Vector2(180f, 110f);
        slotRect.anchoredPosition = position;

        Image slotBackground = slotGO.GetComponent<Image>();
        slotBackground.color = new Color(0.16f, 0.18f, 0.22f, 1f);
        slotBackground.raycastTarget = true;

        Image icon = CreateImage(slotGO.transform, "Icon", new Vector2(0f, 10f), new Vector2(90f, 60f), Color.white);
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        TMP_Text slotLabel = CreateText(slotGO.transform, "SlotLabel", label, 15, new Vector2(0f, -40f), new Vector2(170f, 24f), new Color(0.85f, 0.87f, 0.9f, 1f));
        TMP_Text modLabel = CreateText(slotGO.transform, "ModLabel", "", 13, new Vector2(0f, 42f), new Vector2(170f, 20f), new Color(0.6f, 0.9f, 0.6f, 1f));

        WeaponModSlotUI slot = slotGO.GetComponent<WeaponModSlotUI>();
        slot.Configure(this, type, label, icon, slotLabel, modLabel);
        slots.Add(slot);
    }

    private static Image CreateImage(Transform parent, string name, Vector2 position, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        Image image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static TMP_Text CreateText(Transform parent, string name, string text, float size, Vector2 position, Vector2 rectSize, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = rectSize;
        rect.anchoredPosition = position;

        TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
        label.font = GetFont();
        label.fontSize = size;
        label.color = color;
        label.text = text;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }

    private static TMP_FontAsset GetFont()
    {
        if (TMP_Settings.defaultFontAsset != null) return TMP_Settings.defaultFontAsset;
        return Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
    }
}
