using UnityEngine;

public class _Slot : MonoBehaviour
{
    public GameObject _this;
    private GameObject _obj_image;

    private void Awake()
    {
        _obj_image = transform.Find("Image").gameObject;
    }

    public void ChangeInSlot(GameObject _obj)
    {
        _this = _obj;
        Sprite _sprite_obj = _obj.GetComponent<Sprite>();
        Sprite _sprite_slot = _obj_image.GetComponent<Sprite>();
        _sprite_slot  = _sprite_obj;
    }
    //private void OnTriggerEnter2D(Collider2D collision)
    //{
    //    _this = collision.gameObject;
    //}
    //private void OnTriggerExit2D(Collider2D collision)
    //{
    //    _this = null;
    //}
}
