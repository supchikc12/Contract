using UnityEngine;

public class Granade : MonoBehaviour
{
    private float Power = 4f;
    public bool Granade_Status = false;
    private float timer = 7f;


    private void Awake()
    {
        //////////// ƒоделать узвание направление дл€ броска гранаты 

    }
    void Update()
    {
        if (Granade_Status == true)
        {

        if (timer > 0)
        {
      
            Power -= 0.5f * Time.deltaTime;
            transform.position += transform.forward / Power;
            timer -= Time.deltaTime;
        }
    }

    }
}
