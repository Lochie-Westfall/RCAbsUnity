using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;

public class RCAgent : Agent {

    public Transform ball;
    public List<Transform> opponents;
    public List<Transform> teammates;
    public bool leftSide;

    Transform leftGoal;
    Transform rightGoal;
    Transform otherGoal;
    Transform goal;

    Vector3 targetPos;

    public float kickCooldown = 0.1f;

    float nextReward = 0;
    float timeLastKicked = 0;

    public float turnTime = 1f;
    public float turnRadius = 1f;
    public float straightRunMultiplier = 1.5f;


    public float kickSegmentAngle = 40f;
    public float kickDecisionAngle = 90f;
    public float kickableDistance = 0.3f;
    public float kickPower = 200f;
    public float movingAngleRange = 10f;
    public float accelerationSpeed = 1f;

    float currentSpeed = 0f;
    float timeInBallRange = 0f;

    float numDataPoints = 0;
    List<float> metricData = new List<float>();

    Vector3 sideVector;

    public enum BehaviourSystem {trained, random, clump};

    public BehaviourSystem behaviourSystem = BehaviourSystem.trained;

    bool ballOut;
    void Start () 
    {   
        sideVector = (leftSide)?new Vector3(1f, 1f, 1f):new Vector3(-1f, 1f, -1f);

        teammates = new List<Transform>();
        opponents = new List<Transform>();
        foreach (GameObject obj in GameObject.FindGameObjectsWithTag("left")) 
        {
            if (obj != gameObject) 
            {
                if (leftSide) 
                {
                    teammates.Add(obj.transform);
                }
                else 
                {
                    opponents.Add(obj.transform);
                }
            }
        }
        foreach (GameObject obj in GameObject.FindGameObjectsWithTag("right")) 
        {
            if (obj != gameObject) 
            {
                if (!leftSide) 
                {
                    teammates.Add(obj.transform);
                }
                else 
                {
                    opponents.Add(obj.transform);
                }
            }
        }

        leftGoal = GameObject.Find("Goal L").transform;
        rightGoal = GameObject.Find("Goal R").transform;
        otherGoal = (leftSide)?rightGoal:leftGoal;
        goal = (leftSide)?leftGoal:rightGoal;

        otherGoal.GetComponent<Goal>().scoreEvent.AddListener(AddPositiveReward);
        goal.GetComponent<Goal>().scoreEvent.AddListener(AddNegativeReward);

    }

    public override void CollectObservations()
    {
        AddVectorObs(Vector3.Scale(sideVector, ball.position));
        AddVectorObs(Vector3.Scale(sideVector, ball.GetComponent<Rigidbody>().velocity));
        AddVectorObs(Vector3.Scale(sideVector, transform.position));
        AddVectorObs(Vector3.Scale(sideVector, GetComponent<Rigidbody>().velocity));

        foreach (Transform teammate in teammates) {
            AddVectorObs(Vector3.Scale(sideVector, teammate.position));
            AddVectorObs(Vector3.Scale(sideVector, teammate.GetComponent<Rigidbody>().velocity));
        }

        foreach (Transform opponent in opponents) {
            AddVectorObs(Vector3.Scale(sideVector, opponent.position));
            AddVectorObs(Vector3.Scale(sideVector, opponent.GetComponent<Rigidbody>().velocity));
        }

    }

