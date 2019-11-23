using UnityEngine;

[CreateAssetMenu(fileName = "AgentConfig", menuName = "ScriptableObjects/AgentConfig", order = 1)]
public class AgentConfig : ScriptableObject
{
    [Header("Falling Config")]
    public bool isFallingEnabled;
    public float fallRecoveryTime;

    [Header("Kick Config")]
    public float timeToKick;
    public float kickPower;
    public float kickableRange;

    // TODO:: rename these variables
    public float kickSegmentAngle;
    public float facingAngleRange;

    [Header("Walk Config")]
    public float walkSpeed;
    public float turnRadius;
    public float decisionRadius;
    public int decisionInterval;
    [HideInInspector]
    public float turnTime {
        get {
            return (turnRadius*2f*Mathf.PI) / walkSpeed;
        }
    }
}
