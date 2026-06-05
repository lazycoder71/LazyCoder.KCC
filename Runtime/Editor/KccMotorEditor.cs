using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace LazyCoder.Kcc.Editor
{
    [CustomEditor(typeof(KccMotor))]
    public class KccMotorEditor : UnityEditor.Editor
    {
        protected virtual void OnSceneGUI()
        {            
            KccMotor motor = (target as KccMotor);
            if (motor)
            {
                Vector3 characterBottom = motor.transform.position + (motor.Capsule.center + (-Vector3.up * (motor.Capsule.height * 0.5f)));

                Handles.color = Color.yellow;
                Handles.CircleHandleCap(
                    0, 
                    characterBottom + (motor.transform.up * motor.MaxStepHeight), 
                    Quaternion.LookRotation(motor.transform.up, motor.transform.forward), 
                    motor.Capsule.radius + 0.1f, 
                    EventType.Repaint);
            }
        }
    }
}