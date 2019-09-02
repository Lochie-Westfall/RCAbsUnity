using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentController : MonoBehaviour
{
    public AgentConfig config;

    // State variables

    float timeLastCollided;
    float timeInBallRange;
    float timeLastKicked;

    // Methods

    public bool isStanding {
        get { 
            return Time.time - timeLastCollided > config.fallRecoveryTime;
        }
    }

    public bool canKick {
        get {
            bool hasKickDelayExpired = timeInBallRange > config.timeToKick;
            bool isWithinKickRange = config.kickableRange < Vector3.Distance(transform.position, GameObject.Find("ball").transform.position);
            return timeInBallRange > config.timeToKick;
        }
    }

    bool KickBallAtAngle (float angle) {
        if (isStanding && canKick) {
            Vector3 kickVector = Quaternion.AngleAxis(angle, Vector3.up) * transform.forward;
            kickVector.Normalize();
            GameObject.Find("ball").GetComponent<Rigidbody>().AddForce(kickVector * config.kickPower);
            timeLastKicked = Time.time;
            timeInBallRange = 0f;

            return true;
        }
        else {
            return false;
        }
    }

    bool Walk (float direction) {
        if (isStanding) {
            GetComponent<Rigidbody>().angularVelocity = (new Vector3(0f, Time.deltaTime * 360f / config.turnTime * direction, 0f));
            GetComponent<Rigidbody>().velocity = transform.forward * config.walkSpeed;

            return true;
        } 
        else {
            return false;
        }

    }

    private void OnTriggerEnter(Collider other) {
        if (config.isFallingEnabled && (other.gameObject.tag == "left" || other.gameObject.tag == "right")) {
            timeLastCollided = Time.time;
            Vector3 collisionReactionVector = transform.position - other.transform.position;
            collisionReactionVector.y = 0;
            transform.position += (collisionReactionVector).normalized * 0.125f;
            timeInBallRange = -config.fallRecoveryTime;
        }
    }
}
