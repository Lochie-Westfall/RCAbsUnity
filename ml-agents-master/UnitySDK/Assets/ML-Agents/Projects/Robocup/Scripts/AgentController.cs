using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class AgentController : MonoBehaviour
{
    // Configuration

    public AgentConfig config;
    public bool isLeftSide;
    public bool isStriker;

    // Scene objects

    Transform ball;

    Transform leftGoal;
    Transform rightGoal;
    [HideInInspector]
    public Transform otherGoal;
    [HideInInspector]
    public Transform goal;

    // State variables

    [HideInInspector]
    public float timeLastCollided;
    float timeInBallRange;
    float timeLastKicked;
    Vector3 startingPosition;

    bool isBallOut;
    bool wasGoalScored;
    bool lastGoalWasUs;

    [HideInInspector]
    public bool isKicking;


    // Methods

    void Start () {
        // TODO:: find better way to determine the first kickoff
        lastGoalWasUs = isLeftSide;
        ball = GameObject.Find("Ball").transform;
        startingPosition = transform.position;

        leftGoal = GameObject.Find("Goal L").transform;
        rightGoal = GameObject.Find("Goal R").transform;
        otherGoal = (isLeftSide)?rightGoal:leftGoal;
        goal = (isLeftSide)?leftGoal:rightGoal;

        otherGoal.GetComponent<Goal>().scoreEvent.AddListener(GoalScored);
        goal.GetComponent<Goal>().scoreEvent.AddListener(GoalScoredAgainst);

        Reset();
    }

    void FixedUpdate () {
        if (isBallOut && isStriker) {
            WriteOutcome(1);
        }
        if (isBallOut || wasGoalScored) {
            Reset();
        }

        if (Vector3.Distance(transform.position, ball.position) < config.kickableRange) {
            timeInBallRange += Time.deltaTime;
            if (Vector3.Distance(transform.position, ball.position) < config.kickableRange*0.75f) {
                GetComponent<Rigidbody>().isKinematic = true;
            }
            else {
                GetComponent<Rigidbody>().isKinematic = false;
            }
        }
        else {
            timeInBallRange = 0f;
            GetComponent<Rigidbody>().isKinematic = false;
        }

        Vector3 ballPos = ball.position;
        ballPos.y = 0f;
        if (!isStanding || (ballPos.magnitude < 0.001f && (lastGoalWasUs || (!lastGoalWasUs && !isStriker)))) {
            GetComponent<Rigidbody>().isKinematic = true;
        }

        Vector3 eulerAngles = transform.eulerAngles;
        eulerAngles.x = 0f;
        eulerAngles.z = 0f;
        transform.eulerAngles = eulerAngles;

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
            bool isWithinKickRange = config.kickableRange > Vector3.Distance(transform.position, ball.transform.position);
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
        if (isStriker) {
            WriteOutcome(2);
        }
        wasGoalScored = true;
        lastGoalWasUs = true;
    }

    void GoalScoredAgainst () {
        if (isStriker) {
            WriteOutcome(0);
        }
        wasGoalScored = true;
        lastGoalWasUs = false;
    }
    
    public void WriteOutcome (int outcome) {
        string path = "Assets/Resources/outcomes" + (lastGoalWasUs?"_theirs":"_ours") + (isLeftSide?"_left":"_right") + ".txt";
        StreamWriter writer = new StreamWriter(path, true);
        writer.Write(string.Format("{0} ", outcome.ToString()));
        writer.Close();
    }

    public void Reset () {
        timeLastCollided = -9999999999f;
        transform.position = startingPosition;
        // TODO:: move ball resetting to own script
        ball.position = Vector3.up*0.25f;
        ball.GetComponent<Rigidbody>().velocity = Vector3.zero;
        ball.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        GetComponent<Rigidbody>().velocity = Vector3.zero;
        GetComponent<Rigidbody>().angularVelocity = Vector3.zero;

        transform.LookAt(new Vector3(ball.position.x, transform.position.y, ball.position.z));

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
