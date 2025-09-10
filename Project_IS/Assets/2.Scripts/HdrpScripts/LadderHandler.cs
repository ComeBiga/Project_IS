using PropMaker;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

[RequireComponent(typeof(Ladder))]
public class LadderHandler : MonoBehaviour
{
    private Ladder mLadder;

    public PlayerMovement.EDirection GetLadderDirection()
    {
        int result = PlayerMovement.RotationToDirection(transform.rotation);

        Assert.IsTrue(result != 0);

        PlayerMovement.EDirection direction = PlayerMovement.EDirection.Right;

        if (result == 1)
            direction = PlayerMovement.EDirection.Right;
        else if(result == -1)
            direction = PlayerMovement.EDirection.Left;
        else if(result == 2)
            direction = PlayerMovement.EDirection.Forward;

        return direction;
    }

    public List<Vector3> GetStepPositions()
    {
        return mLadder.GetStepPositions();
    }

    // Start is called before the first frame update
    void Start()
    {
        mLadder = GetComponent<Ladder>();
    }

    private void OnDrawGizmosSelected()
    {        
        Handles.color = Color.red;
        Handles.ArrowHandleCap(0, transform.position, Quaternion.identity, 2f, EventType.Repaint);
    }
}
