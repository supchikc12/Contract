using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Отображение надетых контейнеров в правом доке InventoryFrame.
// Отвечает только за карточки, стрелки и перенос панелей; хранением не занимается.
public class InventoryContainerDock : MonoBehaviour
{
    [SerializeField] private InventoryManager manager;

    [Header("Рюкзак")]
    [SerializeField] private RectTransform backpackSection;
    [SerializeField] private RectTransform backpackHost;
    [SerializeField] private GameObject backpackCollapsed;
    [SerializeField] private Image backpackIcon;
    [SerializeField] private TMP_Text backpackName;
    [SerializeField] private Button backpackArrow;
    [SerializeField] private LayoutElement backpackLayout;

    [Header("Разгрузка")]
    [SerializeField] private RectTransform rigSection;
    [SerializeField] private RectTransform rigHost;
    [SerializeField] private GameObject rigCollapsed;
    [SerializeField] private Image rigIcon;
    [SerializeField] private TMP_Text rigName;
    [SerializeField] private Button rigArrow;
    [SerializeField] private LayoutElement rigLayout;

    [Header("Размеры карточек")]
    [SerializeField] private float collapsedWidth = 320f;
    [SerializeField] private float collapsedHeight = 300f;
    [SerializeField] private float expandedWidth = 800f;
    [SerializeField] private float expandedHeight = 440f;

    private ItemInstance displayedBackpack;
    private ItemInstance displayedRig;
    private bool backpackExpanded;
    private bool rigExpanded;

    private void Awake()
    {
        if (manager == null) manager = GetComponentInParent<InventoryManager>();
    }

    private void Start()
    {
        Refresh();
    }

    private void OnEnable()
    {
        SetupDockArrow(backpackArrow, true);
        SetupDockArrow(rigArrow, false);
        Refresh();
    }

    private void OnDisable()
    {
        RestoreDockSection(ref displayedBackpack, backpackHost);
        RestoreDockSection(ref displayedRig, rigHost);
    }

    // Показывает карточки надетых контейнеров и применяет сохранённое состояние.
    public void Refresh()
    {
        if (manager == null) manager = GetComponentInParent<InventoryManager>();
        if (manager == null) return;

        ItemInstance backpack = manager.FindEquippedContainer(ItemCategory.Backpack);
        ItemInstance rig = manager.FindEquippedContainer(ItemCategory.Rig);

        UpdateDockSection(backpack, ref displayedBackpack,
            backpackSection, backpackHost, backpackCollapsed,
            backpackIcon, backpackName, backpackArrow, backpackLayout,
            backpackExpanded, "РЮКЗАК");

        UpdateDockSection(rig, ref displayedRig,
            rigSection, rigHost, rigCollapsed,
            rigIcon, rigName, rigArrow, rigLayout,
            rigExpanded, "РАЗГРУЗКА");
    }

    // Возвращает панель контейнера, если она сейчас отображается в доке.
    public Transform GetContainerPanel(ItemInstance container)
    {
        if (container == null) return null;

        if (container == displayedBackpack && backpackHost != null)
            return backpackHost.Find("Conteyner");

        if (container == displayedRig && rigHost != null)
            return rigHost.Find("Conteyner");

        return null;
    }

    // Возвращает панель контейнера из дока обратно в предмет.
    public void RestoreContainer(ItemInstance container)
    {
        if (container == null) return;

        if (container == displayedBackpack)
            RestoreDockSection(ref displayedBackpack, backpackHost);
        else if (container == displayedRig)
            RestoreDockSection(ref displayedRig, rigHost);
    }

    private void UpdateDockSection(ItemInstance container, ref ItemInstance displayed,
        RectTransform section, RectTransform host, GameObject collapsed,
        Image icon, TMP_Text nameLabel, Button arrow, LayoutElement layout,
        bool expanded, string title)
    {
        if (section == null) return;

        if (container == null)
        {
            if (displayed != null) RestoreDockSection(ref displayed, host);
            section.gameObject.SetActive(false);
            return;
        }

        section.gameObject.SetActive(true);

        if (displayed != container)
        {
            if (displayed != null) RestoreDockSection(ref displayed, host);

            ItemState state = container.GetItemState();
            if (nameLabel != null)
            {
                nameLabel.text = state != null && state.data != null
                    ? state.data.itemName.ToUpperInvariant()
                    : title;
            }

            if (icon != null)
            {
                icon.sprite = state != null && state.data != null ? state.data.icon : null;
                icon.enabled = icon.sprite != null;
            }
        }

        if (collapsed != null) collapsed.SetActive(!expanded);
        if (host != null) host.gameObject.SetActive(expanded);

        if (layout != null)
        {
            RectTransform parent = section.parent as RectTransform;
            float maxWidth = parent != null ? Mathf.Max(collapsedWidth, parent.rect.width - 16f) : expandedWidth;
            layout.preferredWidth = expanded ? Mathf.Min(expandedWidth, maxWidth) : collapsedWidth;
            layout.preferredHeight = expanded ? expandedHeight : collapsedHeight;
        }

        RefreshArrowVisual(arrow, expanded);

        if (expanded)
        {
            DisplayDockSection(container, host);
            displayed = container;
        }
        else if (displayed != null)
        {
            RestoreDockSection(ref displayed, host);
        }
    }

