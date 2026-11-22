using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArcaneRestoration
{
    /// <summary>
    /// Sound and particle feedback. Place one in the scene (under Systems) with its clips and sparkle prefab;
    /// everything else calls the static helpers. Pooled, so playing feedback does not allocate.
    /// </summary>
    [DisallowMultipleComponent]
    public class Feedback : MonoBehaviour
    {
        public enum Sound
        {
            Lift,
            Release,
            SortCorrect,
            SortWrong,
            BinGulp,
            Bounce,
        }

        [Serializable]
        struct SoundClip
        {
            public Sound sound;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume;
        }

        [SerializeField] SoundClip[] m_Clips = Array.Empty<SoundClip>();
        [SerializeField, Range(1, 16)] int m_Voices = 8;
        [SerializeField, Range(0f, 0.3f)] float m_PitchVariation = 0.06f;
        [SerializeField] ParticleSystem m_SparklePrefab;
        [SerializeField, Range(1, 8)] int m_Sparkles = 4;

        static Feedback s_Instance;

        readonly List<AudioSource> m_Sources = new List<AudioSource>();
        readonly List<ParticleSystem> m_SparklePool = new List<ParticleSystem>();
        int m_NextSource;
        int m_NextSparkle;

        void Awake()
        {
            s_Instance = this;
            for (int i = 0; i < m_Voices; i++)
            {
                var go = new GameObject("Voice " + i);
                go.transform.SetParent(transform, false);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 1f;
                src.rolloffMode = AudioRolloffMode.Linear;
                src.minDistance = 1f;
                src.maxDistance = 25f;
                m_Sources.Add(src);
            }
            if (m_SparklePrefab != null)
                for (int i = 0; i < m_Sparkles; i++)
                {
                    var ps = Instantiate(m_SparklePrefab, transform);
                    ps.gameObject.name = "Sparkle " + i;
                    m_SparklePool.Add(ps);
                }
        }

        void OnDestroy()
        {
            if (s_Instance == this)
                s_Instance = null;
        }

        /// <summary>Plays a feedback sound at a world position.</summary>
        public static void Play(Sound sound, Vector3 position, float volumeScale = 1f)
        {
            if (s_Instance != null)
                s_Instance.PlayInternal(sound, position, volumeScale);
        }

        /// <summary>Plays a burst of particles of the given colour at a world position.</summary>
        public static void Sparkle(Vector3 position, Color colour)
        {
            if (s_Instance != null)
                s_Instance.SparkleInternal(position, colour);
        }

        void PlayInternal(Sound sound, Vector3 position, float volumeScale)
        {
            foreach (var c in m_Clips)
            {
                if (c.sound != sound || c.clip == null)
                    continue;
                var src = m_Sources[m_NextSource];
                m_NextSource = (m_NextSource + 1) % m_Sources.Count;
                src.transform.position = position;
                src.pitch = 1f + UnityEngine.Random.Range(-m_PitchVariation, m_PitchVariation);
                src.PlayOneShot(c.clip, c.volume * volumeScale);
                return;
            }
        }

        void SparkleInternal(Vector3 position, Color colour)
        {
            if (m_SparklePool.Count == 0)
                return;
            var ps = m_SparklePool[m_NextSparkle];
            m_NextSparkle = (m_NextSparkle + 1) % m_SparklePool.Count;
            ps.transform.position = position;
            var main = ps.main;
            main.startColor = colour;
            ps.Clear();
            ps.Play();
        }
    }
}
