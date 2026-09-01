using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SafeTrigger : MonoBehaviour
{
    [SerializeField]
    private SafePlank _safePlank;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Safe"))
        {
            Debug.Log("Player entered the safe trigger!");
            // You can add additional logic here, such as granting invincibility or triggering an event.

            _safePlank.BreakPlank();
        }
    }
}
