using PropMaker;
using System.Collections;
using System.Collections.Generic;
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
}
