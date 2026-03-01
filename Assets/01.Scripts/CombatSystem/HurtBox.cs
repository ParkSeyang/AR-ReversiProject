using UnityEngine;

[RequireComponent(typeof(Collider))]
public class HurtBox : MonoBehaviour, IHitTargetPart
{
    public ICombatAgent Owner { get; private set; }
    
    [SerializeField] private Collider hurtCollider;
    public Collider Collider => hurtCollider;

    private void Awake()
    {
        if (hurtCollider == null)
        {
            hurtCollider = GetComponent<Collider>();
        }
    }

    public void Initialize(ICombatAgent owner)
    {
        Owner = owner;
        
        if (hurtCollider == null)
        {
            hurtCollider = GetComponent<Collider>();
        }

        if (CombatSystem.Instance != null && hurtCollider != null)
        {
            CombatSystem.Instance.AddHitTarget(hurtCollider, this);
        }
    }

    private void OnDestroy()
    {
        if (CombatSystem.IsInitialized == true && CombatSystem.Instance != null && Collider != null)
        {
            CombatSystem.Instance.RemoveHitTarget(Collider, this);
        }
    }
}
