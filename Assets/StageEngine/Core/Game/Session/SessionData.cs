using System.Collections.ObjectModel;
using StageEngine.Core.Players;

namespace StageEngine.Core.Data
{
    public abstract class SessionData
    {
        public ObservableCollection<Player> Players { get; set; } = new ObservableCollection<Player>();
    }
}
