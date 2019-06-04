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

    Vector3 sideVector;

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
        if (Vector3.Distance(ball.position, transform.position) < Vector3.Distance(ball.position, GetNearestTeammateToPoint(ball.position).position))
        {
            float closestOpponentDist = 9999999f;
            foreach (Transform opponent in opponents) {
//                float dist = Vector3.Distance(transform.position, opponent.position); 
                float dist = AngleBetweenPoints(opponent.position, otherGoal.position);
                if (dist < closestOpponentDist) {
                    closestOpponentDist = dist;
                }
            }
            if (closestOpponentDist > 20f) {
                if (Vector3.Distance(transform.position, ball.position) < 0.25f && Time.time - timeLastKicked > kickCooldown)
                {
                    KickBallPoint(otherGoal.position);
                }
                else
                {
                    MoveToPoint(ball.position);
                }
            }
            else
            {
                if (Vector3.Distance(transform.position, ball.position) < 0.25f && Time.time - timeLastKicked > kickCooldown) 
                {
                    KickBallPoint(GetNearestTeammateToPoint(transform.position).position);
                }
                else
                {
                    MoveToPoint(ball.position);
                }
            }
        }
        else 
        {
            MoveToPoint(new Vector3(vectorAction[0] * sideVector[0], 0, vectorAction[1] * sideVector[2]) * 10f);
        }

        SetReward(nextReward);
        if (nextReward != 0 || ball.position.y <= 0f) 
        {
            nextReward = 0;
            AgentReset();
        }
    }

    public override void AgentReset()
    {
        transform.position = Vector3.right * Random.Range(-2f, 2f) + Vector3.forward * Random.Range(0f, 4f) * ((leftSide)?1:-1) + Vector3.up*0;
        ball.position = Vector3.up;
        ball.GetComponent<Rigidbody>().velocity = Vector3.zero;
    }

    public override void AgentOnDone()
    {
    }
    
    Transform GetNearestTeammateToPoint (Vector3 point) 
    {
        Transform closestTeammate = transform;
        float closestTeammateDist = 99999999999999f;
        foreach (Transform teammate in teammates) 
        {
            float dist = Vector3.Distance(point, teammate.position); 
            if (dist < closestTeammateDist) 
            {
                closestTeammateDist = dist;
                closestTeammate = teammate;
            }
        }

        return closestTeammate;
    }

    void KickBallPoint(Vector3 point) 
    {
        ball.GetComponent<Rigidbody>().AddForce((point - (ball.position)).normalized * 200f);
        GetComponent<Rigidbody>().velocity *= 0f;
        timeLastKicked = Time.time;
    }

    void MoveToPoint(Vector3 point)
    {
        Vector3 offset = point - transform.position;
        offset.y = 0f;
        transform.rotation = Quaternion.LookRotation(offset);
        Vector3 velocity = Vector3.MoveTowards(GetComponent<Rigidbody>().velocity, offset.normalized * 1f, Time.deltaTime * 1f); 
        velocity.y = 0f;
        GetComponent<Rigidbody>().velocity = velocity; 
    }

    float AngleBetweenPoints (Vector3 a, Vector3 b) 
    {
           return Vector3.Angle(b - transform.position, a - transform.position);
    }

    void AddPositiveReward ()
     {
        nextReward = 10;
        if (gameObject.name.Contains("1")) {
            print((leftSide)?"Left Scored":"Right Scored");
        }
    }

    void AddNegativeReward () 
    {
        nextReward = -5;
    }
}
