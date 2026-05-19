using System;
using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// シールド量を管理するモデル。
    ///
    /// 変更点：
    ///   - OnShieldAdded   : シールドが加算されたとき発火（付与演出用）
    ///   - OnShieldChanged : 吸収・リセット後の確定値で発火（通常更新用）
    ///   の2イベントに分離。
    ///
    ///   ShieldView は OnShieldAdded で一度付与量を表示してから
    ///   OnShieldChanged で最終値に更新することで、
    ///   「張ったターンで即破壊」でも付与アニメが見える。
    /// </summary>
    public class ShieldModel
    {
        private int current;

        public int Current => current;
        public bool HasShield => current > 0;

        /// <summary>シールドが加算されたとき発火。引数は加算後の値。付与演出用。</summary>
        public event Action<int> OnShieldAdded;

        /// <summary>シールドが吸収・リセット・Setで変化したとき発火。引数は変化後の値。</summary>
        public event Action<int> OnShieldChanged;

        public ShieldModel(int initial = 0)
        {
            current = Mathf.Max(0, initial);
        }

        /// <summary>シールドを加算する。</summary>
        public void Add(int amount)
        {
            if (amount <= 0) return;
            current += amount;
            OnShieldAdded?.Invoke(current);   // 付与演出イベント
            OnShieldChanged?.Invoke(current); // 通常更新イベント
        }

        /// <summary>
        /// ダメージをシールドで吸収する。
        /// 余剰ダメージを返す。
        /// </summary>
        public int AbsorbDamage(int damage)
        {
            if (damage <= 0) return 0;

            int absorbed = Mathf.Min(current, damage);
            current -= absorbed;
            OnShieldChanged?.Invoke(current); // 吸収後の確定値を通知

            return damage - absorbed;
        }

        /// <summary>シールドをリセットする。</summary>
        public void Reset()
        {
            current = 0;
            OnShieldChanged?.Invoke(current);
        }

        /// <summary>シールドを直接セットする。</summary>
        public void Set(int value)
        {
            current = Mathf.Max(0, value);
            OnShieldChanged?.Invoke(current);
        }
    }
}