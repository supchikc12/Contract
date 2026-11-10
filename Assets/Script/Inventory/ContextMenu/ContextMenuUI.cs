using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.EventSystems;

public class ContextMenuUI : MonoBehaviour
{
    [SerializeField] private float offsetX = 100f;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private GameObject buttonPrefab;

    private List<IItemAction> allActions = new List<IItemAction>();
    private RectTransform menuRect;
    private GameObject buttonsContainer;

    private void Awake()
    {
        allActions.Add(new DropAction());
        allActions.Add(new UseAction());
        allActions.Add(new OpenAction());
        allActions.Add(new EquipeItem());   
        allActions.Add(new ModifyWeaponAction());
        allActions.Add(new UnequipAction());
        allActions.Add(new SplitAction());

        menuRect = GetComponent<RectTransform>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void Update()
    {
        // Если меню открыто
        if (canvasGroup.alpha > 0)
        {
            // Закрытие на Escape 
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Hide();
            }
            // Закрытие на ПКМ мимо меню
            else if (Input.GetMouseButtonDown(1))
            {
                PointerEventData eventData = new PointerEventData(EventSystem.current);
                eventData.position = Input.mousePosition;

                List<RaycastResult> results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(eventData, results);

                bool clickedValidTarget = false;

                foreach (var result in results)
                {
                    // Если кликнули по предмету — не скрываем (предмет сам вызовет Show и обновит меню)
                    if (result.gameObject.GetComponentInParent<ItemInstance>() != null)
                    {
                        clickedValidTarget = true;
                        break;
                    }
                    // Если кликнули по слоту — не скрываем (слот сам вызовет Show)
                    if (result.gameObject.GetComponentInParent<EquipmentSlot>() != null)
                    {
                        clickedValidTarget = true;
                        break;
                    }
                    // Если кликнули по самому меню или его кнопкам — не скрываем
                    if (result.gameObject.transform.IsChildOf(menuRect.transform) || result.gameObject == menuRect.gameObject)
                    {
                        clickedValidTarget = true;
                        break;
                    }
                }

                // Если кликнули в пустоту или по UI, который не является предметом/слотом/меню — скрываем
                if (!clickedValidTarget)
                {
                    Hide();
                }
            }
        }
    }

    public void Show(ItemInstance item)
    {
        PositionMenu(item);
        buttonsContainer = CreateVerticalLayoutGroup();
        CreateButtons(item);

        transform.SetAsLastSibling();

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    public void Hide()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void PositionMenu(ItemInstance item)
    {
        Canvas canvas = item.GetComponentInParent<Canvas>();
        Vector3 worldPosition = item.transform.position;
        RectTransform rectTransform = canvas.GetComponent<RectTransform>();

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, worldPosition);
        RectTransform rectTransformItem = item.GetComponent<RectTransform>();
        
        screenPoint.x += offsetX + (rectTransformItem.sizeDelta.x / 2);
        
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, null, out Vector2 localPoint))
        {
            menuRect.anchoredPosition = localPoint;
        }
        
    }

    private GameObject CreateVerticalLayoutGroup()
    {
        if (buttonsContainer != null)
        {
            ClearButtonsContainer();
            return buttonsContainer;
        }
            
        GameObject contextMenu = new GameObject("ButtonsContainer", typeof(RectTransform));
        VerticalLayoutGroup vlg = contextMenu.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 5f;
        vlg.padding = new RectOffset(10, 10, 10, 10);
        vlg.childAlignment = TextAnchor.UpperLeft;

        ContentSizeFitter fitter = contextMenu.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        contextMenu.transform.SetParent(menuRect, false);

        return contextMenu;

    }

    private void CreateButtons(ItemInstance item) 
    {
        if (buttonPrefab == null) { Debug.LogError("buttonPrefab не назначен!"); return; }

        foreach (var action in allActions) 
        {
            if (action.CanExecute(item))
            {
                GameObject newButton = Instantiate(buttonPrefab, buttonsContainer.transform, false);
                newButton.layer = LayerMask.NameToLayer("UI");
                TMP_Text text = newButton.GetComponentInChildren<TMP_Text>();
                text.text = action.ActionName;
                Button button = newButton.GetComponent<Button>();
                IItemAction captured = action;
                button.onClick.AddListener(() => { captured.Execute(item); Hide(); });
            }
        }
    }

    private void ClearButtonsContainer() 
    {
        if (buttonsContainer == null) return;

        while (buttonsContainer.transform.childCount > 0)
        {
            Transform child = buttonsContainer.transform.GetChild(0);
            child.SetParent(null); // Мгновенно убирает из контейнера ? childCount уменьшается
            Destroy(child.gameObject);
        }
    }
}
