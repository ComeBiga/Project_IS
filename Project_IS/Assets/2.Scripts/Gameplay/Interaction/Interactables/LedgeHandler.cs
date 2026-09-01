using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LedgeHandler : MonoBehaviour
{
    [SerializeField]
    private GameObject _goLedgePointL;
    [SerializeField]
    private GameObject _goLedgePointR;

    [Header("Debug")]
    [SerializeField]
    private bool _drawPoint = true;

    public Vector3? GetNearestLedgePoint(Vector3 fromPosition)
    {
        if(_goLedgePointL == null && _goLedgePointR == null)
        {
            return null;
        }

        if(_goLedgePointL != null && _goLedgePointR == null)
        {
            return _goLedgePointL.transform.position;
        }

        if(_goLedgePointR != null && _goLedgePointL == null)
        {
            return _goLedgePointR.transform.position;
        }

        float distanceToL = Vector3.Distance(fromPosition, _goLedgePointL.transform.position);
        float distanceToR = Vector3.Distance(fromPosition, _goLedgePointR.transform.position);

        if(distanceToL <= distanceToR)
        {
            return _goLedgePointL.transform.position;
        }
        else
        {
            return _goLedgePointR.transform.position;
        }
    }

    public Vector3? GetLedgePointLOrNull()
    {
        if(_goLedgePointL != null)
        {
            return _goLedgePointL.transform.position;
        }
        return null;
    }

    public Vector3? GetLedgePointROrNull()
    {
        if(_goLedgePointR != null)
        {
            return _goLedgePointR.transform.position;
        }
        return null;
    }

    private void OnDrawGizmosSelected()
    {
        if(_drawPoint && _goLedgePointL != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_goLedgePointL.transform.position, .2f);
        }

        if(_drawPoint && _goLedgePointR != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_goLedgePointR.transform.position, .2f);
        }
    }
}
