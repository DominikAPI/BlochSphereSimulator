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

    /// <summary>
    /// A szimulátor modellje. Tárolja a kezdőállapotokat és a kapusort; az állapotok mindig
    /// ezekből számolódnak, így a kapusor tetszőleges módosítása (beszúrás, törlés, visszavonás)
    /// nem hagy inkonzisztens állapotot maga után.
    /// </summary>
    public sealed class BlochSimulator
    {
        private readonly List<StateEntry> _states = new List<StateEntry>();
        private readonly List<Gate> _circuit = new List<Gate>();
        private int _nextId = 1;

        /// <summary>Bármilyen változás után sül el (állapot hozzáadás/törlés/módosítás, kapusor-változás).</summary>
        public event Action Changed;

        public IReadOnlyList<StateEntry> States => _states;
        public IReadOnlyList<Gate> Circuit => _circuit;

        // --- Állapotok ---

        public StateEntry AddState(QubitState initial, string label = null)
        {
            var entry = new StateEntry(_nextId, label ?? ("ψ" + _nextId), initial);
            _nextId++;
            _states.Add(entry);
            Changed?.Invoke();
            return entry;
        }

        public bool RemoveState(int id)
        {
            int idx = _states.FindIndex(e => e.Id == id);
            if (idx < 0) return false;
            _states.RemoveAt(idx);
            Changed?.Invoke();
            return true;
        }

        public void SetInitial(int id, QubitState initial)
        {
            Find(id).Initial = initial;
            Changed?.Invoke();
        }

        // --- Kapusor ---

        public void AppendGate(Gate gate) => InsertGate(_circuit.Count, gate);

        public void InsertGate(int index, Gate gate)
        {
            if (gate == null) throw new ArgumentNullException(nameof(gate));
            _circuit.Insert(index, gate);
            Changed?.Invoke();
        }

        public void RemoveGateAt(int index)
        {
            _circuit.RemoveAt(index);
            Changed?.Invoke();
        }

        /// <summary>Visszavonás: az utolsó kapu törlése. Üres kapusornál false.</summary>
        public bool UndoLastGate()
        {
            if (_circuit.Count == 0) return false;
            RemoveGateAt(_circuit.Count - 1);
            return true;
        }

        public void ClearCircuit()
        {
            _circuit.Clear();
            Changed?.Invoke();
        }

        // --- Lekérdezések ---

        /// <summary>Az állapot az első <paramref name="steps"/> kapu alkalmazása után (0 = kezdőállapot).</summary>
        public QubitState StateAfter(int id, int steps)
        {
            if (steps < 0 || steps > _circuit.Count)
                throw new ArgumentOutOfRangeException(nameof(steps));

            QubitState s = Find(id).Initial;

            for (int i = 0; i < steps; i++)
            {
                s = _circuit[i].Apply(s);
            }

            return s;
        }

        public QubitState FinalState(int id) => StateAfter(id, _circuit.Count);

        /// <summary>Az állapotok a kapusor minden lépése után; hossza Circuit.Count + 1.</summary>
        public IReadOnlyList<QubitState> Trajectory(int id)
        {
            var result = new List<QubitState>(_circuit.Count + 1);
            QubitState s = Find(id).Initial;
            result.Add(s);

            foreach (Gate g in _circuit)
            {
                s = g.Apply(s);
                result.Add(s);
            }

            return result;
        }

        /// <summary>Az első <paramref name="steps"/> kapu eredő mátrixa (fordított sorrendű szorzat).</summary>
        public Matrix2x2 TotalMatrix(int steps)
        {
            if (steps < 0 || steps > _circuit.Count)
                throw new ArgumentOutOfRangeException(nameof(steps));

            Matrix2x2 m = Matrix2x2.Identity;

            for (int i = 0; i < steps; i++)
            {
                m = _circuit[i].Matrix * m;
            }

            return m;
        }

        private StateEntry Find(int id)
        {
            StateEntry e = _states.Find(x => x.Id == id);
            if (e == null) throw new ArgumentException("State not found: " + id);
            return e;
        }
    }
}
