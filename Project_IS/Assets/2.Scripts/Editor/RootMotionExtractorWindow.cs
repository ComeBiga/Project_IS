using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class RootMotionExtractorWindow : EditorWindow
{
    private GameObject rigPrefab;
    private AnimationClip clip;

    private float sampleRate = 60f;

    [MenuItem("Tools/Animation/Root Motion Extractor")]
    private static void Open()
    {
        GetWindow<RootMotionExtractorWindow>("Root Motion Extractor");
    }

    private void OnGUI()
    {
        rigPrefab = (GameObject)EditorGUILayout.ObjectField("Rig Prefab", rigPrefab, typeof(GameObject), false);

        clip = (AnimationClip)EditorGUILayout.ObjectField("Animation Clip", clip, typeof(AnimationClip), false);

        sampleRate = EditorGUILayout.FloatField( "Sample Rate", sampleRate);

        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(rigPrefab == null || clip == null || sampleRate <= 0f))
        {
            if (GUILayout.Button("Extract"))
            {
                Extract();
            }
        }
    }


    private void Extract()
    {
        GameObject instance = null;
        PlayableGraph graph = default;

        try
        {
            //
            // 1. 샘플링용 캐릭터 생성
            //

            instance = Instantiate(rigPrefab);

            instance.name = "__RootMotionPreview__";

            instance.hideFlags = HideFlags.HideAndDontSave;

            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            //
            // 불필요한 스크립트 비활성화
            //

            foreach (MonoBehaviour behaviour in instance.GetComponentsInChildren<MonoBehaviour>())
            {
                behaviour.enabled = false;
            }

            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>())
            {
                renderer.enabled = false;
            }

            Animator animator = instance.GetComponentInChildren<Animator>();

            if (animator == null)
            {
                Debug.LogError("Rig Prefab에 Animator가 없습니다.");

                return;
            }

            //
            // Root Motion delta 계산에 필요
            //

            animator.applyRootMotion = true;

            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;


            //
            // 2. PlayableGraph 생성
            //

            graph = PlayableGraph.Create("Root Motion Extractor");

            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);


            AnimationClipPlayable playable = AnimationClipPlayable.Create(graph, clip);

            playable.SetApplyFootIK(false);


            AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "Animation", animator);

            output.SetSourcePlayable(playable);

            graph.Play();


            //
            // 시작 상태 평가
            //

            playable.SetTime(0);

            graph.Evaluate(0);


            //
            // 3. Root Rotation 샘플링
            //

            List<Keyframe> degreeKeys = new List<Keyframe>();

            degreeKeys.Add(new Keyframe(0f, 0f));

            float accumulatedYaw = 0f;

            float currentTime = 0f;

            int sampleCount = Mathf.CeilToInt(clip.length * sampleRate);


            for (int i = 1; i <= sampleCount; ++i)
            {
                float nextTime = Mathf.Min(i / sampleRate, clip.length);

                float deltaTime = nextTime - currentTime;

                if (deltaTime <= 0f)
                    continue;


                //
                // 애니메이션을 deltaTime만큼 진행
                //

                graph.Evaluate(deltaTime);


                //
                // 방금 평가한 구간의 Root 회전량
                //

                Quaternion deltaRotation = animator.deltaRotation;


                //
                // Y축 회전만 추출
                //

                float deltaYaw = Mathf.DeltaAngle(0f, deltaRotation.eulerAngles.y);

                accumulatedYaw += deltaYaw;

                float normalizedTime = nextTime / clip.length;

                degreeKeys.Add(new Keyframe(normalizedTime, accumulatedYaw));

                currentTime = nextTime;
            }


            //
            // 4. 정규화 Curve 생성
            //

            if (Mathf.Abs(accumulatedYaw) < 0.001f)
            {
                Debug.LogWarning("Root Rotation이 거의 없습니다. " + "Animation Import Settings의 " + "Root Transform Rotation을 확인하세요.");

                return;
            }

            AnimationCurve degreeCurve = new AnimationCurve(degreeKeys.ToArray());

            Keyframe[] normalizedKeys = new Keyframe[degreeKeys.Count];

            for (int i = 0; i < degreeKeys.Count; ++i)
            {
                normalizedKeys[i] = new Keyframe(degreeKeys[i].time, degreeKeys[i].value / accumulatedYaw);
            }

            AnimationCurve normalizedCurve = new AnimationCurve(normalizedKeys);

            SetLinearTangents(degreeCurve);
            SetLinearTangents(normalizedCurve);


            //
            // 5. Asset 저장
            //

            string path = EditorUtility.SaveFilePanelInProject("Save Root Motion Data", clip.name + "_RootRotation", "asset", "저장할 위치를 선택하세요.");

            if (string.IsNullOrEmpty(path))
                return;

            RootMotionRotationData data = CreateInstance<RootMotionRotationData>();

            data.sourceClip = clip;
            data.totalYaw = accumulatedYaw;
            data.rotationDegrees = degreeCurve;
            data.normalizedRotation = normalizedCurve;

            AssetDatabase.CreateAsset(data, path);

            AssetDatabase.SaveAssets();

            Selection.activeObject = data;

            Debug.Log($"Root Motion 추출 완료 : " + $"{accumulatedYaw:F2}°");
        }
        finally
        {
            if (graph.IsValid())
                graph.Destroy();

            if (instance != null)
                DestroyImmediate(instance);
        }
    }


    private static void SetLinearTangents(AnimationCurve curve)
    {
        for (int i = 0; i < curve.length; ++i)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear);

            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
        }
    }
}
