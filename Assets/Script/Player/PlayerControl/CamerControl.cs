using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamerControl : MonoBehaviour
{
    public float sensitivity = 2.0f; // ���������������� ����
    public float maxYAngle = 80.0f; // ������������ ���� �������� �� ���������

    private float rotationX = 0.0f;
    public GameObject body;

    public PlayerController playerController;

    private void Start()
    {
        playerController =GameObject.Find("Player").GetComponent<PlayerController>();
    }

    private void Update()
    {
        
        // �������� ���� �� ����
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        // ������� ��������� � �������������� ���������
        transform.parent.Rotate(Vector3.up * mouseX * sensitivity);

        // ������� ������ � ������������ ���������
        rotationX -= mouseY * sensitivity;
        rotationX = Mathf.Clamp(rotationX, -maxYAngle, maxYAngle);
        transform.localRotation = Quaternion.Euler(rotationX, 0.0f, 0.0f);

        body.transform.rotation = transform.rotation;

        if (playerController._inHands != null)
        {
            playerController._inHands.transform.rotation = transform.rotation;
        }
    }
}
