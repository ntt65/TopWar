using System;
using NUnit.Framework;
using TopWar.Core.Economy;
using TopWar.Core.Grid;
using TopWar.Core.Units;

namespace TopWar.Core.Tests
{
    public class SummonServiceTests
    {
        [Test]
        public void Summon_PlacesLv1UnitAndSpendsGold()
        {
            var grid = new GridModel();
            var wallet = new Wallet(100);
            var summon = new SummonService(grid, wallet, baseCost: 10, costStep: 2, new Random(1));

            var result = summon.TrySummon(UnitType.Navy, out var pos);

            Assert.That(result, Is.EqualTo(SummonResult.Success));
            Assert.That(grid.Get(pos), Is.EqualTo(new UnitData(UnitType.Navy, 1)));
            Assert.That(wallet.Gold, Is.EqualTo(90));
        }

        [Test]
        public void Cost_IncreasesPerSummon()
        {
            var summon = new SummonService(new GridModel(), new Wallet(1000), 10, 2, new Random(1));

            Assert.That(summon.CurrentCost, Is.EqualTo(10));
            summon.TrySummon(UnitType.Army, out _);
            Assert.That(summon.CurrentCost, Is.EqualTo(12));
            summon.TrySummon(UnitType.AirForce, out _);
            Assert.That(summon.CurrentCost, Is.EqualTo(14));
        }

        [Test]
        public void NotEnoughGold_DoesNothing()
        {
            var grid = new GridModel();
            var wallet = new Wallet(5);
            var summon = new SummonService(grid, wallet, 10, 2, new Random(1));

            Assert.That(summon.TrySummon(UnitType.Army, out _), Is.EqualTo(SummonResult.NotEnoughGold));
            Assert.That(wallet.Gold, Is.EqualTo(5));
            Assert.That(grid.EmptyCells().Count, Is.EqualTo(25));
            Assert.That(summon.SummonCount, Is.EqualTo(0));
        }

        [Test]
        public void GridFull_DoesNotSpendGold()
        {
            var grid = new GridModel(1, 1);
            var wallet = new Wallet(100);
            var summon = new SummonService(grid, wallet, 10, 2, new Random(1));

            summon.TrySummon(UnitType.Army, out _);
            Assert.That(summon.TrySummon(UnitType.Army, out _), Is.EqualTo(SummonResult.GridFull));
            Assert.That(wallet.Gold, Is.EqualTo(90));
        }
    }
}
