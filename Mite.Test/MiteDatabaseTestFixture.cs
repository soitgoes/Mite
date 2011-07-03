using System.Collections.Generic;
using Mite.Core;

using NUnit.Framework;

namespace Mite.Test
{
    [TestFixture]
    public class MiteDatabaseTestFixture
    {
        [SetUp]
        public void Setup()
        {
            
        }
        [Test]
        public void ShouldBeValidStateIfAllHashesAreTheSame()
        {
            var migrations = new[] {new Migration("2006", "asdf", ""), new Migration("2007", "fdsa", "")};
            var hash = new Dictionary<string, string>();
            foreach ( var mig in migrations)
                hash.Add(mig.Version, mig.Hash);
            var db = new MiteDatabase(migrations, hash);
            Assert.IsTrue(db.IsValidState());
        }
        [Test]
        public void ShouldNotBeValidStateIfAnyHashIsDifferent()
        {
            var migrations = new[] { new Migration("2006", "asdf", ""), new Migration("2007", "fdsa", "") };
            var hash = new Dictionary<string, string>();
            foreach (var mig in migrations)
                hash.Add(mig.Version, mig.Hash);
            hash["2006"] = "222";
            var db = new MiteDatabase(migrations, hash);
            Assert.IsFalse(db.IsValidState());
        }
        [Test]
        public void ShouldNotBeValidIfThereIsAMigrationGap()
        {
            var migrations = new List<Migration> { new Migration("2006", "asdf", ""), new Migration("2007", "fdsa", "") };
            var hash = new Dictionary<string, string>();
            foreach (var mig in migrations)
                hash.Add(mig.Version, mig.Hash);
            migrations.Insert(1, new Migration("2006-01", "fdsw", ""));
            var db = new MiteDatabase(migrations, hash);
            Assert.IsFalse(db.IsValidState());
        }

              /*
        [Test]
        public void UpMigrationWorksIfPartialDateString()
        {
            var container = new MigrationContainer(
                new Migration("2011-01-05", MigrationType.Up, "1"), 
                new Migration("2012-02-05", MigrationType.Up, "2"));
            var plan = container.GetMigrationPlan(null, "3000");
            Assert.AreEqual(plan.SqlToExecute.Length, 2);            
        }
        [Test]
        public void UpMigrationsIgnoreDownMigrations()
        {
            var container = new MigrationContainer(
                new Migration("2011-01-05", MigrationType.Up, "1"), 
                new Migration("2012-02-05", MigrationType.Up, "2"), 
                new Migration("2011-02-04", MigrationType.Down, "2"));
            var plan = container.GetMigrationPlan(null, "3000");
            Assert.AreEqual(plan.SqlToExecute.Length, 2);
        }

        [Test]
        public void UpMigrationsAreOrderedAscending()
        {
            var migrations = new[]
                                 {
                                     new Migration("2011-01-05", MigrationType.Up, "1"),
                                     new Migration("2012-02-05", MigrationType.Up, "3"),
                                     new Migration("2012-02-05T06-05-08Z", MigrationType.Up, "4"),
                                     new Migration("2011-02-04", MigrationType.Up, "2")
                                 };
            var container = new MigrationContainer(migrations);
            var plan = container.GetMigrationPlan(null, "3000");
            Assert.AreEqual(plan.SqlToExecute , new[] {"1", "2", "3", "4"});
        }
               */
    }
}
