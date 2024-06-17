using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5.0f; // �������� �������� ���������

    private CharacterController controller;
    private Camera main_camera;
    Vector3 Ray_start_position = new Vector3(Screen.width / 2, Screen.height / 2, 0);
    public GameObject _inHands;
    public Transform tragetHand;
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
        // �������� ���� �� ������
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        // ��������� ����������� ��������
        Vector3 moveDirection = transform.forward * verticalInput + transform.right * horizontalInput;

        // ��������� ����������
        moveDirection.y -= 9.81f * Time.deltaTime;

        // ������� ���������
        controller.Move(moveDirection * moveSpeed * Time.deltaTime);

        // ���������
        //if (Input.GetKey(KeyCode.I))
        //{

        //}

        // ������ ���������
        if (Input.GetKey(KeyCode.F))
        {
            // ��� ���
            Ray ray = main_camera.ScreenPointToRay(Ray_start_position);
            // ������ �������, � ������� ������ ���, � ����������
            RaycastHit hit;
            Physics.Raycast(ray, out hit);
            //������ ��� ����������� ������ ��� � ���� Scene
            Debug.DrawLine(ray.origin, hit.point, Color.red);

            if (hit.collider.tag == "Item" || hit.collider.tag == "Weapon")
            {
                _inHands = hit.collider.gameObject;
            }   
        }
        if (_inHands != null)
        {
            _inHands.transform.position = tragetHand.position;
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
