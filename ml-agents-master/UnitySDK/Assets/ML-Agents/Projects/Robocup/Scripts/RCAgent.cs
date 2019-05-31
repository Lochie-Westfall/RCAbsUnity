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

    void Start () 
    {   
        teammates = new List<Transform>();
        opponents = new List<Transform>();
        foreach (GameObject obj in GameObject.FindGameObjectsWithTag("left")) {
            if (obj != gameObject) {
                if (leftSide) {
                    teammates.Add(obj.transform);
                }
                else {
                    opponents.Add(obj.transform);
                }
            }
        }
        foreach (GameObject obj in GameObject.FindGameObjectsWithTag("right")) {
            if (obj != gameObject) {
                if (!leftSide) {
                    teammates.Add(obj.transform);
                }
                else {
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
        // Add team input 
        AddVectorObs(ball.position);
        AddVectorObs(ball.GetComponent<Rigidbody>().velocity);
        AddVectorObs(opponents[0].position);
        AddVectorObs(opponents[0].GetComponent<Rigidbody>().velocity);
        AddVectorObs(opponents[1].position);
        AddVectorObs(opponents[1].GetComponent<Rigidbody>().velocity);
        AddVectorObs(teammates[0].position);
        AddVectorObs(teammates[0].GetComponent<Rigidbody>().velocity);
        AddVectorObs(transform.position);
        AddVectorObs(GetComponent<Rigidbody>().velocity);
    }

    public override void AgentAction(float[] vectorAction, string textAction)
    {
        Vector3 ballpos = ball.position - Vector3.up /4f;
        if (Vector3.Distance(transform.position, ballpos) < Vector3.Distance(teammates[0].position, ballpos)) {
            float opponentAngle1 = AngleBetweenPoints(opponents[0].position, goal.position);
            float opponentAngle2 = AngleBetweenPoints(opponents[1].position, goal.position);

            float opponentDist1 = Vector3.Distance(transform.position, opponents[0].position);
            float opponentDist2 = Vector3.Distance(transform.position, opponents[1].position);

            if (opponentDist1 > 1f && opponentDist2 > 1f) {
                if (Vector3.Distance(transform.position, ballpos) < 1 && Time.time - timeLastKicked > kickCooldown)
                {
                    ball.GetComponent<Rigidbody>().AddForce((otherGoal.position - ballpos).normalized * 500f);
                    timeLastKicked = Time.time;
                }
                else
                {
                    GetComponent<Rigidbody>().velocity = (ballpos - transform.position).normalized * 5f;
                }
            }
            else
            {
                if (Vector3.Distance(transform.position, ballpos) < 0.75 && Time.time - timeLastKicked > kickCooldown) {
                    ball.GetComponent<Rigidbody>().AddForce((teammates[0].position - ballpos).normalized * 500f);
                    timeLastKicked = Time.time;
                }
            }
        }
        else 
        {
            targetPos = new Vector3(vectorAction[0], 0, vectorAction[1]) * 50f;
            GetComponent<Rigidbody>().velocity = (targetPos - transform.position).normalized * 5f; 
        }

        SetReward(nextReward);
        if (nextReward != 0 || ball.position.y <= 0.2f) {
            nextReward = 0;
            Done();
        }
    }

    public override void AgentReset()
    {
        transform.position = Vector3.forward * Random.Range(0f, 2f) * ((leftSide)?10:-10);
        ball.position = Vector3.up;
    }

    public override void AgentOnDone()
    {
    }


    float AngleBetweenPoints (Vector3 a, Vector3 b) {
           float angle = Vector3.Dot((a - transform.position).normalized, (b - transform.position).normalized); 
           return angle;
    }

    void AddPositiveReward () {
        nextReward = 10;
    }

    void AddNegativeReward () {
        nextReward = -5;
    }
}
