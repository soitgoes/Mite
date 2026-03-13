using System.Collections.Generic;
using System.Linq;
using Mite.Core;
using Moq;
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
        public void ShouldAllowStepDownWhenDirty()
        {
            var dbRepo = new Mock<IDatabaseRepository>();
            var tracker = new Mock<IMigrationTracker>();
            tracker.Setup(t => t.Version).Returns("1.0.0");
            tracker.Setup(t => t.IsValidState()).Returns(false);
            tracker.Setup(t => t.GetMigrationDictionary()).Returns(new Dictionary<string, Migration>());
            var migrator = new Migrator(tracker.Object, dbRepo.Object);
            migrator.StepDown();
            Assert.That(true, "Step up should not require that the database be clean");
        }
        
        [Test]
        public void ShouldBeValidStateIfAllHashesAreTheSame()
        {
            var migrations = new[] {new Migration("2006", "asdf", ""), new Migration("2007", "fdsa", "")};
            var hash = new Dictionary<string, string>();
            foreach ( var mig in migrations)
                hash.Add(mig.Version, mig.Hash);
            var db = new MigrationTracker(migrations, hash);
            Assert.That(db.IsHashMismatch(), Is.False);
            Assert.That(db.IsValidState(), Is.True);
        }
        [Test]
        public void ShouldNotBeValidStateIfAnyHashIsDifferent()
        {
            var migrations = new[] { new Migration("2006", "asdf", ""), new Migration("2007", "fdsa", "") };
            var hash = new Dictionary<string, string>();
            foreach (var mig in migrations)
                hash.Add(mig.Version, mig.Hash);
            hash["2006"] = "222";
            var db = new MigrationTracker(migrations, hash);
            Assert.That(db.IsValidState(), Is.False);
        }
        
        [Test]
        public void GapShouldBeValidIfPermissiveIsTrue()
        {
            var migrations = new List<Migration> { new Migration("2006", "asdf", ""), new Migration("2007", "fdsa", "") };
            var hash = new Dictionary<string, string>();
            foreach (var mig in migrations)
                hash.Add(mig.Version, mig.Hash);
            migrations.Insert(1, new Migration("2006-01", "fdsw", ""));
            var db = new MigrationTracker(migrations, hash, true);
            Assert.That(db.IsValidState(), Is.True);
        }
        
        [Test]
        public void ShouldNotBeValidIfThereIsAMigrationGap()
        {
            var migrations = new List<Migration> { new Migration("2006", "asdf", ""), new Migration("2007", "fdsa", "") };
            var hash = new Dictionary<string, string>();
            foreach (var mig in migrations)
                hash.Add(mig.Version, mig.Hash);
            migrations.Insert(1, new Migration("2006-01", "fdsw", ""));
            var db = new MigrationTracker(migrations, hash);
            Assert.That(db.IsValidState(), Is.False);
        }
        [Test]
        public void ShouldFindLastValidMigration()
        {
            var migrations = new List<Migration> { new Migration("2006", "asdf", ""), new Migration("2007", "fdsa", "") };
            var hash = new Dictionary<string, string>();
            foreach (var mig in migrations)
                hash.Add(mig.Version, mig.Hash);
            migrations.Insert(1, new Migration("2006-01", "fdsw", ""));
            var db = new MigrationTracker(migrations, hash);
            Assert.That(db.LastValidMigration.Version, Is.EqualTo("2006"));
        }
        [Test]
        public void ShouldReturnAllMigrationsWhichResideInTheHash()
        {
            var migrations = new List<Migration> { new Migration("2006", "asdf", ""), new Migration("2007", "fdsa", "") };
            var hash = new Dictionary<string, string>();
            foreach (var mig in migrations)
                hash.Add(mig.Version, mig.Hash);
            migrations.Insert(1, new Migration("2006-01", "fdsw", ""));
            var db = new MigrationTracker(migrations, hash);
            Assert.That(db.ExecutedMigrations.Count(), Is.EqualTo(2));
        }
        [Test]
        public void ExecutedMigrationsShouldBePopulated()
        {
            var migrations = new List<Migration> { 
                new Migration("2006", "asdf", ""), 
                new Migration("2006-01", "98sd98", ""), 
                new Migration("2007", "fdsa", "") };
            var hash = new Dictionary<string, string>();
            foreach (var mig in migrations)
                hash.Add(mig.Version, mig.Hash);
            var db = new MigrationTracker(migrations, hash);
            Assert.That(db.ExecutedMigrations.Count(), Is.EqualTo(3));
        }
        [Test]
        public void ShouldNotThinkThereIsAGapWhenThereAreNoMigrations()
        {
            var migrations = new List<Migration> { 
                new Migration("2006", "asdf", ""), 
                new Migration("2006-01", "98sd98", ""), 
                new Migration("2007", "fdsa", "") };
            var db = new MigrationTracker(migrations, null);
            Assert.That(db.IsMigrationGap(), Is.False);
        }
        [Test]
        public void ShouldNotThinkThereIsAGapWhenThereIsASingleMigration()
        {
            var migrations = new List<Migration> { 
                new Migration("2006", "asdf", ""), 
                new Migration("2006-01", "98sd98", ""), 
                new Migration("2007", "fdsa", "") };
            var hashes = new Dictionary<string, string>();
            hashes.Add(migrations[0].Version, migrations[0].Hash);
            var db = new MigrationTracker(migrations, hashes);
            Assert.That(db.IsMigrationGap(), Is.False);
        }
        [Test]
        public void DownMigrationShouldExecuteToVersionSpecified()
        {
            var migrations = new List<Migration> { 
                new Migration("2006", "asdf", ""), 
                new Migration("2006-01", "98sd98", ""), 
                new Migration("2007", "fdsa", "") };
            var hashes = new Dictionary<string, string>();
            foreach (var mig in migrations)
                hashes.Add(mig.Version, mig.Hash);
            var db = new MigrationTracker(migrations, hashes);
            var repoMock = new Mock<IDatabaseRepository>();
            int y = 0;
            repoMock.Setup(x => x.ExecuteDown(It.IsAny<Migration>())).Returns(db).Callback(() => { y++; });
            var migrator = new Migrator(db, repoMock.Object);
            migrator.MigrateTo("2006");
            Assert.That(y, Is.EqualTo(2));
        }
        [Test]
        public void AnEmptyHashListShouldReturnFalseForInvalidHash()
        {
            var migrations = new List<Migration>
            {
                new Migration("2006", "asdf", ""), 
                new Migration("2007", "fdsa", "")
            };
            var db = new MigrationTracker(migrations, new Dictionary<string, string>());
            Assert.That(db.IsHashMismatch(), Is.False);
        }
    }
}
