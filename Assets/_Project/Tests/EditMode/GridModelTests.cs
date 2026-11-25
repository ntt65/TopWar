using System;
using NUnit.Framework;
using TopWar.Core.Grid;
using TopWar.Core.Units;

namespace TopWar.Core.Tests
{
    public class GridModelTests
    {
        private static readonly UnitData ArmyLv1 = new UnitData(UnitType.Army, 1);
        private static readonly UnitData NavyLv1 = new UnitData(UnitType.Navy, 1);

        [Test]
        public void NewGrid_IsEmpty()
        {
            var grid = new GridModel();
            Assert.That(grid.Width, Is.EqualTo(5));
            Assert.That(grid.Height, Is.EqualTo(5));
            Assert.That(grid.EmptyCells().Count, Is.EqualTo(25));
        }

        [Test]
        public void TryPlace_OnOccupiedCell_Fails()
        {
            var grid = new GridModel();
            var pos = new GridPos(1, 1);
            Assert.That(grid.TryPlace(pos, ArmyLv1), Is.True);
            Assert.That(grid.TryPlace(pos, NavyLv1), Is.False);
            Assert.That(grid.Get(pos), Is.EqualTo(ArmyLv1));
        }

        [Test]
        public void Move_ToEmptyCell_Moves()
        {
            var grid = new GridModel();
            var from = new GridPos(0, 0);
            var to = new GridPos(2, 3);
            grid.TryPlace(from, ArmyLv1);

            Assert.That(grid.Move(from, to), Is.EqualTo(MoveResult.Moved));
            Assert.That(grid.IsEmpty(from), Is.True);
            Assert.That(grid.Get(to), Is.EqualTo(ArmyLv1));
        }

        [Test]
        public void Move_OntoSameUnit_Merges()
        {
            var grid = new GridModel();
            var from = new GridPos(0, 0);
            var to = new GridPos(1, 0);
            grid.TryPlace(from, ArmyLv1);
            grid.TryPlace(to, ArmyLv1);

            Assert.That(grid.Move(from, to), Is.EqualTo(MoveResult.Merged));
            Assert.That(grid.IsEmpty(from), Is.True);
            Assert.That(grid.Get(to), Is.EqualTo(new UnitData(UnitType.Army, 2)));
        }

        [Test]
        public void Move_OntoDifferentUnit_Swaps()
        {
            var grid = new GridModel();
            var from = new GridPos(0, 0);
            var to = new GridPos(1, 0);
            grid.TryPlace(from, ArmyLv1);
            grid.TryPlace(to, NavyLv1);

            Assert.That(grid.Move(from, to), Is.EqualTo(MoveResult.Swapped));
            Assert.That(grid.Get(from), Is.EqualTo(NavyLv1));
            Assert.That(grid.Get(to), Is.EqualTo(ArmyLv1));
        }

        [Test]
        public void Move_MaxLevelOntoMaxLevel_Swaps()
        {
            var grid = new GridModel();
            var max = new UnitData(UnitType.Army, GameRules.MaxLevel);
            grid.TryPlace(new GridPos(0, 0), max);
            grid.TryPlace(new GridPos(1, 0), max);

            Assert.That(grid.Move(new GridPos(0, 0), new GridPos(1, 0)), Is.EqualTo(MoveResult.Swapped));
            Assert.That(grid.Get(new GridPos(1, 0)), Is.EqualTo(max));
        }

        [Test]
        public void Move_FromEmptyOrOutOfBounds_IsInvalid()
        {
            var grid = new GridModel();
            Assert.That(grid.Move(new GridPos(0, 0), new GridPos(1, 0)), Is.EqualTo(MoveResult.Invalid));

            grid.TryPlace(new GridPos(0, 0), ArmyLv1);
            Assert.That(grid.Move(new GridPos(0, 0), new GridPos(5, 0)), Is.EqualTo(MoveResult.Invalid));
        }

        [Test]
        public void Move_ToSameCell_IsNone()
        {
            var grid = new GridModel();
            grid.TryPlace(new GridPos(2, 2), ArmyLv1);
            Assert.That(grid.Move(new GridPos(2, 2), new GridPos(2, 2)), Is.EqualTo(MoveResult.None));
        }

        [Test]
        public void Changed_FiresOnMutation()
        {
            var grid = new GridModel();
            var count = 0;
            grid.Changed += () => count++;

            grid.TryPlace(new GridPos(0, 0), ArmyLv1);
            grid.Move(new GridPos(0, 0), new GridPos(1, 0));

            Assert.That(count, Is.EqualTo(2));
        }

        [Test]
        public void Expand_KeepsUnitsAndAddsCells()
        {
            var grid = new GridModel();
            grid.TryPlace(new GridPos(4, 4), ArmyLv1);

            grid.Expand(7, 7);

            Assert.That(grid.Width, Is.EqualTo(7));
            Assert.That(grid.Get(new GridPos(4, 4)), Is.EqualTo(ArmyLv1));
            Assert.That(grid.EmptyCells().Count, Is.EqualTo(48));
        }

        [Test]
        public void Expand_BeyondMaxOrShrink_Throws()
        {
            var grid = new GridModel();
            Assert.Throws<ArgumentOutOfRangeException>(() => grid.Expand(8, 8));
            Assert.Throws<ArgumentException>(() => grid.Expand(4, 4));
        }
    }
}
