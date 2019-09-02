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

    [Header("Walk Config")]
    public float walkSpeed;
    public float turnRadius;
    [HideInInspector]
    public float turnTime {
        get {
            return (turnRadius*2f*Mathf.PI) / walkSpeed;
        }
    }
}
