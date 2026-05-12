using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace TechC.Core.Manager
{
    public class AudioManager : Singleton<AudioManager>
    {
        [SerializeField] private AudioData audioData;

        #region 音量設定

        [Range(0f, 1f)] public float masterVolume = 1.0f;
        [Range(0f, 1f)] public float bgmVolume = 1.0f;
        [Range(0f, 1f)] public float seVolume = 1.0f;
        [Range(0f, 1f)] public float voiceVolume = 1.0f;
        [Range(0f, 1f)] public float ambientVolume = 1.0f; // 環境音（雨など）用

        #endregion

        [Header("SE ランダムピッチ設定")]
        [Tooltip("SE を連続再生するときにピッチをランダムにずらす範囲（±）")]
        [SerializeField, Range(0f, 0.5f)] private float sePitchVariance = 0.05f;

        private AudioSource bgmSource;
        private AudioSource bgmCrossSource;
        private List<AudioSource> seSources = new List<AudioSource>();

        [SerializeField] private int seSourceCount = 10;

        private BGMID currentBGM = BGMID.None;
        private bool isBgmFading = false;

        protected override bool DontDestroy => true;

        protected override void OnInit()
        {
            base.OnInit();

            var bgmObj = new GameObject("BGM_Source");
            bgmObj.transform.parent = transform;
            bgmSource = bgmObj.AddComponent<AudioSource>();
            bgmSource.playOnAwake = false;

            var bgmCrossObj = new GameObject("BGM_Cross_Source");
            bgmCrossObj.transform.parent = transform;
            bgmCrossSource = bgmCrossObj.AddComponent<AudioSource>();
            bgmCrossSource.playOnAwake = false;

            CreateAudioSourcePool("SE_Source", seSourceCount, seSources);
        }

        private void CreateAudioSourcePool(string name, int count, List<AudioSource> pool)
        {
            var poolParent = new GameObject(name + "_Pool");
            poolParent.transform.parent = transform;

            for (int i = 0; i < count; i++)
            {
                var obj = new GameObject($"{name}_{i}");
                obj.transform.parent = poolParent.transform;
                var src = obj.AddComponent<AudioSource>();
                src.playOnAwake = false;
                pool.Add(src);
            }
        }

        #region BGM 関連

        public void PlayBGM(BGMID id, bool isCrossFade = true)
        {
            if (currentBGM == id) return;

            if (isBgmFading) { StopAllCoroutines(); isBgmFading = false; }

            var info = audioData.GetBGM(id);
            if (info == null || info.clip == null)
            {
                Debug.LogWarning($"BGM の ID {id} が見つかりません");
                return;
            }

            currentBGM = id;

            if (isCrossFade && bgmSource.isPlaying)
                StartCoroutine(CrossFadeBGM(info));
            else
            {
                bgmSource.clip = info.clip;
                bgmSource.volume = info.volume * bgmVolume * masterVolume;
                bgmSource.pitch = info.pitch;
                bgmSource.loop = info.loop;

                if (info.fadeInTime > 0)
                {
                    bgmSource.volume = 0;
                    bgmSource.Play();
                    StartCoroutine(FadeBGM(bgmSource, 0,
                        info.volume * bgmVolume * masterVolume, info.fadeInTime));
                }
                else bgmSource.Play();
            }
        }

        public void StopBGM(float fadeOutTime = 0.5f)
        {
            if (!bgmSource.isPlaying) return;

            var info = audioData.GetBGM(currentBGM);
            float fadeTime = info != null ? info.fadeOutTime : fadeOutTime;

            if (fadeTime > 0) StartCoroutine(FadeBGM(bgmSource, bgmSource.volume, 0, fadeTime, true));
            else bgmSource.Stop();

            currentBGM = BGMID.None;
        }

        private IEnumerator CrossFadeBGM(AudioData.BGMInfo newInfo)
        {
            isBgmFading = true;
            var current = bgmSource;
            var next = bgmCrossSource;

            next.clip = newInfo.clip; next.volume = 0;
            next.pitch = newInfo.pitch; next.loop = newInfo.loop;
            next.Play();

            float startVol = current.volume;
            float endVol = newInfo.volume * bgmVolume * masterVolume;
            float fadeTime = Mathf.Max(newInfo.fadeInTime, 0.5f);
            float timer = 0;

            while (timer < fadeTime)
            {
                timer += Time.deltaTime; float t = timer / fadeTime;
                current.volume = Mathf.Lerp(startVol, 0, t);
                next.volume = Mathf.Lerp(0, endVol, t);
                yield return null;
            }

            current.Stop();
            SwapBGMSources();
            isBgmFading = false;
        }

        private IEnumerator FadeBGM(AudioSource src, float from, float to,
            float fadeTime, bool stopAfter = false)
        {
            isBgmFading = true;
            float timer = 0;
            while (timer < fadeTime)
            {
                timer += Time.deltaTime;
                src.volume = Mathf.Lerp(from, to, timer / fadeTime);
                yield return null;
            }
            if (stopAfter) src.Stop();
            isBgmFading = false;
        }

        private void SwapBGMSources()
        {
            var tmp = bgmSource; bgmSource = bgmCrossSource; bgmCrossSource = tmp;
        }

        public void SetBGMVolume(float volume)
        {
            bgmVolume = Mathf.Clamp01(volume);
            if (bgmSource.isPlaying && !isBgmFading)
            {
                var info = audioData.GetBGM(currentBGM);
                if (info != null) bgmSource.volume = info.volume * bgmVolume * masterVolume;
            }
        }

        #endregion

        #region SE 関連（ランダムピッチあり）

        public AudioSource PlaySE(SEID id)
        {
            var info = audioData.GetSE(id);
            if (info == null || info.clip == null)
            {
                Debug.LogWarning($"SE の ID {id} が見つかりません");
                return null;
            }
            return PlaySEInternal(info, randomizePitch: true);
        }

        public AudioSource PlaySE(SEID id, bool preventDuplicate)
        {
            if (!preventDuplicate) return PlaySE(id);

            var info = audioData.GetSE(id);
            if (info == null || info.clip == null) return null;

            foreach (var s in seSources)
                if (s.isPlaying && s.clip == info.clip) return null;

            return PlaySEInternal(info, randomizePitch: true);
        }

        public AudioSource PlaySE(SEID id, float minSeconds)
        {
            var info = audioData.GetSE(id);
            if (info == null || info.clip == null)
            {
                Debug.LogWarning($"SE の ID {id} が見つかりません");
                return null;
            }
            foreach (var s in seSources)
                if (s.isPlaying && s.clip == info.clip && s.time < minSeconds) return null;

            return PlaySEInternal(info, randomizePitch: true);
        }

        private AudioSource PlaySEInternal(AudioData.SEInfo info, bool randomizePitch)
        {
            var src = GetAvailableAudioSource(seSources);
            if (src == null)
            {
                Debug.LogWarning("利用可能な SE AudioSource が見つかりません。");
                return null;
            }
            src.clip = info.clip;
            src.volume = info.volume * seVolume * masterVolume;
            src.pitch = randomizePitch ? ApplyPitchVariance(info.pitch) : info.pitch;
            src.loop = info.loop;
            src.Play();
            return src;
        }

        public void StopSE(AudioSource src)
        {
            if (src != null && seSources.Contains(src)) src.Stop();
        }

        public void StopAllSE()
        {
            foreach (var s in seSources) if (s.isPlaying) s.Stop();
        }

        public void SetSEVolume(float volume)
        {
            seVolume = Mathf.Clamp01(volume);
            foreach (var s in seSources)
                if (s.isPlaying)
                {
                    float ratio = seVolume > 0 ? s.volume / (seVolume * masterVolume) : 0f;
                    s.volume = ratio * seVolume * masterVolume;
                }
        }

        #endregion

        #region 環境音（雨など）管理──ランダムピッチなし ─────────────────────

        /// <summary>
        /// 外部の LoopingAudioSource が使う音量スケールを返す。
        /// BaseRainScript から呼んで、LoopingAudioSource の targetVolume にかける。
        /// </summary>
        public float GetAmbientVolumeScale() => ambientVolume * masterVolume;

        public void SetAmbientVolume(float volume)
        {
            ambientVolume = Mathf.Clamp01(volume);
        }

        #endregion

        #region 共通処理

        public void SetMasterVolume(float volume)
        {
            float prev = masterVolume;
            masterVolume = Mathf.Clamp01(volume);

            if (Mathf.Approximately(prev, 0f))
            {
                var info = audioData.GetBGM(currentBGM);
                if (bgmSource.isPlaying && !isBgmFading && info != null)
                    bgmSource.volume = info.volume * bgmVolume * masterVolume;
                foreach (var s in seSources)
                    if (s.isPlaying && s.clip != null)
                        s.volume = seVolume * masterVolume;
            }
            else
            {
                float ratio = masterVolume / prev;
                if (bgmSource.isPlaying && !isBgmFading) bgmSource.volume *= ratio;
                foreach (var s in seSources) if (s.isPlaying) s.volume *= ratio;
            }
        }

        private AudioSource GetAvailableAudioSource(List<AudioSource> pool)
        {
            foreach (var s in pool) if (!s.isPlaying) return s;

            AudioSource oldest = null; float longest = 0;
            foreach (var s in pool)
                if (s.time > longest) { longest = s.time; oldest = s; }
            return oldest;
        }

        /// <summary>ランダムピッチを適用（SE 専用）</summary>
        private float ApplyPitchVariance(float basePitch)
            => basePitch + Random.Range(-sePitchVariance, sePitchVariance);

        #endregion
    }
}