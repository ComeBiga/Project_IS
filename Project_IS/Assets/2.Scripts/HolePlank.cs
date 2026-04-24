using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class HolePlank : MonoBehaviour
{
    [SerializeField] private GameObject _goNormal;
    [SerializeField] private GameObject _goBroken;
    [SerializeField] private ExplosionEffector _explosionEffector;
    [SerializeField] private VisualEffect _explosionSmokeEffector;
    [SerializeField] private VisualEffect _holeSmokeEffector;
    [SerializeField] private VisualEffect _centerSmokeEffector;
    [SerializeField] private float _demolishDeepSoundDelay = .8f;

    public void BreakPlank()
    {
        _goNormal.SetActive(false);
        _goBroken.SetActive(true);

        // _explosionEffector.Explode();
        _explosionSmokeEffector.gameObject.SetActive(true);
        _holeSmokeEffector.gameObject.SetActive(true);
        _centerSmokeEffector.gameObject.SetActive(true);

        StartCoroutine(eSoundDemolition());
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Safe"))
        {
            other.GetComponent<BoxCollider>().excludeLayers = LayerMask.GetMask("Ground");

            BreakPlank();
        }
    }

    private IEnumerator eSoundDemolition()
    {
        AudioManager.instance.PlayOneShot("SafeDemolish");
        AudioManager.instance.PlayOneShot("SafeDemolish2");

        yield return new WaitForSeconds(_demolishDeepSoundDelay);

        AudioManager.instance.PlayOneShot("SafeDemolishDeep");
    }
}
