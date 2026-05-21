using System;
using System.Collections.Generic;
using UnityEngine;

namespace TechC.Core.Manager
{
    /// <summary>
    /// 敵1種類分の SE データ。EnemyData に持たせる ScriptableObject。
    ///
    /// 使い方：
    ///   Create > Audio > EnemyAudioData で作成。
    ///   EnemyData.AudioData にアサイン。
    ///   EnemyView からは AudioManager.I.PlayEnemySE(enemyAudioData, EnemyActionSEID.Attack) で鳴らす。
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyAudioData", menuName = "Audio/EnemyAudioData")]
    public class EnemyAudioData : ScriptableObject
    {
        [Serializable]
        public class EnemySEInfo
        {
            public EnemyActionSEID actionId;

            [Tooltip("このアクションで鳴らすクリップのリスト。複数登録するとランダムに選ばれる。")]
            public List<AudioClip> clips = new();

            [Range(0f, 1f)] public float volume = 1.0f;
            [Range(0f, 2f)] public float pitch  = 1.0f;
        }

        [Header("敵SE設定")]
        public List<EnemySEInfo> seList = new();

        /// <summary>アクション種別から SE 情報を取得する。なければ null。</summary>
        public EnemySEInfo GetSE(EnemyActionSEID actionId)
            => seList.Find(se => se.actionId == actionId);

        /// <summary>複数クリップからランダムに1つ選ぶ。</summary>
        public AudioClip GetRandomClip(EnemyActionSEID actionId)
        {
            var info = GetSE(actionId);
            if (info == null || info.clips == null || info.clips.Count == 0) return null;
            return info.clips[UnityEngine.Random.Range(0, info.clips.Count)];
        }
    }
}
