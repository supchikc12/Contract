using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class EnemyLogic : MonoBehaviour
{
    private NavMeshAgent Agent;//собака
    private Characteristic _characteristicThis, _characteristicTarget;
    public GameObject Target; //Цель
    private void Awake()
    {
        Agent = GetComponent<NavMeshAgent>();
        _characteristicThis = gameObject.GetComponent<Characteristic>();
    }
    private void Update()
    {
        _characteristicThis._time -= Time.deltaTime;
        if (Target != null)
        {
            Agent.destination = Target.transform.position;            
            if (_characteristicThis._distanceAtack > Vector3.Distance(Target.transform.position, transform.position) && _characteristicThis._time <= 0 )
            {
                Debug.Log(Vector3.Distance(Target.transform.position, transform.position));

                _characteristicTarget.TakeDamage(_characteristicThis._damage);
                _characteristicThis._time = _characteristicThis._stTime;
            }
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Player")
        {
            Target = other.gameObject;
            _characteristicTarget = Target.GetComponent<Characteristic>();
        }
            
    }
}
