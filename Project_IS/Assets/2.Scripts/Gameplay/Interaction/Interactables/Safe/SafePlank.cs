using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SafePlank : MonoBehaviour
{
    [SerializeField] private GameObject _goNormal;
    [SerializeField] private GameObject _goBroken;
    [SerializeField] private GameObject _goNormalGround;
    [SerializeField] private GameObject _goBrokenGround;
    [SerializeField] private Transform _trRopeSet;
    [SerializeField] private Transform _trNormalRopePlank;
    [SerializeField] private Transform _trBrokenRopePlank;
    [SerializeField] private ExplosionEffector _explosionEffector;

    public void BreakPlank()
    {
        _goNormal.SetActive(false);
        _goBroken.SetActive(true);
        _goNormalGround.SetActive(false);
        _goBrokenGround.SetActive(true);

        _trRopeSet.SetParent(_trBrokenRopePlank);

        _explosionEffector.Explode();

        AudioManager.instance.PlayOneShot("SafeDemolishBridge");
    }
}
