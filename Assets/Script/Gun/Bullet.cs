using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed;
    public float damage;
    public float _timeLife;
    private void Start()
    {
        speed = speed / 10;
        transform.parent = null;
        transform.localScale = new Vector3(1,1,1);

    }
    void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;
        _timeLife -= Time.deltaTime;
        if (_timeLife <= 0)
            Destroy(gameObject);
    }
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log(other.name);
        Characteristic _characteristic;
        if ((_characteristic = other.GetComponent<Characteristic>()) != null)
            _characteristic.TakeDamage(damage);
        else
            Debug.Log(other.name);
        Destroy(gameObject);
    }
}
