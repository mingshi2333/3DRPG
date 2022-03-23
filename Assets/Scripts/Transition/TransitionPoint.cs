using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransitionPoint : MonoBehaviour
{
    public enum TransitionType
    {
        SameScene,DfferentScene
    }
    // Start is called before the first frame update
    [Header("Transition Info")]
    public string sceneName;
    public TransitionType transitionType;
    public TransitionDestination.DestinationTag destinationTag;
    private bool canTrans;
    void OnTriggerStay(Collider other) {
        if(other.CompareTag("Player"))
            canTrans = true;
    }
    void OnTriggerExit(Collider other) {
        if(other.CompareTag("Player"))
            canTrans = false;
    }
}
