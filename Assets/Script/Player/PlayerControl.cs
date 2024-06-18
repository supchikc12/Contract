using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5.0f; // Скорость движения персонажа
    public float reloadSpeed;
    private CharacterController controller;
    private PlayerInventory _playerInventory;
    private InventoryObject _inventoryObject;
    private Camera main_camera;
    Vector3 Ray_start_position = new Vector3(Screen.width / 2, Screen.height / 2, 0);
    public GameObject _inHands;
    public Transform _targetHand;
    public Gun _gun;


    private void Start()
    {
        controller = GetComponent<CharacterController>();
        main_camera = GameObject.Find("Main Camera").GetComponent<Camera>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        // Получаем ввод от игрока
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        // Вычисляем направление движения
        Vector3 moveDirection = transform.forward * verticalInput + transform.right * horizontalInput;

        // Применяем гравитацию
        moveDirection.y -= 9.81f;

        // Двигаем персонажа
        controller.Move(moveDirection * moveSpeed * Time.deltaTime);
            
        // Инвентарь
        //if (Input.GetKey(KeyCode.I))
        //{

        //}

        // Подбор предметов
        if (Input.GetKey(KeyCode.F))
        {
            // Сам луч
            Ray ray = main_camera.ScreenPointToRay(Ray_start_position);
            // Запись объекта, в который пришел луч, в переменную
            RaycastHit hit;
            Physics.Raycast(ray, out hit);
            //просто для наглядности рисуем луч в окне Scene
            Debug.DrawLine(ray.origin, hit.point, Color.red);
            _playerInventory = GetComponent<PlayerInventory>();

            if (_inHands == null && hit.collider.tag == "Item" || hit.collider.tag == "Weapon")
            {
                _inHands = hit.collider.gameObject;
            }
            else if (_inHands != null && hit.collider.tag == "Item" || hit.collider.tag == "Weapon")
            {
               
                _playerInventory.TakeItem(hit);
            }
            else if (_playerInventory._backPack == null && hit.collider.tag == "ItemClothing")
                _playerInventory._backPack = hit.collider.gameObject;
        }
        // Перезарядка
        else if (Input.GetKeyUp(KeyCode.R) && _inHands.tag == "Weapon")
        {
            _playerInventory = GetComponent<PlayerInventory>();
            _gun = _inHands.GetComponent<Gun>();
            if (_playerInventory._backPack != null)
            {
                _inventoryObject = _playerInventory._backPack.GetComponent<InventoryObject>();
                for (int i = _inventoryObject._item.Count - 1; i > 0; i--)
                {
                    Item item = _gun._idMagazinType.GetComponent<Item>();
                    if (_inventoryObject._item[i] == item._id)
                    {
                        for (int b = item._slot; b>0;b--)
                        {                            
                            Debug.Log(b);
                            _inventoryObject._item.Remove(_inventoryObject._item[i]);
                            i--;
                        }
                        break;
                    }

                }

            }

        }
        if (_inHands != null)
        {
            _inHands.transform.position = _targetHand.position;
            _inHands.transform.rotation = gameObject.transform.rotation;

            if (Input.GetMouseButton(0))
            {
                if (_inHands.tag == "Weapon")
                {
                    _gun = _inHands.GetComponent<Gun>();
                    if (_gun.time <= 0)
                        _gun.Fire();
                }
            }
        }
    }
}
