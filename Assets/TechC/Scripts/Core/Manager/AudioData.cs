using System.Collections.Generic;
using UnityEngine;

namespace TechC.Core.Manager
{
    #region 列挙型

    public enum BGMID
    {
        None = -1,
        Title = 0,
        Map = 1,
        Battle = 2,
        StageSelect = 3,
        BossBattle = 4,
        Event = 5,
        Reward = 6,
        Result = 7,
    }

    public enum SEID
    {
        None = -1,
        ButtonClick,
        MenuOpen,
        MenuClose,
        PlayerAttack,
    }

    /// <summary>
    /// 敵のアクション種別。
    /// 敵の種類に関わらず共通のアクション定義として使う。
    /// </summary>
    public enum EnemyActionSEID
    {
        None = -1,
        AttackReady,  // 攻撃準備（モーション開始）
        Attack,       // 攻撃（ヒット判定フレーム）
        Dodge,        // 回避
        Hit,          // 被弾
        Death,        // 死亡
    }

    #endregion

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

        public BGMInfo GetBGM(BGMID id)
            => bgmList.Find(bgm => bgm.id == id);

        public SEInfo GetSE(SEID id)
            => seList.Find(se => se.id == id);
    }
}
