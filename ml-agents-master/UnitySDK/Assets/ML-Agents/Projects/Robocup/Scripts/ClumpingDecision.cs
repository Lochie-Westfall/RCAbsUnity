using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;

public class ClumpingDecision : Decision
{
    public override float[] Decide(List<float> vectorObs, List<Texture2D> visualObs, float reward, bool done, List<float> memory)
    {
        float[] act = new float[0];

        return act;
    }

    public override List<float> MakeMemory(List<float> vectorObs, List<Texture2D> visualObs, float reward, bool done, List<float> memory)
    {
        return new List<float>();
    }
}
