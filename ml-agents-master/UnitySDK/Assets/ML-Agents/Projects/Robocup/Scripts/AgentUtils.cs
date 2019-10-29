using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentUtils : MonoBehaviour
{

    AgentController agentController;
    AgentConfig config;

    Transform ball;
    [HideInInspector]
    public Transform floor;

    [HideInInspector]
    public List<Transform> teammates;
    [HideInInspector]
    public List<Transform> opponents;
    
    [HideInInspector]
    public Vector3 sideVector;
    // Start is called before the first frame update
    void Start()
    {
        agentController = GetComponent<AgentController>();
        config = agentController.config;
        ball = GameObject.Find("Ball").transform;
        floor = GameObject.Find("Floor").transform;

        teammates = new List<Transform>();
        opponents = new List<Transform>();

        sideVector = (agentController.isLeftSide)?new Vector3(1, 0f, 1):new Vector3(-1, 0f, -1);
        
        foreach (GameObject obj in GameObject.FindGameObjectsWithTag("left")) 
        {
            if (obj != gameObject) 
            {
                if (agentController.isLeftSide) 
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
                if (!agentController.isLeftSide) 
                {
                    teammates.Add(obj.transform);
                }
                else 
                {
                    opponents.Add(obj.transform);
                }
            }
        }
    }

    public float PathToPointLength (Transform from, Vector3 to) 
    {
        float dir = Vector3.Dot(from.right, to - from.position);
        dir = (dir==0)?0:Mathf.Sign(dir);

        Vector3 circleCentre = from.position+from.right*config.turnRadius*dir;

        if (Vector3.Distance(circleCentre, ball.position) > config.turnRadius) {
            float tangentAngle = Mathf.Asin(config.turnRadius/Vector3.Distance(to, circleCentre));
            float distance = Mathf.Sqrt(config.turnRadius*config.turnRadius + Mathf.Pow(Vector3.Distance(to, circleCentre),2f));
            Vector3 circleBallDelta = (circleCentre - to).normalized;
            Vector3 tangentPos = ((Quaternion.Euler(0f, tangentAngle * Mathf.Rad2Deg, 0f) * circleBallDelta) + to) * distance;

            float pathSectorLength = config.turnRadius * Mathf.Deg2Rad * Vector3.Angle(from.position - circleCentre, tangentPos - circleCentre);

            float totalDistance = pathSectorLength + Vector3.Distance(tangentPos, to);

            return totalDistance;
        }
        else {
            return Mathf.PI*config.turnRadius;
        }
    }

    public Transform GetNearestTeammateToPoint (Vector3 point) 
    {
        Transform closestTeammate = transform;
        float closestTeammateDist = PathToPointLength(transform, point);
        foreach (Transform teammate in teammates) 
        {
            float timeToStand = (agentController.isStanding) ? 0f : agentController.timeLastCollided + config.fallRecoveryTime - Time.time;
            float dist = PathToPointLength(teammate, point) + timeToStand/config.walkSpeed;
            if (dist < closestTeammateDist) 
            {
                closestTeammateDist = dist;
                closestTeammate = teammate;
            }
        }
  
        return closestTeammate;
    }
    
    public void MoveToAndKickBall () 
    {
        if (agentController.canKick)
        {
            agentController.KickBallAtAngle(GetKickDirection());
        }
        else
        {
            MoveToPoint(ball.position);
        }
    }
  
    public void PositionToReceiveBall (float x, float y) 
    {
        MoveToPoint(new Vector3(x * sideVector[0], 0, y * sideVector[2]));
    }
  
    
    public void PositionToReceiveBallRandom () 
    {
        MoveToPoint(new Vector3(Random.Range(-3f,3f) * sideVector[0], 0,  Random.Range(-4.5f,4.5f) * sideVector[2]));
    }
  
    public float GetKickDirection () {
        int leftSegmentScore = kickSegmentScore(config.kickSegmentAngle*-1.5f, config.kickSegmentAngle*-0.5f);
        int middleSegmentScore = kickSegmentScore(config.kickSegmentAngle*-0.5f, config.kickSegmentAngle*0.5f);
        int rightSegmentScore = kickSegmentScore(config.kickSegmentAngle*0.5f, config.kickSegmentAngle*1.5f);
  
        if (rightSegmentScore > middleSegmentScore && rightSegmentScore > leftSegmentScore) 
        {
            return config.kickSegmentAngle;
        }
        else if (leftSegmentScore > middleSegmentScore && leftSegmentScore > rightSegmentScore) 
        {
            return -config.kickSegmentAngle;
        }
        else 
        {
            if (middleSegmentScore == 0) {
                return Mathf.Clamp(SignedAngleToPoint(agentController.otherGoal.position), -config.kickSegmentAngle, config.kickSegmentAngle);
            }
            else {
                return 0f;
            }
        }
    }
  
    public int kickSegmentScore (float leftAngle, float rightAngle) {
  
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
        
        if (leftAngle <= SignedAngleToPoint(agentController.goal.position) && SignedAngleToPoint(agentController.goal.position) <= rightAngle) {
            score += -5;
        }
  
        if (leftAngle <= SignedAngleToPoint(agentController.otherGoal.position) && SignedAngleToPoint(agentController.otherGoal.position) <= rightAngle) {
            score += 5;
        }
  
        return score;
    }
  
    public void MoveToPoint(Vector3 point)
    {
        Vector3 diff = point - transform.position;
        diff.y = 0f;
  
        float dir = Mathf.Sign(Vector3.Dot(transform.right, diff));
  
        if (AngleBetweenPoints(transform.position + transform.forward, point) < config.facingAngleRange) 
        {
            agentController.Walk(0f);
        }
        else 
        {
            if (Vector3.Distance(transform.position + transform.right*config.turnRadius*dir, point) > config.turnRadius) 
            {
                agentController.Walk(dir);
            }
            else
            {
                agentController.Walk(0f);
            }
        }
    }
  
    public float AngleBetweenPoints (Vector3 a, Vector3 b) {
        return Vector3.Angle(b - transform.position, a - transform.position);
    }
  
    public float AngleToPoint (Vector3 point) {
        return Vector3.Angle(transform.forward, point - transform.position);
    }
    
    public float SignedAngleToPoint (Vector3 point) {
        return Vector3.SignedAngle(transform.forward, point - transform.position, Vector3.up);
    }
}
