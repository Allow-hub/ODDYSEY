using System.Collections.Generic;
using UnityEngine;

namespace TechC.Core.Manager
{
    #region 列挙型

    /// <summary>
    /// BGM用の列挙型
    /// </summary>
    public enum BGMID
    {
        None = -1,
        Title,
        Stage1,
        Stage2,
    }

    /// <summary>
    /// SE用（共通のSE）
    /// </summary>
    public enum SEID
    {
        None = -1,
        ButtonClick,
        MenuOpen,
        MenuClose,
    }

    #endregion


    /// <summary>
    /// 共通音声データを管理するScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "AudioData", menuName = "Audio/AudioData")]
    public class AudioData : ScriptableObject
    {
        [System.Serializable]
        public class BGMInfo
        {
            public BGMID id;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 1.0f;
            [Range(0f, 2f)] public float pitch = 1.0f;
            public bool loop = true;
            [Range(0f, 5f)] public float fadeInTime = 0.5f;
            [Range(0f, 5f)] public float fadeOutTime = 0.5f;
        }

        [System.Serializable]
        public class SEInfo
        {
            public SEID id;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 1.0f;
            [Range(0f, 2f)] public float pitch = 1.0f;
            public bool loop = false;
        }

        [Header("BGM設定")]
        public List<BGMInfo> bgmList = new List<BGMInfo>();

        [Header("共通SE設定")]
        public List<SEInfo> seList = new List<SEInfo>();

        /// <summary>
        /// IDからBGMデータを取得
        /// </summary>
        public BGMInfo GetBGM(BGMID id)
        {
            return bgmList.Find(bgm => bgm.id == id);
        }

        /// <summary>
        /// IDからSEデータを取得
        /// </summary>
        public SEInfo GetSE(SEID id)
        {
            return seList.Find(se => se.id == id);
        }
    }
}
