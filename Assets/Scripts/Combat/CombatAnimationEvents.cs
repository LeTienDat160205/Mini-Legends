using UnityEngine;

public class CombatAnimationEvents : MonoBehaviour
{
    [SerializeField] private MonoBehaviour attack;

    private void Awake()
    {
        if (attack == null)
            attack = GetComponentInParent<MonoBehaviour>();
    }

    public void EnableHitbox()
    {
        if (attack != null)
            attack.SendMessage("EnableHitbox", SendMessageOptions.DontRequireReceiver);
    }

    public void DisableHitbox()
    {
        if (attack != null)
            attack.SendMessage("DisableHitbox", SendMessageOptions.DontRequireReceiver);
    }

    public void EndAttack()
    {
        if (attack != null)
            attack.SendMessage("EndAttack", SendMessageOptions.DontRequireReceiver);
    }
}
