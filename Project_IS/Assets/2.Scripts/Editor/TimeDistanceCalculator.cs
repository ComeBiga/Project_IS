using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class TimeDistanceCalculater : EditorWindow
{
    private float mFrameRate = 24f;
    private float mStartVelocity = 0f;
    private int mframeTime = 1;
    private float mAcceleration = 0f;

    private float mResultDistance = 0f;
    private float mResultVelocity = 0f;

    [MenuItem("Tools/Utility/Time Distance Calculator")]
    private static void Open()
    {
        GetWindow<TimeDistanceCalculater>(utility: true, "Time Distance Calculator");
    }

    private void OnGUI()
    {
        mStartVelocity = EditorGUILayout.FloatField("Start Velocity", mStartVelocity);
        mAcceleration = EditorGUILayout.FloatField("Acceleration", mAcceleration);

        EditorGUILayout.LabelField("Time", EditorStyles.boldLabel);
        mFrameRate = EditorGUILayout.FloatField("Frame Rate", mFrameRate);
        mframeTime = EditorGUILayout.IntField("Frame Time", mframeTime);

        EditorGUILayout.LabelField($"Distance = {mResultDistance}", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Velocity = {mResultVelocity}", EditorStyles.boldLabel);

        if (GUILayout.Button("Calculate", GUILayout.Height(50)))
        {
            mResultDistance = calculateDistance();
            mResultVelocity = calculateVelocity();
        }
    }

    private float calculateDistance()
    {
        float time = calculateTimeFromFrame();
        float result = mStartVelocity * time + .5f * mAcceleration * time * time;

        // Debug.Log($"Result: {result}, Time: {time}");
        return result;
    }
    
    private float calculateVelocity()
    {
        float time = calculateTimeFromFrame();
        float result = mStartVelocity + mAcceleration * time;

        return result;
    }

    private float calculateTimeFromFrame()
    {
        return mframeTime / mFrameRate;
    }
}
