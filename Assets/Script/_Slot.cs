using UnityEngine;
using UnityEngine.UI;

public class _Slot : MonoBehaviour
{
    public GameObject _obj_this;
    public Image _sprite_this;
    private Image _defoult_image;

    private void Awake()
    {
        _defoult_image = transform.Find("Image").gameObject.GetComponent<Image>();
        _sprite_this = transform.Find("Image").gameObject.GetComponent<Image>();
    }

    public void ChangeInSlot(GameObject _obj)
    {
        _obj_this = _obj;
        Sprite _sprite_obj = _obj.GetComponent<Image>().sprite;
        _sprite_this.sprite = _sprite_obj;
        //_sprite_this.activeSprite = _sprite_obj.activeSprite;
        //_sprite_slot  = _sprite_obj;
    }

    public void OnTriggerExit2D(Collider2D collision)
    {
        _obj_this = null;
        _sprite_this = _defoult_image;
        //_defoult_image;

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
