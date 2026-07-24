using DG.Tweening;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;

    [Header("Camera Position")]
    [SerializeField]
    private Vector3 offset =
        new Vector3(0f, 8f, -8f);

    [Header("Follow Settings")]
    [SerializeField] private float followSpeed = 10f;

    [Header("Look Settings")]
    [SerializeField]
    private Vector3 lookOffset =
        new Vector3(0f, 1f, 0f);

    [SerializeField] private float rotationSpeed = 10f;

    [Header("Hit Shake")]
    [SerializeField] private float shakeDuration = 0.12f;
    [SerializeField] private float shakeStrength = 0.15f;
    [SerializeField] private int shakeVibrato = 8;

    private void LateUpdate()
    {
        if (player == null)
            return;

        FollowPlayer();
        LookAtPlayer();
    }

    private void FollowPlayer()
    {
        Vector3 targetPosition =
            player.position + offset;

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            followSpeed * Time.deltaTime
        );
    }

    private void LookAtPlayer()
    {
        Vector3 targetPosition =
            player.position + lookOffset;

        Vector3 direction =
            targetPosition - transform.position;

        if (direction.sqrMagnitude <= 0.01f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    public void PlayHitShake()
    {
        transform.DOKill();

        transform.DOShakePosition(
            shakeDuration,
            new Vector3(
                0f,
                shakeStrength,
                0f
            ),
            shakeVibrato,
            0f,
            false,
            true
        );
    }
}