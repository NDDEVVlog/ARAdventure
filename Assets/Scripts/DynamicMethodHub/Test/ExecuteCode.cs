using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem; 

public class ExecuteCode : MonoBehaviour
{
    public UnityEvent executeEvent;
    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            executeEvent?.Invoke();
        }
    }
}
