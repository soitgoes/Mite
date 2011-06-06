using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mite.Core;
using NUnit.Framework;

namespace Mite.Test
{
    [TestFixture]
    public class MigrationHelperTestFixture
    {
        [SetUp]
        public void Setup()
        {
            
        }
        
        [Test]
        public void UpMigrationWorksIfGreaterThanAllVersions()
        {
            var container = new MigrationContainer(new Migration("2011-01-05", MigrationType.Up, "1"), new Migration("2012-02-05", MigrationType.Up, "2"));
            var plan = container.GetMigrationPlan(null, "3000");
            Assert.AreEqual(plan.SqlToExecute.Length, 2);
        }
        [Test]
        public void UpMigrationsIgnoreDownMigrations()
        {
            var container = new MigrationContainer(new Migration("2011-01-05", MigrationType.Up, "1"), new Migration("2012-02-05", MigrationType.Up, "2"), new Migration("2011-02-04", MigrationType.Down, "2"));
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
                                     new Migration("2011-02-04", MigrationType.Up, "2")
                                 };
            var container = new MigrationContainer(migrations);
            var plan = container.GetMigrationPlan(null, "3000");
            Assert.AreEqual(plan.SqlToExecute , new[] {"1", "2", "3"});
        }
    }
}
