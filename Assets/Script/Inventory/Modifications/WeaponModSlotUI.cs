using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// UI-слот для одной модификации в окне модификации оружия
public class WeaponModSlotUI : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text slotLabel;
    [SerializeField] private TMP_Text modLabel;

    private WeaponModdingUI owner;
    private WeaponModSlotType slotType;

    public WeaponModSlotType SlotType => slotType;

    public void Configure(WeaponModdingUI owner, WeaponModSlotType type, string label, Image icon, TMP_Text slotText, TMP_Text modText)
    {
        this.owner = owner;
        this.slotType = type;
        this.iconImage = icon;
        this.slotLabel = slotText;
        this.modLabel = modText;

        if (slotLabel != null) slotLabel.text = label;
        if (modLabel != null) modLabel.text = "";
        if (iconImage != null) iconImage.enabled = false;
    }

    public void SetInstalled(WeaponModData mod)
    {
        if (mod == null || mod.icon == null)
        {
            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }
            if (modLabel != null) modLabel.text = "";
            return;
        }

        if (iconImage != null)
        {
            iconImage.sprite = mod.icon;
            iconImage.enabled = true;
        }
        if (modLabel != null) modLabel.text = mod.itemName;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (owner == null || eventData == null || eventData.pointerDrag == null) return;

        ItemInstance modItem = eventData.pointerDrag.GetComponent<ItemInstance>();
        if (modItem == null) return;

        owner.Install(slotType, modItem);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData != null && eventData.button == PointerEventData.InputButton.Right && owner != null)
        {
            owner.Uninstall(slotType);
        }
    }
}
