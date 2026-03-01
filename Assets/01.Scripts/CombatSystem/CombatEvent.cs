public struct CombatEvent
{
    public ICombatAgent Sender;
    public ICombatAgent Receiver;
    
    public float Damage;
    public HitInfo HitInfo;
}