    public override void AgentAction(float[] vectorAction, string textAction)
    {
        bool goalScored = (nextReward != 0);
        if (Vector3.Distance(transform.position, ball.position) < kickableDistance * 1.5f) {
            timeInBallRange += Time.deltaTime;
        }
        else {
            timeInBallRange = 0f;
        }

        // TODO:: consider if you should kick by checking angle from each player to the goal rather than distance to the ball
        switch (behaviourSystem)
        {
            case (BehaviourSystem.trained):
                if (Vector3.Distance(ball.position, transform.position) < Vector3.Distance(ball.position, GetNearestTeammateToPoint(ball.position).position))
                {
                    MoveToAndKickBall();
                }
                else 
                {
                    PositionToReceiveBall(vectorAction);
                }
                break;
            case (BehaviourSystem.random):
                if (Vector3.Distance(ball.position, transform.position) < Vector3.Distance(ball.position, GetNearestTeammateToPoint(ball.position).position))
                {
                    MoveToAndKickBall();
                }
                else 
                {
                    PositionToReceiveBallRandom();
                }
                break;
            case (BehaviourSystem.clump):
                MoveToAndKickBall();
                break;
        }
        

        if (nextReward != 0 || ball.position.y <= 0f) 
        {
            nextReward = 0;
        }

        if (ballOut || goalScored) {
            AgentReset();
        }

        ballOut = ball.position.y <= 0f;
    }

    public override void AgentReset()
    {

        if (gameObject.name.Contains("1")) 
        {
            transform.position = Vector3.forward * ((leftSide)?1f:-1f);
        }
        else 
        {
            transform.position = Vector3.right * Random.Range(-2f, 2f) + Vector3.forward * Random.Range(0f, 4f) * ((leftSide)?1:-1) + Vector3.up*0;
        }
        ball.position = Vector3.up*0.25f;
        ball.GetComponent<Rigidbody>().velocity = Vector3.zero;

        transform.LookAt(new Vector3(ball.position.x, 0f, ball.position.z));

        for (int i = 0; i < metricData.Count; i++)
        {
            //print(metricData[i]/numDataPoints);
        }
        ballOut = false;

        numDataPoints = 0;
        metricData.Clear();
    }

    public override void AgentOnDone()
    {
    }
    
    Vector3 tangentPos;

    float PathToPointLength (Transform from, Vector3 to) 
    {
        float dir = Mathf.Sign(Vector3.Dot(from.right, to - from.position));

        Vector3 circleCentre = from.position+from.right*turnRadius*dir;
        float tangentAngle = Mathf.Asin(turnRadius/Vector3.Distance(to, circleCentre));
        float distance = Mathf.Sqrt(turnRadius*turnRadius + Mathf.Pow(Vector3.Distance(to, circleCentre),2f));
        Vector3 circleBallDelta = (circleCentre - to).normalized;
        tangentPos = ((Quaternion.Euler(0f, tangentAngle * Mathf.Rad2Deg + 0.000001f, 0f) * circleBallDelta) + to) * distance;

        float pathSectorLength = turnRadius * Mathf.Deg2Rad * Vector3.Angle(from.position - circleCentre, tangentPos - circleCentre);

        float totalDistance = pathSectorLength + Vector3.Distance(tangentPos, to);

        return totalDistance;
    }

    Transform GetNearestTeammateToPoint (Vector3 point) 
    {
        Transform closestTeammate = transform;
        float closestTeammateDist = 99999999999999f;
        foreach (Transform teammate in teammates) 
        {
            float dist = PathToPointLength(teammate, point);
            //float dist = Vector3.Distance(point, teammate.position); 
            if (dist < closestTeammateDist) 
            {
                closestTeammateDist = dist;
                closestTeammate = teammate;
            }
        }

        return closestTeammate;
    }
    
    void MoveToAndKickBall () 
    {
        if (ShouldKick()) {
            if (Vector3.Distance(transform.position, ball.position) < kickableDistance / 1.25f)
            {
                currentSpeed = 0f;
            }
            if (Vector3.Distance(transform.position, ball.position) < kickableDistance && timeInBallRange > kickCooldown)
            {
                KickBallAngled(GetKickDirection());
            }
            else
            {
                MoveToPoint(ball.position);
            }
        }
        else
        {
            MoveToPoint(ball.position + (ball.position - otherGoal.position).normalized * turnRadius);
        }
    }

