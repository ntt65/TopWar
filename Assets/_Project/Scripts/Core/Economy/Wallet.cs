using System;

namespace TopWar.Core.Economy
{
    /// <summary>스테이지 안에서 쓰는 골드.</summary>
    public sealed class Wallet
    {
        public int Gold { get; private set; }

        public event Action Changed;

        public Wallet(int startingGold = 0)
        {
            if (startingGold < 0) throw new ArgumentOutOfRangeException(nameof(startingGold));
            Gold = startingGold;
        }

        public bool CanAfford(int amount) => amount <= Gold;

        public void Add(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            Gold += amount;
            Changed?.Invoke();
        }

        public bool TrySpend(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (!CanAfford(amount)) return false;

            Gold -= amount;
            Changed?.Invoke();
            return true;
        }
    }
}
