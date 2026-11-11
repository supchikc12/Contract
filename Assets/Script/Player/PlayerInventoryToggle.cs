using UnityEngine;

// ќткрытие/закрытие инвентар€ по I, курсор и блокировка ввода движени€.
// Ќе содержит логики хранени€ Ч только окно и состо€ние ввода.
public class PlayerInventoryToggle : MonoBehaviour
{
    [SerializeField] private GameObject inventory;
    [SerializeField] private PlayerMotor motor;

    public bool IsOpen => inventory != null && inventory.activeSelf;

    private void Awake()
    {
        if (motor == null) motor = GetComponent<PlayerMotor>();

        if (inventory == null)
        {
            InventoryManager manager = GetComponentInChildren<InventoryManager>(true);
            if (manager != null) inventory = manager.gameObject;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Start()
    {
        // —цена могла быть сохранена с открытым инвентарЄм Ч начинаем с закрытым.
        if (IsOpen) SetOpen(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.I)) Toggle();
    }

    public void Toggle() => SetOpen(!IsOpen);

    public void SetOpen(bool open)
    {
        if (inventory == null) return;

        inventory.SetActive(open);
        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = open;

        if (motor != null) motor.InputEnabled = !open;

        if (!open && WeaponModdingUI.Instance != null)
            WeaponModdingUI.Instance.Close();
    }
}