    void PositionToReceiveBall (float[] vectorAction) 
    {
        MoveToPoint(new Vector3(vectorAction[0] * sideVector[0], 0, vectorAction[1] * sideVector[2]) * 10f);

        List<float> metrics = PerformanceMetrics();

        if (metricData.Count == 0) {
            metricData = metrics;
        }
        else {
            for (int i = 0; i < metrics.Count; i++)
            {
                metricData[i] += metrics[i]; 
                //nextReward += metrics[i];
            } 
        }
        numDataPoints++;

        SetReward(nextReward);
    }

    
    void PositionToReceiveBallRandom () 
    {
        MoveToPoint(new Vector3(Random.Range(-3f,3f) * sideVector[0], 0,  Random.Range(-4.5f,4.5f) * sideVector[2]));
    

        List<float> metrics = PerformanceMetrics();

        if (metricData.Count == 0) {
            metricData = metrics;
        }
        else {
            for (int i = 0; i < metrics.Count; i++)
            {
                metricData[i] += metrics[i]; 
                //nextReward += metrics[i];
            } 
        }
        numDataPoints++;

        SetReward(nextReward);
    }

    bool ShouldKick () {
        bool lookingAtTeammate = false;
        foreach (Transform teammate in teammates) {
            if (AngleToPoint(teammate.position) < kickDecisionAngle) {
                lookingAtTeammate = true;
            }
        }
        foreach (Transform opponent in opponents) {
            if (AngleToPoint(opponent.position) < kickDecisionAngle) {
                lookingAtTeammate = false;
            }
        }

        return (AngleToPoint(otherGoal.position) < kickDecisionAngle || lookingAtTeammate);
    }

    float GetKickDirection () {
        int leftSegmentScore = kickSegmentScore(kickSegmentAngle*-1.5f, kickSegmentAngle*-0.5f);
        int middleSegmentScore = kickSegmentScore(kickSegmentAngle*-0.5f, kickSegmentAngle*0.5f);
        int rightSegmentScore = kickSegmentScore(kickSegmentAngle*0.5f, kickSegmentAngle*1.5f);

        if (rightSegmentScore > middleSegmentScore && rightSegmentScore > leftSegmentScore) 
        {
            return kickSegmentAngle;
        }
        else if (leftSegmentScore > middleSegmentScore && leftSegmentScore > rightSegmentScore) 
        {
            return -kickSegmentAngle;
        }
        else 
        {
            if (middleSegmentScore == 0) {
                return Mathf.Clamp(SignedAngleToPoint(otherGoal.position), -kickSegmentAngle, kickSegmentAngle);
            }
            else {
                return 0f;
            }
        }
    }

    int kickSegmentScore (float leftAngle, float rightAngle) {

        int score = 0;

        foreach (Transform teammate in teammates) {
            if (leftAngle <= SignedAngleToPoint(teammate.position) && SignedAngleToPoint(teammate.position) <= rightAngle) {
                score += 2;
            }
        }
        foreach (Transform opponent in opponents) {
            if (leftAngle <= SignedAngleToPoint(opponent.position) && SignedAngleToPoint(opponent.position) <= rightAngle) {
                score += -1;
            }
        } 
        
        if (leftAngle <= SignedAngleToPoint(goal.position) && SignedAngleToPoint(goal.position) <= rightAngle) {
            score += -5;
        }

        if (leftAngle <= SignedAngleToPoint(otherGoal.position) && SignedAngleToPoint(otherGoal.position) <= rightAngle) {
            score += 5;
        }

//        print(SignedAngleToPoint(otherGoal.position));

        return score;
    }

    void KickBallPoint(Vector3 point) 
    {
        ball.GetComponent<Rigidbody>().AddForce((point - (ball.position)).normalized * kickPower);
        currentSpeed = 0f;
        timeLastKicked = Time.time;
        timeInBallRange = 0f;
    }

    void KickBallForward(){
        ball.GetComponent<Rigidbody>().AddForce(transform.forward * kickPower);
        currentSpeed = 0f;
        timeLastKicked = Time.time;
        timeInBallRange = 0f;
    }
    
