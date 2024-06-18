using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class PlayeCharacteristic : MonoBehaviour
{
    public float _playerHealth;

    public void Damage(float damage)
    {
        _playerHealth -= damage;
    }

    public void Update()
    {
        if (_playerHealth <= 0)
            Destroy(gameObject);
    }
}