using System;
using PurrNet.Prediction;

namespace Game.Player.Movement.States
{
    public interface IPredictedMachine<EState>
        where EState : Enum
    {
        public void ChangeState(EState state);
    }

    public interface IPredictedMachineState<EState, TInput, TState>
        where EState : Enum
        where TInput : IPredictedData<TInput>
        where TState : IPredictedData<TState>
    {
        public void TryTransition(in TInput input, ref TState state, float delta, IPredictedMachine<EState> machine);
        public void Tick(in TInput input, ref TState state, float delta);
    }
}