    private void DisplayDockSection(ItemInstance container, RectTransform host)
    {
        if (host == null) return;

        Transform panel = container.transform.Find("Conteyner");
        if (panel == null) return;

        panel.SetParent(host, false);
        panel.gameObject.SetActive(true);

        InitializeGrid grid = panel.GetComponentInChildren<InitializeGrid>(true);
        if (grid != null && grid.listGrid.Count == 0) grid.TryInit();

        RectTransform rect = panel as RectTransform;
        if (rect != null)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(4f, 4f);
            rect.offsetMax = new Vector2(-4f, -4f);
            rect.localScale = Vector3.one;
        }

        // Заголовок и рамка панели не нужны: у карточки свой заголовок.
        Transform header = panel.Find("Header");
        if (header != null) header.gameObject.SetActive(false);
        Transform frame = panel.Find("Frame");
        if (frame != null) frame.gameObject.SetActive(false);

        Image panelImage = panel.GetComponent<Image>();
        if (panelImage != null) panelImage.color = new Color(0.8f, 0.81f, 0.79f, 0.08f);

        Transform scroll = panel.Find("Scroll View");
        RectTransform scrollRect = scroll as RectTransform;
        if (scrollRect != null)
        {
            // Сетка 50?15 остаётся с ячейкой 50 px: уменьшаем окно, а не масштаб.
            scrollRect.offsetMin = new Vector2(8f, 8f);
            scrollRect.offsetMax = new Vector2(-8f, -8f);
        }
        Image scrollImage = scroll != null ? scroll.GetComponent<Image>() : null;
        if (scrollImage != null) scrollImage.color = new Color(0.58f, 0.59f, 0.57f, 0.30f);

        ContainerSizeHandler sizeHandler = panel.GetComponent<ContainerSizeHandler>();
        if (sizeHandler != null) sizeHandler.enabled = false;

        UIDragHandler dragHandler = panel.GetComponent<UIDragHandler>();
        if (dragHandler != null) dragHandler.enabled = false;

        CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }

    private void RestoreDockSection(ref ItemInstance displayed, RectTransform host)
    {
        if (displayed == null) return;

        Transform panel = host != null ? host.Find("Conteyner") : null;
        if (panel != null)
        {
            panel.SetParent(displayed.transform, false);

            RectTransform rect = panel as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(800f, 800f);
                rect.localPosition = Vector3.zero;
            }

            panel.localScale = Vector3.one;

            Transform header = panel.Find("Header");
            if (header != null) header.gameObject.SetActive(true);
            Transform frame = panel.Find("Frame");
            if (frame != null) frame.gameObject.SetActive(true);

            Image panelImage = panel.GetComponent<Image>();
            if (panelImage != null) panelImage.color = new Color(0.72f, 0.73f, 0.71f, 0.55f);

            Transform scroll = panel.Find("Scroll View");
            RectTransform scrollRect = scroll as RectTransform;
            if (scrollRect != null)
            {
                scrollRect.offsetMin = new Vector2(50f, 0f);
                scrollRect.offsetMax = new Vector2(-50f, -80f);
            }
            Image scrollImage = scroll != null ? scroll.GetComponent<Image>() : null;
            if (scrollImage != null) scrollImage.color = new Color(0.62f, 0.63f, 0.61f, 0.45f);

            ContainerSizeHandler sizeHandler = panel.GetComponent<ContainerSizeHandler>();
            if (sizeHandler != null) sizeHandler.enabled = true;

            UIDragHandler dragHandler = panel.GetComponent<UIDragHandler>();
            if (dragHandler != null) dragHandler.enabled = true;

            panel.gameObject.SetActive(false);
        }

        displayed = null;
    }

    private void SetupDockArrow(Button arrow, bool backpack)
    {
        if (arrow == null) return;

        arrow.onClick.RemoveAllListeners();
        arrow.onClick.AddListener(() =>
        {
            if (backpack) backpackExpanded = !backpackExpanded;
            else rigExpanded = !rigExpanded;

            Refresh();
        });
    }

    private void RefreshArrowVisual(Button arrow, bool expanded)
    {
        if (arrow == null) return;

        TMP_Text label = arrow.GetComponentInChildren<TMP_Text>();
        if (label != null)
        {
            label.text = ">";
            label.rectTransform.localRotation = expanded
                ? Quaternion.Euler(0f, 0f, -90f)
                : Quaternion.identity;
        }
    }
}
