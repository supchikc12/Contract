using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyLogic_Human : MonoBehaviour
{
    public Transform targetHand;
    private NavMeshAgent Agent;
    public GameObject _inHands;
    private Characteristic _characteristicThis, _characteristicTarget;
    public GameObject Target; //����
    public List<GameObject> TradeList = new List<GameObject> (); // ���� ��������� ��� �������
    public List<GameObject> ItemInTrigger = new List<GameObject>(); // ���� ��������� ��������
    private void Awake()
    { 
        Agent = GetComponent<NavMeshAgent>();
        _characteristicThis = gameObject.GetComponent<Characteristic>();
    }
    private void Update()
    {
        if (_inHands == null)
            SearchItem("Weapon");
        else
            _inHands.transform.position = targetHand.position;

        if (_characteristicThis._hunger < 20)
        {
            //_inHands = null;
            SearchItem("Eat");

            Use(_inHands);

        }

        if (Target != null)
        {
            Agent.destination = Target.transform.position;

            if (Vector3.Distance(gameObject.transform.position, Target.transform.position) <= 5)
            {
                Debug.Log(Vector3.Distance(gameObject.transform.position, Target.transform.position), Target);
                if(Target.tag == "Weapon" || Target.tag == "Item" || Target.tag == "Eat")
                    TakeItem(Target);
                
                Target = null;
                //Destroy(Target);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        //if (other.tag == "Player")
        //{
        //    Target = other.gameObject;
        //    _characteristicTarget = Target.GetComponent<Characteristic>();
        //}
        //else if (other.tag == "Weapon")
        //    ItemInTrigger.Add(other.gameObject);

        if (other.tag == "Weapon")
            ItemInTrigger.Add(other.gameObject);
    }


    private void OnTriggerExit(Collider other)
    {
        ItemInTrigger.Remove(other.gameObject);
    }


    //������ �������
    //Item - ������� ������� �������
    public void TakeItem(GameObject Item) 
    {
        if (Item.tag == "Weapon")
        {
            if (_inHands == null)
                _inHands = Item;
        }
            
    }

    //������������� ������� � �����
    //Item - ������� ������� ������������
    public void Use(GameObject Item) //������������� �������
    {
        if (_inHands == Item)
        {
            Item item = _inHands.GetComponent<Item>();
            item.Use(gameObject);
        }
        else
        {
            //����� � ����
        }

        //Gun Using = _inHands.GetComponent<Gun>(); // �� �� ��������
    }

    //����� ������� �������� �� ����
    //Item - ��� �������� ������� ����
    public void SearchItem(string Item) 
    {
        if (ItemInTrigger.Count > 1)
        {
            for (int i = ItemInTrigger.Count - 1; i >= 0; i--)
            {
                float Distance = Vector3.Distance(ItemInTrigger[i].transform.position, transform.position);
                float DistanceNext = Vector3.Distance(ItemInTrigger[i - 1].transform.position, transform.position);
                if (Distance < DistanceNext)
                    Target = ItemInTrigger[i];
                else
                    Target = ItemInTrigger[i - 1];
            }
        }
        else if (ItemInTrigger.Count == 1)
        {
            Target = ItemInTrigger[ItemInTrigger.Count-1];
        }
    }


    //������� ��� ������� ���������
    //TradeList - ���� ��������� �� �������
    public void Trade(List<GameObject> TradeList)
    { 
        
    }

}
