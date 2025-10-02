using System.Collections.Generic;
using System.Linq;
using System.Text;
using StageEngine.Fiasco.Game.Session;

namespace StageEngine.Fiasco.Utility
{
    public static class FiascoAgentsMessageHelper
    {
        public static string FormatDicePool(List<Die> dicePool)
        {
            var availableDice = dicePool.Where(d => !d.IsUsed).Select(d => d.Value);
            return $"Available: {string.Join(", ", availableDice)}";
        }

        public static string FormatCards(List<Card> cards)
        {
            var builder = new StringBuilder();

            for (int i = 0; i < cards.Count; i++)
            {
                builder.AppendLine($"  {i + 1}. {cards[i].ToString()}");
            }

            return builder.ToString();
        }
    }
}
