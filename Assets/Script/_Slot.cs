using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class _Slot : MonoBehaviour, IDragHandler//IDragHandler /*IEndDragHandler*/
{

    [SerializeField]
    private string _correct_tag;
    [SerializeField]
    private GameObject currItem;
    private Image _sprite_this;
    private Image _defoult_image;

    private void Awake()
    {
        _defoult_image = transform.Find("Image").gameObject.GetComponent<Image>();
        _sprite_this = transform.Find("Image").gameObject.GetComponent<Image>();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (currItem != null)
        {
            Instantiate(currItem, gameObject.transform.parent);
            Drag drag = currItem.GetComponent<Drag>();
            drag.OnDrag(eventData);
            currItem = null;
            _sprite_this.sprite = _defoult_image.sprite;
        }
        
        //transform.position = Input.mousePosition;

        //inventoryController._itemContainer = transform.gameObject;
    }
    public void ChangeInSlot(GameObject _obj, bool isRight)
    {
        if (_obj.tag == _correct_tag)
        {
            currItem = Instantiate(_obj);
            Sprite _sprite_obj = _obj.GetComponent<Image>().sprite;
            _sprite_this.sprite = _sprite_obj;
            
        }
        else
            isRight = false;

        //_sprite_this.activeSprite = _sprite_obj.activeSprite;
        //_sprite_slot  = _sprite_obj;
    }

    //public void OnTriggerExit2D(Collider2D collision)
    //{
    //    //currItem = null;
       
    //    //_defoult_image;

    //}
    //private void OnTriggerEnter2D(Collider2D collision)
    //{
    //    _this = collision.gameObject;
    //}
    //private void OnTriggerExit2D(Collider2D collision)
    //{
    //    _this = null;
    //}
}
