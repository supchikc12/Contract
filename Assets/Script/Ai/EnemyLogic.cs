using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class EnemyLogic : MonoBehaviour
{
    private NavMeshAgent Agent;//������
    private PlayeCharacteristic _playeCharacteristic;
    public GameObject Target; //����
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

        //if (��������� ����� ����� � ���� ������ ��� �������� �����)
        //    �������
    }
    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Player")
        {
            Target = other.gameObject;
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


}
