using UnityEngine;
using Dreamteck.Splines; // Wajib diisi agar script mengenali package Dreamteck

public class SplineNPCBrain : MonoBehaviour
{
    private SplineFollower follower;
    private Animator anim;
    
    private bool isIdling = false;
    private float savedSpeed = 2f; // Tempat menyimpan kecepatan asli NPC

    void Start()
    {
        // Otomatis mencari komponen di tubuh NPC saat game dimulai
        follower = GetComponent<SplineFollower>();
        anim = GetComponent<Animator>();
        
        if (follower != null)
        {
            // Catat berapa kecepatan jalan awal NPC yang diatur di Inspector (misal: 2)
            savedSpeed = follower.followSpeed;
        }
    }

    void Update()
    {
        if (anim == null || follower == null) return;

        // JIKA NPC sedang dalam masa istirahat (di-trigger)
        if (isIdling)
        {
            follower.followSpeed = 0f; // Paksa kecepatan fisik ke 0 (berhenti di tempat)
            anim.SetFloat("Speed", 0f); // Paksa Animator ke 0 (memicu animasi Idle)
        }
        else
        {
            // JIKA sedang jalan normal, samakan parameter animasi dengan kecepatan jalannya
            anim.SetFloat("Speed", follower.followSpeed);
        }
    }

    // FUNGSI UTAMA: Fungsi ini yang akan dipanggil oleh tombol Trigger di lintasan Dreamteck
    public void StopAndIdle(float duration)
    {
        // Cegah sistem menumpuk perintah jika NPC sudah dalam posisi diam
        if (!isIdling)
        {
            StartCoroutine(IdleRoutine(duration));
        }
    }

    // Fungsi pewaktu (Coroutine) untuk menghitung durasi diam NPC
    private System.Collections.IEnumerator IdleRoutine(float duration)
    {
        isIdling = true;
        
        // Ambil data kecepatan terakhir sebelum di-nol-kan untuk amannya
        if (follower.followSpeed > 0) savedSpeed = follower.followSpeed;
        
        follower.followSpeed = 0f;
        anim.SetFloat("Speed", 0f);

        // TAHAN NPC di titik ini selama beberapa detik sesuai input dari Trigger
        yield return new WaitForSeconds(duration);

        // Setelah waktu habis, kembalikan kecepatan ke semula dan perbolehkan jalan lagi
        if (follower != null)
        {
            follower.followSpeed = savedSpeed;
        }
        isIdling = false;
    }
}