using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : MonoBehaviour
{
    private AmmunitionMagazine _ammo;
    public GameObject magazin;
    public Transform _magazin;
    public GameObject _fire;
    public GameObject _idMagazinType;
    public float _stTime;
    public float time = 0;

    private void Update()
    {
        if (magazin != null)
        {
            magazin.transform.position = _magazin.position;
            magazin.transform.rotation = _magazin.rotation;
        }
        _fire.transform.rotation = transform.rotation;
        if ( time > 0)
            time -= Time.deltaTime;
    }
    public void Fire()
    { 
        _ammo = magazin.GetComponent<AmmunitionMagazine>();
        if (_ammo.Ammunition.Count != 0)
        {
            Instantiate(_ammo.Ammunition[_ammo.Ammunition.Count - 1], _fire.transform);
            _ammo.Ammunition.Remove(_ammo.Ammunition[_ammo.Ammunition.Count - 1]);
            time = _stTime;
        }
    }
}
