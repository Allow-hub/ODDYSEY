using System.Collections.Generic;

namespace TechC.ODDESEY.Result
{
    /// <summary>
    /// ランク定義。スコアのしきい値と表示文字で管理する。
    /// </summary>
    public enum Rank { S, A, B, C, D }

    /// <summary>
    /// ミッション1件の結果。
    /// 将来のミッション追加はここにエントリを追加するだけ。
    /// </summary>
    public class MissionResult
    {
        public string Label;      // 表示名（例: バトル勝利）
        public int Count;      // 達成数
        public int ScoreGain;  // このミッションのスコア加算量

        public MissionResult(string label, int count, int scoreGain)
        {
            Label = label;
            Count = count;
            ScoreGain = scoreGain;
        }
    }

    /// <summary>
    /// リザルト画面に渡すデータ。
    /// MainManager または GameContext が組み立てて渡す。
    /// </summary>
    public class ResultData
    {
        public bool IsCleared;                      // ゲームクリアか
        public List<MissionResult> Missions = new();// ミッション結果リスト

        // ─── 集計 ────────────────────────────────────────────────────────

        /// <summary>全ミッションのスコア合計</summary>
        public int TotalScore
        {
            get
            {
                int total = 0;
                foreach (var m in Missions) total += m.ScoreGain;
                return total;
            }
        }

        /// <summary>
        /// スコアからランクを算出する。
        /// しきい値は後から調整しやすいよう定数で管理。
        /// </summary>
        public Rank CalcRank()
        {
            int score = TotalScore;
            if (score >= RankThreshold.S) return Rank.S;
            if (score >= RankThreshold.A) return Rank.A;
            if (score >= RankThreshold.B) return Rank.B;
            if (score >= RankThreshold.C) return Rank.C;
            return Rank.D;
        }
    }

    /// <summary>ランクしきい値（後から調整しやすいよう分離）</summary>
    public static class RankThreshold
    {
        public const int S = 5000;
        public const int A = 3000;
        public const int B = 1500;
        public const int C = 500;
    }

    /// <summary>ミッションのスコア単価（後から調整しやすいよう分離）</summary>
    public static class MissionScore
    {
        public const int PerBattleWin = 1000; // バトル勝利1回あたり
        // 将来ミッションが増えたらここに追加
        // public const int PerEventClear = 300;
        // public const int PerCardCollected = 100;
    }
}