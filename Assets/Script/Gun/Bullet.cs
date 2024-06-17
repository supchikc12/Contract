using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    private void Start()
    {
        transform.parent = null;
    }
    void Update()
    {
        transform.Translate(0, 0, 0.5f);
    }
    private void OnTriggerEnter(Collider other)
    {
        Destroy(gameObject);
    }
}
