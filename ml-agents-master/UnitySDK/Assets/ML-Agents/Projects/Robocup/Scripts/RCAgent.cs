using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;
using System.IO;

public class RCAgent : Agent {

    AgentController agentController;
    AgentUtils utils;
    AgentConfig config;

    Transform ball;

    float nextReward = 0;

    float numDataPoints = 0;
    List<float> metricData = new List<float>();

    Vector3 sideVector; 

    bool ballOut;

    float circumference;

    int stepsSincePositionWrite = 0;
    int stepsSinceTargetWrite = 0;

    Vector3 currentTargetPosition;

    //[HideInInspector]
    public bool isOptimalBallCharger;
    bool wasOptimalBallCharger;
    bool readyForDecision;

    int framesSinceDecision;
    Vector3 fieldSize;

    int stepsSinceStart = 0;

    Vector3 currentBallRegion;

    void Start () {   
        agentController = GetComponent<AgentController>();
        utils = GetComponent<AgentUtils>();
        config = agentController.config;

        sideVector = utils.sideVector;
        ball = GameObject.Find("Ball").transform;
        agentController.otherGoal.GetComponent<Goal>().scoreEvent.AddListener(AddPositiveReward);
        agentController.goal.GetComponent<Goal>().scoreEvent.AddListener(AddNegativeReward);

        fieldSize = utils.floor.lossyScale;

        currentTargetPosition = Vector3.zero;

        currentBallRegion = ball.position;

        RequestDecision();
    }

    void FixedUpdate () {

        if (agentController.isStriker && Vector3.Distance(ball.position, currentBallRegion) > 0.1f) {
            Transform nearestTeammateToBall = utils.GetNearestTeammateToPoint(ball.position);
            foreach (Transform teammate in utils.teammates) {
                teammate.GetComponent<RCAgent>().isOptimalBallCharger = teammate == nearestTeammateToBall;
            }
            isOptimalBallCharger = transform == utils.GetNearestTeammateToPoint(ball.position);
            currentBallRegion = ball.position;
        }



        StreamWriter writer;
        string path = "";

        if ((Vector3.Distance(transform.position, currentTargetPosition) < config.decisionRadius && !isOptimalBallCharger) || (wasOptimalBallCharger && !isOptimalBallCharger)) {
            readyForDecision = true;
            wasOptimalBallCharger = false;

            if (Application.isEditor && GetComponent<Rigidbody>().isKinematic == false) {
                path = "Assets/Resources/target_data" + (agentController.isLeftSide?"_left":"_right") + ".txt";
                writer = new StreamWriter(path, true);
                writer.WriteLine(string.Format("{0} {1}", currentTargetPosition.x.ToString(), currentTargetPosition.z.ToString()));
                writer.Close();
                
                stepsSinceTargetWrite = 0;
            }
        }

        if (Application.isEditor && GetComponent<Rigidbody>().isKinematic == false && stepsSincePositionWrite > 3) {
            path = "Assets/Resources/position_data" + (agentController.isLeftSide?"_left":"_right") + ".txt";
            writer = new StreamWriter(path, true);
            writer.WriteLine(string.Format("{0} {1}", Mathf.Clamp(transform.position.x,-fieldSize.x,fieldSize.x).ToString(), Mathf.Clamp(transform.position.z, -fieldSize.z, fieldSize.z).ToString()));
            writer.Close();
            stepsSincePositionWrite = 0;
        }

        if (framesSinceDecision > config.decisionInterval && readyForDecision) {
            RequestDecision();
            framesSinceDecision = 0;
            readyForDecision = false;
        }

        Act();

        framesSinceDecision++;
        stepsSinceStart++;

        if (isOptimalBallCharger) wasOptimalBallCharger = true;
    }

    void OnDrawGizmos () {
        Gizmos.DrawSphere(currentTargetPosition, 0.1f);
    }

    public void Act() {
        if (Vector3.Distance(transform.position, ball.position) < config.kickableRange * 1.25f || isOptimalBallCharger)
        {
            utils.MoveToAndKickBall();
        }
        else 
        {
            utils.PositionToReceiveBall(currentTargetPosition.x, currentTargetPosition.z);
        }
        stepsSincePositionWrite++;
        stepsSinceTargetWrite++;
    }

    public override void CollectObservations() {
        AddVectorObs(Vector3.Scale(sideVector, ball.position));
        AddVectorObs(Vector3.Scale(sideVector, ball.GetComponent<Rigidbody>().velocity));
        AddVectorObs(Vector3.Scale(sideVector, transform.position));
        AddVectorObs(Vector3.Scale(sideVector, GetComponent<Rigidbody>().velocity));

        foreach (Transform teammate in utils.teammates) {
            AddVectorObs(Vector3.Scale(sideVector, teammate.position));
            AddVectorObs(Vector3.Scale(sideVector, teammate.GetComponent<Rigidbody>().velocity));
        }

        foreach (Transform opponent in utils.opponents) {
            AddVectorObs(Vector3.Scale(sideVector, opponent.position));
            AddVectorObs(Vector3.Scale(sideVector, opponent.GetComponent<Rigidbody>().velocity));
        }
    }

    public override void AgentAction(float[] vectorAction, string textAction) {
        currentTargetPosition.x = Mathf.Clamp(fieldSize.x/2f*vectorAction[0], -fieldSize.x/2f, fieldSize.x/2f);
        currentTargetPosition.z = Mathf.Clamp(fieldSize.z/2f*vectorAction[1], -fieldSize.z/2f, fieldSize.z/2f);
        if (Mathf.Abs(vectorAction[0]) > 1f || Mathf.Abs(vectorAction[1]) > 1f) {
            print("Target position is considerably off field, check for exploding gradients!");
        }
    }
    public override void AgentReset() {
        RequestDecision();
        numDataPoints = 0;
        metricData.Clear();
    }

    public override void AgentOnDone() {
    }

    void AddPositiveReward () {
        nextReward = 100;
    }

    void AddNegativeReward () {
        nextReward = -100;
    }

    List<float> PerformanceMetrics () {
        float closestOpponentToBallPath = Mathf.Infinity;
        float closestOpponentToGoalPath = Mathf.Infinity;
        float distanceToOpponentGoal = Vector3.Distance(transform.position, agentController.otherGoal.position);
        Transform closestTeammateToBall = utils.GetNearestTeammateToPoint(ball.position);
        float angleToTeammatePassPath = Vector3.Angle(closestTeammateToBall.forward, (transform.position - closestTeammateToBall.position));
        float angleToGoalPath = Vector3.Angle(transform.forward, agentController.otherGoal.position - transform.position);

        foreach (Transform opponent in utils.opponents) {
            float ballAngle = Vector3.Angle(opponent.position - ball.position, transform.position - ball.position);
            float goalAngle = utils.AngleBetweenPoints(agentController.otherGoal.position, opponent.position);

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