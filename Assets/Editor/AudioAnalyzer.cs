using UnityEngine;
using UnityEditor;
using System.IO;

public class AudioAnalyzer : EditorWindow
{
    [MenuItem("Tools/Analyze Door Audio")]
    public static void Analyze()
    {
        string outputPath = Path.Combine(Application.dataPath, "../audio_analysis.txt");
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/NewAssets/SFX/openDoor.mp3");
        if (clip == null)
        {
            File.WriteAllText(outputPath, "Error: Could not find openDoor.mp3!");
            return;
        }

        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        int firstSoundIndex = -1;
        float threshold = 0.005f; // Threshold for actual sound

        for (int i = 0; i < samples.Length; i++)
        {
            if (Mathf.Abs(samples[i]) > threshold)
            {
                firstSoundIndex = i;
                break;
            }
        }

        if (firstSoundIndex != -1)
        {
            float timeOfSound = (float)firstSoundIndex / (clip.frequency * clip.channels);
            string result = $"File: {clip.name}\nDuration: {clip.length}s\nChannels: {clip.channels}\nFrequency: {clip.frequency}Hz\nFirstSoundIndex: {firstSoundIndex}\nTimeOfSound: {timeOfSound}s\n";
            File.WriteAllText(outputPath, result);
            Debug.Log($"<color=cyan>[AudioAnalyzer]</color> {result}");
        }
        else
        {
            File.WriteAllText(outputPath, $"File: {clip.name} is completely silent.");
        }
    }
}
