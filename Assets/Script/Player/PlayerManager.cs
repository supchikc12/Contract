using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [Header("Интерфейс инвенторя")]
    [SerializeField] private GameObject Inventory;
    [Header("Настройки")]
    [Range(0.1f, 9f)]
    [SerializeField] private float sensitivity = 2f;
    [SerializeField] public float speed = 5f;
    [SerializeField] public float gravity = -9.81f; // Сила гравитации
    [SerializeField] public float jumpHeight = 1.5f;

    [Header("Ограничения")]
    [Range(0f, 90f)]
    [SerializeField] private float yRotationLimit = 88f;
    [SerializeField] public List<GameObject> SelectedObj;

    // Внутренние переменные для хранения текущего угла поворота
    private Vector2 rotation = Vector2.zero;

    // Имена осей в Input Manager. Храним их как константы, чтобы не создавать мусор в памяти
    private const string MouseXAxis = "Mouse X";
    private const string MouseYAxis = "Mouse Y";

    private CharacterController controller;
    private Vector3 velocity;

    private void Awake()
    {
        SelectedObj = new List<GameObject>();
        controller = GetComponent<CharacterController>();
        // ЗАБЛОКИРОВАТЬ КУРСОР В ЦЕНТРЕ ЭКРАНА
        Cursor.lockState = CursorLockMode.Locked;
        InventoryManager inv = gameObject.GetComponentInChildren<InventoryManager>(true);
        if (inv == null)
        {
            Debug.LogError("InventoryManager не найден!");
            return;
        }
        Inventory = inv.gameObject;
    }

    private void Update()
    {
        CameraLook();

        if (Input.GetKeyDown(KeyCode.I)) {
            OpenOrCloseInventory();            
        }

        // 1. Получаем ввод (WASD)
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // 2. Движение относительно камеры (чтобы W было "от себя")
        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * speed * Time.deltaTime);

        // 3. Прыжок
        if (Input.GetButtonDown("Jump") && controller.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // 4. Гравитация
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Небольшой прижим к земле
        }
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void OpenOrCloseInventory() {

        if (Inventory == null) return;

        Inventory.SetActive(!Inventory.activeSelf);
        InventoryManager inventoryManager = Inventory.GetComponent<InventoryManager>();
        if (Inventory.activeSelf)
        {
            Cursor.lockState = CursorLockMode.None; // Мышь свободно двигается
            Cursor.visible = true;                  // Показываем курсор
            inventoryManager.AddCollectionZone(SelectedObj);
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked; // Захватываем мышь в центр
            Cursor.visible = false;                   // Прячем курсор
            inventoryManager.ReplaceCollectionZone(SelectedObj);
        }
    }

    void CameraLook() {
        // 1. Получаем ввод от мыши и умножаем на чувствительность
        float mouseX = Input.GetAxis(MouseXAxis) * sensitivity;
        float mouseY = Input.GetAxis(MouseYAxis) * sensitivity;

        // 2. Накапливаем углы поворота
        // По горизонтали (влево-вправо) углы могут быть любыми, поэтому просто прибавляем
        rotation.x += mouseX;

        // По вертикали (вверх-вниз) мы прибавляем, но потом ограничиваем
        rotation.y += mouseY;
        rotation.y = Mathf.Clamp(rotation.y, -yRotationLimit, yRotationLimit);

        // 3. Преобразуем накопленные углы в поворот (кватернионы)
        // Вращение по оси Y (горизонталь) вокруг вектора вверх (Vector3.up)
        Quaternion xQuaternion = Quaternion.AngleAxis(rotation.x, Vector3.up);

        // Вращение по оси X (вертикаль) вокруг вектора влево (Vector3.left) - так работает в Unity
        Quaternion yQuaternion = Quaternion.AngleAxis(rotation.y, Vector3.left);

        // 4. Применяем поворот к камере
        // Умножаем кватернионы (сначала вертикаль, потом горизонталь)
        transform.localRotation = xQuaternion * yQuaternion;
    }
    // Публичный метод, чтобы можно было менять чувствительность из других скриптов (например, из меню настроек)
    public void SetSensitivity(float newSensitivity)
    {
        sensitivity = newSensitivity;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == 8)
            SelectedObj.Add(other.gameObject);
    }

    private void OnTriggerExit(Collider other)
    {
        SelectedObj.Remove(other.gameObject);
    }
}
