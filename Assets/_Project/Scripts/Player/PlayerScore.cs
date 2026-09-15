using Game.Services;
using PurrNet;
using PurrNet.Pooling;
using PurrNet.Prediction;
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

        private static readonly StringBuilder sb = new StringBuilder();
        public struct ScoreState : IPredictedData<ScoreState>
        {
            public DisposableDictionary<PlayerID, ScoreData> Score;

            public void Dispose()
            {
                Score.Dispose();
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
            if (currentState.Score.TryGetValue(player, out var score))
                currentState.Score[player] = score.AddKill();
        }

        public void Sim_AddDeath(PlayerID player)
        {
            if (currentState.Score.TryGetValue(player, out var score))
                currentState.Score[player] = score.AddDeath();
        }

        protected override ScoreState GetInitialState()
        {
            return new ScoreState() { Score = DisposableDictionary<PlayerID, ScoreData>.Create() };
        }
    }
}
