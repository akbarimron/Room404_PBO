using UnityEngine;

public class EnemyDamage : MonoBehaviour
{
    private void OnTriggerEnter(Collider other) // Gunakan OnTrigger dan Collider (bukan Collision)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth health = other.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(1);
                Debug.Log("Player terkena hit!"); 
            }
        }
    }
}