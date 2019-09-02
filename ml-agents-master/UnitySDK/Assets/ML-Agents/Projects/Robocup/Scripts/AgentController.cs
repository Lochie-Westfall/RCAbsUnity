using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentController : MonoBehaviour
{
    // Configuration

    public AgentConfig config;
    public bool isLeftSide;

    // Scene objects

    Transform ball;

    Transform leftGoal;
    Transform rightGoal;
    [HideInInspector]
    public Transform otherGoal;
    [HideInInspector]
    public Transform goal;

    // State variables

    float timeLastCollided;
    float timeInBallRange;
    float timeLastKicked;
    Vector3 startingPosition;

    bool isBallOut;
    bool wasGoalScored;


    // Methods

    void Start () {
        ball = GameObject.Find("ball").transform;
        startingPosition = transform.position;

        leftGoal = GameObject.Find("Goal L").transform;
        rightGoal = GameObject.Find("Goal R").transform;
        otherGoal = (isLeftSide)?rightGoal:leftGoal;
        goal = (isLeftSide)?leftGoal:rightGoal;

        otherGoal.GetComponent<Goal>().scoreEvent.AddListener(GoalScored);
        goal.GetComponent<Goal>().scoreEvent.AddListener(GoalScoredAgainst);
    }

    void Update () {
        if (isBallOut || wasGoalScored) {
            Reset();
        }
        isBallOut = ball.position.y < 0f;
    }

    public bool isStanding {
        get { 
            return Time.time - timeLastCollided > config.fallRecoveryTime;
        }
    }

    public bool canKick {
        get {
            bool hasKickDelayExpired = timeInBallRange > config.timeToKick;
            bool isWithinKickRange = config.kickableRange < Vector3.Distance(transform.position, ball.transform.position);
            return hasKickDelayExpired && isWithinKickRange && isStanding;
        }
    }

    public bool KickBallAtAngle (float angle) {
        if (canKick) {
            Vector3 kickVector = Quaternion.AngleAxis(angle, Vector3.up) * transform.forward;
            kickVector.Normalize();
            ball.GetComponent<Rigidbody>().AddForce(kickVector * config.kickPower);
            timeLastKicked = Time.time;
            timeInBallRange = 0f;

            return true;
        }
        else {
            return false;
        }
    }

    public bool Walk (float direction) {
        if (isStanding) {
            GetComponent<Rigidbody>().angularVelocity = (new Vector3(0f, Time.deltaTime * 360f / config.turnTime * direction, 0f));
            GetComponent<Rigidbody>().velocity = transform.forward * config.walkSpeed;

            return true;
        } 
        else {
            return false;
        }

    }

    void GoalScored () {
        wasGoalScored = true;
    }

    void GoalScoredAgainst () {
        wasGoalScored = true;
    }

    public void Reset () {
        timeLastCollided = -9999999999f;
        transform.position = startingPosition;
        ball.position = Vector3.up*0.25f;
        ball.GetComponent<Rigidbody>().velocity = Vector3.zero;
        ball.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        GetComponent<Rigidbody>().velocity = Vector3.zero;
        GetComponent<Rigidbody>().angularVelocity = Vector3.zero;

        transform.LookAt(new Vector3(ball.position.x, 0f, ball.position.z));

        isBallOut = false;
        wasGoalScored = false;
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