    void KickBallAngled (float angle) {
//        Vector3 point = Mathf.Cos(Mathf.Deg2Rad*angle/360f) * transform.forward + Mathf.Sin(Mathf.Deg2Rad*angle/360f) * transform.right;
        Vector3 point = Quaternion.AngleAxis(angle, Vector3.up) * transform.forward;

        point.Normalize();
        ball.GetComponent<Rigidbody>().AddForce(point * kickPower);
        currentSpeed = 0f;
        timeLastKicked = Time.time;
        timeInBallRange = 0f;
    }

    void MoveToPoint(Vector3 point)
    {
        Vector3 diff = point - transform.position;
        diff.y = 0f;

        float dir = Mathf.Sign(Vector3.Dot(transform.right, diff));

        if (AngleBetweenPoints(transform.position + transform.forward, point) < movingAngleRange) 
        {
            Turn(0f, turnTime, turnRadius * straightRunMultiplier);
        }
        else 
        {
            if (Vector3.Distance(transform.position + transform.right*turnRadius*dir, point) > turnRadius) 
            {
                Turn(dir, turnTime, turnRadius);
            }
            else
            {
                Turn(0f, turnTime, turnRadius * straightRunMultiplier);
            }
        }
    }

    void Turn(float direction, float timeToRotate, float radius) {
        transform.Rotate(new Vector3(0f, Time.deltaTime * 360f / timeToRotate * direction * currentSpeed, 0f));
        GetComponent<Rigidbody>().velocity = transform.forward * radius / timeToRotate * 2f * Mathf.PI * currentSpeed;
        currentSpeed = Mathf.Clamp01(currentSpeed + Time.deltaTime * accelerationSpeed);
    }

    float AngleBetweenPoints (Vector3 a, Vector3 b) 
    {
        return Vector3.Angle(b - transform.position, a - transform.position);
    }

    float AngleToPoint (Vector3 point) {
        return Vector3.Angle(transform.forward, point - transform.position);
    }
    
    float SignedAngleToPoint (Vector3 point) {
        return Vector3.SignedAngle(transform.forward, point - transform.position, Vector3.up);
    }

    void AddPositiveReward ()
     {
        nextReward = 100;
        if (gameObject.name.Contains("1")) {
            print((leftSide)?"Left Scored":"Right Scored");
        }
    }

    void AddNegativeReward () 
    {
        nextReward = -100;
    }

    List<float> PerformanceMetrics () {
        float closestOpponentToBallPath = Mathf.Infinity;
        float closestOpponentToGoalPath = Mathf.Infinity;
        float distanceToOpponentGoal = Vector3.Distance(transform.position, otherGoal.position);
        Transform closestTeammateToBall = GetNearestTeammateToPoint(ball.position);
        float angleToTeammatePassPath = Vector3.Angle(closestTeammateToBall.forward, (transform.position - closestTeammateToBall.position));
        float angleToGoalPath = Vector3.Angle(transform.forward, otherGoal.position - transform.position);

        foreach (Transform opponent in opponents) {
            float ballAngle = Vector3.Angle(opponent.position - ball.position, transform.position - ball.position);
            float goalAngle = AngleBetweenPoints(otherGoal.position, opponent.position);

            closestOpponentToBallPath = (ballAngle<closestOpponentToBallPath)?ballAngle:closestOpponentToBallPath;
            closestOpponentToGoalPath = (goalAngle<closestOpponentToGoalPath)?goalAngle:closestOpponentToGoalPath;
        }

        List<float> metrics = new List<float>();
        metrics.Add(Mathf.Clamp01(closestOpponentToBallPath/90f));
        metrics.Add(Mathf.Clamp01(closestOpponentToGoalPath/90f));
        metrics.Add(-Mathf.Clamp01(distanceToOpponentGoal/10.81f));
        metrics.Add(-Mathf.Clamp01(angleToTeammatePassPath/90f));
        metrics.Add(-Mathf.Clamp01(angleToGoalPath/90f));

        return metrics;
    }
}


