using UnityEngine;
using System.Collections.Generic;

public class HitBox : MonoBehaviour, IHitDetector
{
    [field: SerializeField] public LayerMask DetectionLayer { get; private set; }
    public ICombatAgent Owner { get; private set; }
    
    private Collider hitBoxCollider;
    private HashSet<IHitTargetPart> hitList = new HashSet<IHitTargetPart>();

    private void Awake()
    {
        hitBoxCollider = GetComponent<Collider>();
        if (hitBoxCollider != null)
        {
            hitBoxCollider.isTrigger = true; 
            hitBoxCollider.enabled = false;
        }
    }

    public void Initialize(ICombatAgent owner)
    {
        Owner = owner;
    }

    public void Initialize(ICombatAgent owner, LayerMask detectionLayer)
    {
        Owner = owner;
        DetectionLayer = detectionLayer;
    }

    public void EnableDetection()
    {
        if (hitBoxCollider != null) hitBoxCollider.enabled = true;
        hitList.Clear();
    }

    public void DisableDetection()
    {
        if (hitBoxCollider != null) hitBoxCollider.enabled = false;
        hitList.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (CombatSystem.Instance == null) return;

        // 레이어 마스크 필터링 (Extension 사용)
        if (DetectionLayer.Contains(other.gameObject.layer) == false) return;

        // 유효한 HurtBox인지 확인
        if (CombatSystem.Instance.HasHitTarget(other) == false) return;

        IHitTargetPart targetPart = CombatSystem.Instance.GetHitTarget(other);
        
        // 중복 타격 방지
        if (hitList.Contains(targetPart)) return;

        // 데미지 전송을 위한 정보 구성
        HitInfo hitInfo = new HitInfo();
        hitInfo.hitTarget = targetPart;
        hitInfo.receiver = targetPart.Owner;
        hitInfo.gameObject = other.gameObject;
        hitInfo.position = other.ClosestPoint(transform.position);
        hitInfo.parameter = 0;

        Owner?.OnHitDetected(hitInfo);
        hitList.Add(targetPart);
    }
}
