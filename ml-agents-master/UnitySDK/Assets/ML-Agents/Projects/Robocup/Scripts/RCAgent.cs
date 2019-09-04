using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;

public class RCAgent : Agent {

    AgentController agentController;
    AgentUtils utils;
    AgentConfig config;

    Transform ball;

    Vector3 targetPos;

    float nextReward = 0;

    bool lastGoalWasUs = false;

    float numDataPoints = 0;
    List<float> metricData = new List<float>();

    Vector3 sideVector; 

    bool ballOut;

    float circumference;

    void Start () {   
        agentController = GetComponent<AgentController>();
        utils = GetComponent<AgentUtils>();
        config = agentController.config;

        sideVector = utils.sideVector;
        ball = GameObject.Find("Ball").transform;
        agentController.otherGoal.GetComponent<Goal>().scoreEvent.AddListener(AddPositiveReward);
        agentController.goal.GetComponent<Goal>().scoreEvent.AddListener(AddNegativeReward);
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
        bool optimalBallCharger = transform == utils.GetNearestTeammateToPoint(ball.position);
     
        if (Vector3.Distance(transform.position, ball.position) < config.kickableRange || optimalBallCharger)
        {
            utils.MoveToAndKickBall();
        }
        else 
        {
            utils.PositionToReceiveBall(vectorAction);
        }

        bool goalScored = (nextReward != 0);

        SetReward(nextReward);
        nextReward = 0;

        if (ballOut || goalScored) {
            AgentReset();
        }


    }

    public override void AgentReset() {
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


