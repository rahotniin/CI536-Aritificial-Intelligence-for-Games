using System.Collections.Generic;
using UnityEngine;

public class FrameRateStats : MonoBehaviour
{
    [SerializeField] float current;
    [SerializeField] float average;
    [Range(0f, 10f)]
    [SerializeField] float averagingInterval;
    float timeSinceLastAverage = 0f;
    List<float> samples = new();

    void Update()
    {
        float dt = Time.deltaTime;
        current = 1f / dt;

        samples.Add(dt);
        timeSinceLastAverage += dt;

        if (timeSinceLastAverage > averagingInterval)
        {
            timeSinceLastAverage = 0f;

            float sum = 0f;

            foreach (float sample in samples)
            {
                sum += sample;
            }

            float avg = sum / (samples.Count + 1);
            average = 1f / avg;

            samples.Clear();
        }
    }
}
