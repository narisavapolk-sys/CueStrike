#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// Generates placeholder audio clips for CueStrike.
/// Menu: CueStrike → Generate → Create Placeholder Audio
/// 
/// Creates WAV files with simple synthesized sounds:
/// - Ball-ball collision (sharp click)
/// - Ball-cushion hit (thud)
/// - Ball-pocket drop (rolling thud)
/// - Cue-ball impact (crisp hit)
/// - Ambient room tone (low drone)
/// - UI click (short blip)
/// - Chalk sound (scrape)
/// </summary>
public static class CueStrikeAudioGenerator
{
    private const int SampleRate = 44100;
    private const string OutputFolder = "Assets/CueStrike/Audio/Clips";

    [MenuItem("CueStrike/Generate/Create Placeholder Audio")]
    public static void GenerateAllAudio()
    {
        Directory.CreateDirectory(Path.GetFullPath(OutputFolder));

        // Ball-ball collision: sharp metallic click
        GenerateWav("ball_ball_hit", 0.3f, (t) =>
        {
            float env = Mathf.Exp(-t * 30f); // fast decay
            float tone = Mathf.Sin(2f * Mathf.PI * 3200f * t) * 0.4f; // high freq click
            float noise = (Random.value * 2f - 1f) * 0.3f * Mathf.Exp(-t * 50f); // noise burst
            return (tone + noise) * env;
        });

        // Ball-cushion hit: softer thud
        GenerateWav("ball_cushion_hit", 0.4f, (t) =>
        {
            float env = Mathf.Exp(-t * 20f);
            float tone = Mathf.Sin(2f * Mathf.PI * 800f * t) * 0.5f;
            float sub = Mathf.Sin(2f * Mathf.PI * 200f * t) * 0.3f;
            return (tone + sub) * env;
        });

        // Ball-pocket drop: rolling thud with low end
        GenerateWav("ball_pocket_drop", 0.8f, (t) =>
        {
            float env = Mathf.Exp(-t * 8f);
            float thud = Mathf.Sin(2f * Mathf.PI * 150f * t) * 0.6f;
            float roll = Mathf.Sin(2f * Mathf.PI * (300f + t * 100f) * t) * 0.2f * Mathf.Exp(-t * 5f);
            float rattle = Mathf.Sin(2f * Mathf.PI * 1200f * t) * 0.1f * Mathf.Exp(-t * 15f);
            return (thud + roll + rattle) * env;
        });

        // Cue-ball impact: crisp sharp hit
        GenerateWav("cue_ball_hit", 0.25f, (t) =>
        {
            float env = Mathf.Exp(-t * 40f);
            float tone = Mathf.Sin(2f * Mathf.PI * 4000f * t) * 0.3f;
            float mid = Mathf.Sin(2f * Mathf.PI * 1500f * t) * 0.4f;
            float click = (Random.value * 2f - 1f) * 0.2f * Mathf.Exp(-t * 80f);
            return (tone + mid + click) * env;
        });

        // Chalk sound: scraping texture
        GenerateWav("chalk_scrape", 0.5f, (t) =>
        {
            float env = Mathf.Clamp01(t * 10f) * Mathf.Exp(-t * 4f);
            float noise = (Random.value * 2f - 1f) * 0.3f;
            float grind = Mathf.Sin(2f * Mathf.PI * 600f * t + Mathf.Sin(t * 50f) * 3f) * 0.2f;
            return (noise + grind) * env;
        });

        // Ambient room tone: low subtle drone (loopable)
        GenerateWav("ambient_room_tone", 3.0f, (t) =>
        {
            float drone = Mathf.Sin(2f * Mathf.PI * 60f * t) * 0.05f;
            float hum = Mathf.Sin(2f * Mathf.PI * 120f * t) * 0.03f;
            float air = (Random.value * 2f - 1f) * 0.01f;
            float warmth = Mathf.Sin(2f * Mathf.PI * 180f * t) * 0.02f;
            return drone + hum + air + warmth;
        });

        // UI click: short digital blip
        GenerateWav("ui_click", 0.1f, (t) =>
        {
            float env = Mathf.Exp(-t * 60f);
            float tone = Mathf.Sin(2f * Mathf.PI * 2400f * t) * 0.5f;
            return tone * env;
        });

        // UI hover: softer shorter blip
        GenerateWav("ui_hover", 0.08f, (t) =>
        {
            float env = Mathf.Exp(-t * 80f);
            float tone = Mathf.Sin(2f * Mathf.PI * 1800f * t) * 0.3f;
            return tone * env;
        });

        // Crowd murmur: random low chatter
        GenerateWav("crowd_murmur", 3.0f, (t) =>
        {
            float murmur = 0f;
            for (int i = 0; i < 5; i++)
            {
                float freq = 200f + i * 80f + Mathf.Sin(t * (3f + i)) * 30f;
                murmur += Mathf.Sin(2f * Mathf.PI * freq * t) * 0.02f;
            }
            float noise = (Random.value * 2f - 1f) * 0.015f;
            return murmur + noise;
        });

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Audio Generated",
            $"9 placeholder audio clips created in:\n{OutputFolder}\n\n" +
            "Clips:\n" +
            "  • ball_ball_hit.wav\n" +
            "  • ball_cushion_hit.wav\n" +
            "  • ball_pocket_drop.wav\n" +
            "  • cue_ball_hit.wav\n" +
            "  • chalk_scrape.wav\n" +
            "  • ambient_room_tone.wav\n" +
            "  • ui_click.wav\n" +
            "  • ui_hover.wav\n" +
            "  • crowd_murmur.wav\n\n" +
            "Replace these with real recordings for AAA quality.",
            "OK");

        Debug.Log($"[CueStrike] Generated 9 placeholder audio clips in {OutputFolder}");
    }

    private delegate float SynthFunc(float t);

    private static void GenerateWav(string filename, float duration, SynthFunc synth)
    {
        int sampleCount = Mathf.CeilToInt(SampleRate * duration);
        float[] samples = new float[sampleCount];

        // Use a fixed seed per file for reproducibility
        Random.InitState(filename.GetHashCode());

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SampleRate;
            samples[i] = Mathf.Clamp(synth(t), -1f, 1f);
        }

        string path = Path.GetFullPath(Path.Combine(OutputFolder, filename + ".wav"));
        WriteWav(path, samples, SampleRate);
        Debug.Log($"[CueStrike Audio] Created: {filename}.wav ({duration}s)");
    }

    private static void WriteWav(string filepath, float[] samples, int sampleRate)
    {
        using (var stream = new FileStream(filepath, FileMode.Create))
        using (var writer = new BinaryWriter(stream))
        {
            int channels = 1;
            int bitsPerSample = 16;
            int byteRate = sampleRate * channels * bitsPerSample / 8;
            int blockAlign = channels * bitsPerSample / 8;
            int dataSize = samples.Length * blockAlign;

            // RIFF header
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + dataSize);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

            // fmt chunk
            writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16); // chunk size
            writer.Write((short)1); // PCM
            writer.Write((short)channels);
            writer.Write(sampleRate);
            writer.Write(byteRate);
            writer.Write((short)blockAlign);
            writer.Write((short)bitsPerSample);

            // data chunk
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            writer.Write(dataSize);

            foreach (float sample in samples)
            {
                short s = (short)(Mathf.Clamp(sample, -1f, 1f) * 32767f);
                writer.Write(s);
            }
        }
    }
}
#endif
