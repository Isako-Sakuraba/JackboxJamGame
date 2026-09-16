using Game.Services;
using PurrNet;
using PurrNet.Pooling;
using PurrNet.Prediction;
using System.Collections.Generic;
using System.Text;

namespace Game.Player
{
    public sealed class PlayerScore : PredictedIdentity<PlayerScore.ScoreState>
    {
        public struct ScoreData
        {
            public int Kills;
            public int Deaths;

            public ScoreData AddKill() => new ScoreData() { Kills = this.Kills + 1, Deaths = this.Deaths };
            public ScoreData AddDeath() => new ScoreData() { Kills = this.Kills, Deaths = this.Deaths + 1 };
        }

        public readonly struct ScoreResult
        {
            public readonly PlayerID Player;
            public readonly ScoreData Score;

            public ScoreResult(PlayerID player, ScoreData score)
            {
                Player = player;
                Score = score;
            }
        }

        private static readonly StringBuilder sb = new StringBuilder();
        public struct ScoreState : IPredictedData<ScoreState>
        {
            public DisposableDictionary<PlayerID, ScoreData> Score;
            public DisposableList<PlayerID> EligiblePlayers;
            public bool RoundFinalized;

            public void Dispose()
            {
                Score.Dispose();
                EligiblePlayers.Dispose();
            }

            public override string ToString()
            {
                if (Score.isDisposed) return string.Empty;

                sb.Clear();
                foreach ((PlayerID id, ScoreData data) in Score)
                {
                    sb.AppendLine($"ID: {id} | Kills: {data.Kills} | Deaths: {data.Deaths}");
                }
                return sb.ToString();
            }
        }

        public DisposableDictionary<PlayerID, ScoreData> Score => currentState.Score;

        protected override void LateAwake()
        {
            base.LateAwake();

            ServiceLocator.Register(this);
        }

        public void Sim_InitializeFromPlayers()
        {
            var players = predictionManager.players;

            foreach (var player in players.players)
            {
                currentState.Score[player] = new ScoreData();
            }
        }

        public void Sim_AddKill(PlayerID player)
        {
            currentState.Score.TryGetValue(player, out var score);
            currentState.Score[player] = score.AddKill();
        }

        public void Sim_AddDeath(PlayerID player)
        {
            currentState.Score.TryGetValue(player, out var score);
            currentState.Score[player] = score.AddDeath();
        }

        public void Sim_FinalizeRound()
        {
            currentState.EligiblePlayers.Clear();

            foreach (PlayerID player in predictionManager.players.players)
            {
                if (!currentState.Score.ContainsKey(player))
                    currentState.Score[player] = new ScoreData();

                currentState.EligiblePlayers.Add(player);
            }

            currentState.RoundFinalized = true;
        }

        public bool TryGetVerifiedWinners(out List<ScoreResult> winners)
        {
            winners = new List<ScoreResult>();
            if (!verifiedState.HasValue || !verifiedState.Value.RoundFinalized)
                return false;

            ScoreState state = verifiedState.Value;
            int bestKills = int.MinValue;
            int bestDeaths = int.MaxValue;

            foreach (PlayerID player in state.EligiblePlayers)
            {
                if (!state.Score.TryGetValue(player, out ScoreData score))
                    continue;

                if (score.Kills > bestKills || score.Kills == bestKills && score.Deaths < bestDeaths)
                {
                    winners.Clear();
                    bestKills = score.Kills;
                    bestDeaths = score.Deaths;
                    winners.Add(new ScoreResult(player, score));
                }
                else if (score.Kills == bestKills && score.Deaths == bestDeaths)
                {
                    winners.Add(new ScoreResult(player, score));
                }
            }

            winners.Sort((a, b) => a.Player.id.value.CompareTo(b.Player.id.value));
            return true;
        }

        public List<ScoreResult> GetViewScores()
        {
            var scores = new List<ScoreResult>();
            if (viewState.Score.isDisposed)
                return scores;

            foreach ((PlayerID player, ScoreData score) in viewState.Score)
                scores.Add(new ScoreResult(player, score));

            scores.Sort((a, b) => a.Player.id.value.CompareTo(b.Player.id.value));
            return scores;
        }

        protected override ScoreState GetInitialState()
        {
            return new ScoreState
            {
                Score = DisposableDictionary<PlayerID, ScoreData>.Create(),
                EligiblePlayers = DisposableList<PlayerID>.Create()
            };
        }

        protected override void Destroyed()
        {
            if (ServiceLocator.TryGet<PlayerScore>(out var registered) && ReferenceEquals(registered, this))
                ServiceLocator.Unregister<PlayerScore>();
        }
    }
}
