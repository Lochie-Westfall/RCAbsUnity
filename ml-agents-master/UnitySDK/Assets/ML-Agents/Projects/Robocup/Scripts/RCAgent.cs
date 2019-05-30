using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;

public class RCAgent : Agent {

    public Transform target;

    public override void CollectObservations()
    {
        AddVectorObs(target.position.x);
        AddVectorObs(transform.position.x);
    }

    public override void AgentAction(float[] vectorAction, string textAction)
    {
        float delta = Mathf.Sign(vectorAction[0]*20f - transform.parent.position.x);

        transform.parent.position += Vector3.right * delta * Time.deltaTime * 25f * Mathf.Abs(vectorAction[1]);

        //transform.parent.position = Vector3.right * vectorAction[0] * 20f + Vector3.up;

        SetReward((Vector3.Distance(transform.parent.position, target.position)<1f)?1:0);
    }

    public override void AgentReset()
    {
        transform.parent.position = Vector3.up + Vector3.right*Random.Range(-2f, 2f);
        target.position = Vector3.up + Vector3.right * Random.Range(-2f, 2f);
    }

    public override void AgentOnDone()
    {
        transform.parent.position = Vector3.up + Vector3.right * Random.Range(-2f, 2f);
        target.position = Vector3.up + Vector3.right * Random.Range(-2f, 2f);
    }
}
