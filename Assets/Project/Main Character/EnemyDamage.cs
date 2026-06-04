using UnityEngine;

/// <summary>
/// EnemyDamage — Ditrigger saat collider hantu menyentuh Player.
/// Mengaktifkan seluruh alur jumpscare via JumpscareManager.
/// </summary>
public class EnemyDamage : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerHealth health = other.GetComponent<PlayerHealth>();
        if (health == null || health.isHiding) return;

        // Cari referensi GhostAI di parent
        GhostAI ghost = GetComponentInParent<GhostAI>();

        if (JumpscareManager.Instance != null)
        {
            // JumpscareManager mengurus:
            //   - ghost invisible
            //   - tampilkan wajah + audio
            //   - kurangi nyawa (health.TakeDamage)
            //   - ghost teleport jauh
            //   - efek kelap-kelip
            //   - unfreeze player
            JumpscareManager.Instance.TriggerJumpscare(ghost, health);
        }
        else
        {
            // Fallback jika JumpscareManager belum ada di scene
            Debug.LogWarning("[EnemyDamage] JumpscareManager.Instance tidak ditemukan! Pastikan ada GameObject JumpscareManager di scene.");
            health.TakeDamage(1);

            if (ghost != null)
                ghost.TriggerJumpscare(); // Audio-only fallback
        }

        Debug.Log("[EnemyDamage] Player terkena hantu! Jumpscare dimulai.");
    }
}