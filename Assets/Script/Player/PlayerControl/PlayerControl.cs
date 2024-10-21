using System.Threading;
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
    public GameObject _canvas;
    public InventoryController _inventoryController;
    public GameObject _Granade;

    private void Awake()
    {
        //Ray_start_position += new Vector3(250f, 0, 0);
        controller = GetComponent<CharacterController>();
        main_camera = GameObject.Find("Main Camera").GetComponent<Camera>();
    }

    private void Start()
    {
        
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
        
       
        if (Input.GetKeyUp(KeyCode.Q))
        {
            if (_inHands != null) 
            {
               
                GameObject ObjectDrop = Instantiate(_inHands,_targetHand);
                ObjectDrop.transform.parent = null;
                Destroy(_inHands);
                _inHands = null;
            }
                
        }


        //Бросок гранаты
        if (Input.GetKeyUp(KeyCode.G))
        {
            if (_inHands != null) {
                {
                    if (_inHands.tag == "Granade")
                    {

                        Granade granade = _inHands.GetComponent<Granade>();
                        granade.Granade_Status = true;

                        GameObject ObjectDrop = Instantiate(_inHands, _targetHand);
                        ObjectDrop.transform.parent = null;
                        Destroy(_inHands);
                        _inHands = null;

                    }

                }
            }

        }
        //Инвентарь
        if (Input.GetKeyUp(KeyCode.I))
        {
            _inventoryController._itemContainer = null;

            if (_canvas.activeInHierarchy == false)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.lockState = CursorLockMode.Confined;
                Cursor.visible = true;
                _canvas.SetActive(true);
            }
            else
            {
                _canvas.SetActive(false);
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

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
            //_playerInventory = GetComponent<PlayerInventory>();

           
            //else if (_inHands != null && hit.collider.tag == "Item" || hit.collider.tag == "Weapon")
            //{

            //    //_playerInventory.TakeItem(hit);
            //}
            //else if (_playerInventory._backPack == null && hit.collider.tag == "ItemClothing")
            //    _playerInventory._backPack = hit.collider.gameObject;
            if (hit.collider.gameObject != null)
            {
                if (_inHands == null && hit.collider.tag == "Item" || hit.collider.tag == "Weapon" || hit.collider.tag == "Granade")
                {
                    _inHands = hit.collider.gameObject;
                }

                else if (_inHands == null && (hit.collider.tag == "Item" || hit.collider.tag == "Weapon"))
                {
                    Item item = hit.collider.GetComponent<Item>();
                    _inventoryController._item = item._itemPrefabIcon;
                    Destroy(hit.collider.gameObject);
                }
                //else if (_inHands == null && (hit.collider.tag == "Item" || hit.collider.tag == "Weapon"))
                //{
                //    _inHands = hit.collider.gameObject;
                //}
            }


        }
        // Перезарядка
        //else if (Input.GetKeyUp(KeyCode.R) && _inHands.tag == "Weapon")
        //{
        //    _playerInventory = GetComponent<PlayerInventory>();
        //    _gun = _inHands.GetComponent<Gun>();
        //    if (_playerInventory._backPack != null)
        //    {
        //        _inventoryObject = _playerInventory._backPack.GetComponent<InventoryObject>();
        //        for (int i = _inventoryObject._item.Count - 1; i > 0; i--)
        //        {
        //            Item item = _gun._idMagazinType.GetComponent<Item>();
        //            if (_inventoryObject._item[i] == item._id)
        //            {
        //                for (int b = item._slot; b>0;b--)
        //                {                            
        //                    Debug.Log(b);
        //                    _inventoryObject._item.Remove(_inventoryObject._item[i]);
        //                    i--;
        //                }
        //                break;
        //            }

        //        }

        //    }

        //}
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
