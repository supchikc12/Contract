using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Characteristic : MonoBehaviour
{
    public string _name;
    public float _health;
    public float _hunger;
    public float _speed;
    public float _distanceAtack;
    public float _damage;
    public float _stTime;
    public float _time;

    public void TakeDamage(float damage)
    {
        _health -= damage;
    }
    private void Update()
    {
        if (_health <= 0)
        {
            Destroy(gameObject);
        }
    }
    
}
