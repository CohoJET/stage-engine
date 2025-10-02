using StageEngine.Core.Data;
using StageEngine.Core.Utility;

namespace StageEngine.Core.Game.Session
{
    public class SessionManager : Singleton<SessionManager>
    {
        public SessionData Data;

        public T GetSessionData<T>() where T : SessionData
        {
            return (T)Data;
        }
    }
}
