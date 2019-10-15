using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Goal : MonoBehaviour
{

    public UnityEvent scoreEvent;
    
    void OnTriggerEnter(Collider other) 
    {
        if (other.gameObject.tag == "ball") {
            scoreEvent.Invoke();
            print(gameObject.name);    
        }
    }


}
