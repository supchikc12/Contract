using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class EnemyLogic : MonoBehaviour
{
    private NavMeshAgent Agent;//собака
<<<<<<< HEAD
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
=======
    private PlayeCharacteristic _playeCharacteristic;
    public GameObject Target; //Цель
    public float _damage;
    public float _distanceAtack;
    public int MaxHealth = 100;
    public float _stTime;
    public float _time;
    private void Awake()
    {
        Agent = GetComponent<NavMeshAgent>();
    }
    private void Update()
    {
        _time -= Time.deltaTime;
        if (Target != null)
        {
            Agent.destination = Target.transform.position;            
            if (_distanceAtack > Vector3.Distance(Target.transform.position, transform.position) && _time <= 0 )
            {
                Debug.Log(Vector3.Distance(Target.transform.position, transform.position));
                
                _playeCharacteristic.Damage(_damage);
                _time = _stTime;
            }
        }

        //if (дистанция между целью и мной меньше чем дистация атаки)
        //    ударить
>>>>>>> origin/main
    }
    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Player")
        {
            Target = other.gameObject;
<<<<<<< HEAD
            _characteristicTarget = Target.GetComponent<Characteristic>();
        }
            
    }
=======
            _playeCharacteristic = Target.GetComponent<PlayeCharacteristic>();
        }
            
    }

    public void Attack()
    {

    }


    //private void OnTriggerEnter(Collider other)
    //{
    //    if (other.GetComponent<PlayerController>())
    //    {
    //        other.GetComponent<PlayerController>().TakeDamage(Damage);

    //    }
    //}
    //private void Start()
    //{
    //    CurrentHealth = MaxHealth;
    //}
    //public void TakeDamage(int damage)
    //{
    //    CurrentHealth = damage;
    //    if (CurrentHealth <= 0)
    //    {
    //        Destroy(gameObject);
    //    }
    //}


>>>>>>> origin/main
}
