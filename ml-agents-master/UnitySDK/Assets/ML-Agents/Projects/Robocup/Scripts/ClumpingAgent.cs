using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;

public class ClumpingAgent : Agent
{
    AgentController agentController;
    AgentUtils utils;
    AgentConfig config;

    Transform ball;

    Vector3 targetPos;

    Vector3 sideVector;

    void Start()
    {
        agentController = GetComponent<AgentController>();
        utils = GetComponent<AgentUtils>();
        config = agentController.config;

        ball = GameObject.Find("Ball").transform;
    }

    public override void CollectObservations()
    {
 
    }

    public override void AgentAction(float[] vectorAction, string textAction)
    {
        utils.MoveToAndKickBall();
    }

    public override void AgentReset()
    {

    }

    public override void AgentOnDone()
    {
    }

}
