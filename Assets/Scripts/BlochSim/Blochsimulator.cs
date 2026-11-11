using System;
using System.Collections.Generic;

namespace BlochSim.Model
{
    public sealed class StateEntry
    {
        public int Id { get; }
        public string Label { get; set; }
        public QubitState Initial { get; set; }

        public StateEntry(int id, string label, QubitState initial)
        {
            Id = id; Label = label; Initial = initial;
        }
    }

    /// <summary>The kind of model change (the view uses this to decide whether to animate or jump).</summary>
    public enum ModelChangeKind
    {
        StateAdded,
        StateRemoved,
        InitialChanged,
        GateAppended,
        CircuitEdited
    }

    /// <summary>
    /// The simulator model. Stores the initial states and the gate sequence; states are always
    /// computed from these, so any modification of the gate sequence (insert, delete, undo)
    /// leaves no inconsistent state behind.
    /// </summary>
    public sealed class BlochSimulator
    {
        private readonly List<StateEntry> _states = new List<StateEntry>();
        private readonly List<Gate> _circuit = new List<Gate>();
        private int _nextId = 1;

        /// <summary>Fires after any change, with the kind of change.</summary>
        public event Action<ModelChangeKind> Changed;

        public IReadOnlyList<StateEntry> States => _states;
        public IReadOnlyList<Gate> Circuit => _circuit;

        // --- States ---

        public StateEntry AddState(QubitState initial, string label = null)
        {
            var entry = new StateEntry(_nextId, label ?? ("ψ" + _nextId), initial);
            _nextId++;
            _states.Add(entry);
            Changed?.Invoke(ModelChangeKind.StateAdded);
            return entry;
        }

        public bool RemoveState(int id)
        {
            int idx = _states.FindIndex(e => e.Id == id);
            if (idx < 0) return false;
            _states.RemoveAt(idx);
            Changed?.Invoke(ModelChangeKind.StateRemoved);
            return true;
        }

        public void SetInitial(int id, QubitState initial)
        {
            Find(id).Initial = initial;
            Changed?.Invoke(ModelChangeKind.InitialChanged);
        }

        // --- Gates ---

        public void AppendGate(Gate gate) => InsertGate(_circuit.Count, gate);

        public void InsertGate(int index, Gate gate)
        {
            if (gate == null) throw new ArgumentNullException(nameof(gate));
            bool atEnd = index == _circuit.Count;
            _circuit.Insert(index, gate);
            Changed?.Invoke(atEnd ? ModelChangeKind.GateAppended : ModelChangeKind.CircuitEdited);
        }

        public void RemoveGateAt(int index)
        {
            _circuit.RemoveAt(index);
            Changed?.Invoke(ModelChangeKind.CircuitEdited);
        }

        /// <summary>Undo: removes the last gate. Returns false if the gate sequence is empty.</summary>
        public bool UndoLastGate()
        {
            if (_circuit.Count == 0) return false;
            RemoveGateAt(_circuit.Count - 1);
            return true;
        }

        public void ClearCircuit()
        {
            _circuit.Clear();
            Changed?.Invoke(ModelChangeKind.CircuitEdited);
        }


        /// <summary>The state after applying the first <paramref name="steps"/> gates (0 = initial state).</summary>
        public QubitState StateAfter(int id, int steps)
        {
            if (steps < 0 || steps > _circuit.Count)
                throw new ArgumentOutOfRangeException(nameof(steps));

            QubitState s = Find(id).Initial;

            for (int i = 0; i < steps; i++)
                s = _circuit[i].Apply(s);

            return s;
        }

        public QubitState FinalState(int id) => StateAfter(id, _circuit.Count);

        /// <summary>A new state that ends up at the desired point at the end of the gate sequence (at the displayed position).</summary>
        public StateEntry AddStateAtCurrent(QubitState desired, string label = null)
            => AddState(BackPropagate(desired), label);

        public void SetCurrentState(int id, QubitState desired)
        {
            Find(id).Initial = BackPropagate(desired);
            Changed?.Invoke(ModelChangeKind.InitialChanged);
        }

        // The inverse of the gate sequence (in reverse order, per gate the conjugate transpose).
        private QubitState BackPropagate(QubitState desired)
        {
            QubitState s = desired;
            for (int i = _circuit.Count - 1; i >= 0; i--)
                s = _circuit[i].Matrix.Dagger().Apply(s);
            return s;
        }

        private StateEntry Find(int id)
        {
            StateEntry e = _states.Find(x => x.Id == id);
            return e ?? throw new ArgumentException("No such state: " + id);
        }
    }
}