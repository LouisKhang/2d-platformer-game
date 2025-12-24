using UnityEngine;
using Platformer;

public class DamageDealer : MonoBehaviour
{
    [Tooltip("Số lượng sát thương mà đối tượng này gây ra.")]
    public int damageAmount = 1;

    /// <summary>
    /// Trả về giá trị sát thương của đối tượng này.
    /// </summary>
    public int GetDamage()
    {
        return damageAmount;
    }
}