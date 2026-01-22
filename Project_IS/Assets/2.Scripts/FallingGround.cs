using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class FallingGround : PushPullObject
{
    [Header("FallingGround")]
    [SerializeField] private int _StepOnCount = 3;
    [SerializeField] private float _StepOnDuration = .1f;
    [SerializeField] private float _StepOnDistance = .2f;
    [SerializeField] private float _pushBackDuration = .2f;
    [SerializeField] private float _pushBackDistance = .1f;
    [SerializeField] private GameObject _goTopGround;

    private Rigidbody mRigidbody;

    private bool mHasFallen = false;
    private int mCurrentStepOnCount = 0;

    public void StepOn()
    {
        if (mHasFallen)
            return;

        ++mCurrentStepOnCount;

        if (mCurrentStepOnCount >= _StepOnCount)
        {
            Fall();

            return;
        }

        StartCoroutine(eStepOn());
    }

    protected override void Start()
    {
        base.Start();

        mRigidbody = GetComponent<Rigidbody>();
    }

    private void Fall()
    {
        mHasFallen = true;
        mRigidbody.isKinematic = false;

        _sidePassable = true;
        _pushable = true;
        _canClimb = true;

        // _goTopGround.SetActive(false);
        // gameObject.layer = LayerMask.NameToLayer("Interactable");
    }

    private IEnumerator eStepOn()
    {
        Vector3 originalPosition = transform.position;
        Vector3 targetPosition = originalPosition - new Vector3(0f, _StepOnDistance, 0f);
        float elapsedTime = 0f;

        while (elapsedTime < _StepOnDuration)
        {
            transform.position = Vector3.Lerp(originalPosition, targetPosition, (elapsedTime / _StepOnDuration));

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPosition;

        Vector3 startPosition = transform.position;
        targetPosition = startPosition + new Vector3(0f, _pushBackDistance, 0f);

        elapsedTime = 0f;

        while (elapsedTime < _pushBackDuration)
        {
            transform.position = Vector3.Lerp(startPosition, targetPosition, (elapsedTime / _pushBackDuration));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPosition;
    }
}
