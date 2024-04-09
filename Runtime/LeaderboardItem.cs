using System.Collections;
using System.Collections.Generic;

namespace HexTecGames.LeaderboardSystem
{
	[System.Serializable]
	public class LeaderboardItem
	{
		public string name;
		public int rank;
		public int score;
        public bool isHighlighted;

        public LeaderboardItem(string name, int rank, int score, bool isHighlighted = false)
        {
            this.name = name;
            this.rank = rank;
            this.score = score;
            this.isHighlighted = isHighlighted;
        }

        public override string ToString()
        {
            return $"R: {rank}, Name: {name}, Score: {score}";
        }
    }
}