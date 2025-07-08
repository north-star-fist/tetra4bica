using System;
using System.Threading;
using Sergei.Safonov.StateMachinery;
using UnityEngine;

namespace Tetra4bica.Flow
{
    public class AppStateMenu : IState
    {
        public string StateId => throw new NotImplementedException();

        public Awaitable OnStateEnterAsync() => throw new NotImplementedException();
        public Awaitable OnStateExitAsync() => throw new NotImplementedException();
        public Awaitable<Type> StartAsync(CancellationToken cancelToken) => throw new NotImplementedException();
    }
}
