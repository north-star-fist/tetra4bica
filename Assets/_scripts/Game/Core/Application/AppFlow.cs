using Sergei.Safonov.StateMachinery;
using VContainer;

namespace Tetra4bica.Flow
{

    /// <summary>
    /// Application Flow state machine.
    /// </summary>
    public class AppFlow : VContainerStateMachine, IAppFlow
    {
        public AppFlow(IObjectResolver resolver) : base(resolver) { }
    }
}
