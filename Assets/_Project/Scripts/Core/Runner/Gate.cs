using System;
using TopWar.Core.Units;

namespace TopWar.Core.Runner
{
    public readonly struct Gate
    {
        private Gate(GateKind kind, int value = 0, UnitType unitType = default)
        {
            Kind = kind;
            Value = value;
            UnitType = unitType;
        }

        public GateKind Kind { get; }
        public int Value { get; }
        public UnitType UnitType { get; }

        public static Gate Add(int count)
        {
            if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));
            return new Gate(GateKind.Add, count);
        }

        public static Gate Multiply(int factor)
        {
            if (factor < 2) throw new ArgumentOutOfRangeException(nameof(factor));
            return new Gate(GateKind.Multiply, factor);
        }

        public static Gate Subtract(int count)
        {
            if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));
            return new Gate(GateKind.Subtract, count);
        }

        public static Gate Merge() => new Gate(GateKind.Merge);

        public static Gate ChangeType(UnitType type) => new Gate(GateKind.ChangeType, unitType: type);

        public void ApplyTo(Squad squad)
        {
            if (squad == null) throw new ArgumentNullException(nameof(squad));

            switch (Kind)
            {
                case GateKind.Add:
                    squad.Add(Value);
                    break;
                case GateKind.Multiply:
                    squad.Multiply(Value);
                    break;
                case GateKind.Subtract:
                    squad.Remove(Value);
                    break;
                case GateKind.Merge:
                    squad.Merge();
                    break;
                case GateKind.ChangeType:
                    squad.ChangeType(UnitType);
                    break;
            }
        }
    }
}
