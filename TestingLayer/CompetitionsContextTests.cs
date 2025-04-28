using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using BusinessLayer.Entities;
using DataLayer.Contexts;
using System.Linq;
using System.Threading.Tasks;
using System;

[TestFixture]
public class CompetitionContextTests
{
    private OnlineJudgeDbContext _context;
    private CompetitionContext _competitionContext;

    [SetUp]
    public void Setup()
    {
        // Create a new in-memory database for each test
        var options = new DbContextOptionsBuilder<OnlineJudgeDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase") // Unique name for each test run
            .Options;

        // Create the DbContext using the in-memory database
        _context = new OnlineJudgeDbContext(options);

        // Create the CompetitionContext using the in-memory database context
        _competitionContext = new CompetitionContext(_context);
    }

    [Test]
    public async Task CreateAsync_ShouldAddCompetition()
    {
        // Arrange
        var competition = new Competition { Id = 1, Name = "Test Competition", Notation = "TC" };

        // Act
        await _competitionContext.CreateAsync(competition);

        // Assert
        var savedCompetition = await _context.Competitions.FirstOrDefaultAsync(c => c.Id == 1);
        Assert.IsNotNull(savedCompetition);
        Assert.That(savedCompetition.Name, Is.EqualTo("Test Competition"));
    }

    [Test]
    public async Task DeleteAsync_ShouldRemoveCompetition_WhenCompetitionExists()
    {
        // Arrange
        var competition = new Competition { Id = 1, Name = "Test Competition", Notation = "TC" };
        await _competitionContext.CreateAsync(competition);

        // Act
        await _competitionContext.DeleteAsync(1);

        // Assert
        var deletedCompetition = await _context.Competitions.FirstOrDefaultAsync(c => c.Id == 1);
        Assert.IsNull(deletedCompetition);
    }

    [Test]
    public async Task DeleteAsync_ShouldThrowException_WhenCompetitionDoesNotExist()
    {
        // Act & Assert
        var ex = Assert.ThrowsAsync<ArgumentException>(async () => await _competitionContext.DeleteAsync(999));
        Assert.That(ex.Message, Is.EqualTo("Competition with this id was not found"));
    }

    [Test]
    public async Task ReadAllAsync_ShouldReturnAllCompetitions()
    {
        // Arrange
        var competition1 = new Competition { Id = 1, Name = "Competition 1", Notation = "TC1" };
        var competition2 = new Competition { Id = 2, Name = "Competition 2", Notation = "TC2" };
        await _competitionContext.CreateAsync(competition1);
        await _competitionContext.CreateAsync(competition2);

        // Act
        var competitions = await _competitionContext.ReadAllAsync();

        // Assert
        Assert.That(competitions.Count, Is.EqualTo(2));
        Assert.IsTrue(competitions.Any(c => c.Name == "Competition 1"));
        Assert.IsTrue(competitions.Any(c => c.Name == "Competition 2"));
    }

    [Test]
    public async Task ReadAsync_ShouldReturnCompetition_WhenCompetitionExists()
    {
        // Arrange
        var competition = new Competition { Id = 1, Name = "Test Competition", Notation = "TC" };
        await _competitionContext.CreateAsync(competition);

        // Act
        var result = await _competitionContext.ReadAsync(1);

        // Assert
        Assert.IsNotNull(result);
        Assert.That(result.Name, Is.EqualTo("Test Competition"));
    }

    [Test]
    public async Task ReadAsync_ShouldReturnNull_WhenCompetitionDoesNotExist()
    {
        // Act
        var result = await _competitionContext.ReadAsync(999);

        // Assert
        Assert.IsNull(result);
    }

    [Test]
    public async Task UpdateAsync_ShouldUpdateCompetition_WhenCompetitionExists()
    {
        // Arrange
        var competition = new Competition { Id = 1, Name = "Old Name", Notation = "TC" };
        await _competitionContext.CreateAsync(competition);
        competition.Name = "Updated Name";

        // Act
        await _competitionContext.UpdateAsync(competition);

        // Assert
        var updatedCompetition = await _context.Competitions.FirstOrDefaultAsync(c => c.Id == 1);
        Assert.IsNotNull(updatedCompetition);
        Assert.That(updatedCompetition.Name, Is.EqualTo("Updated Name"));
    }

    [Test]
    public async Task UpdateAsync_ShouldThrowException_WhenCompetitionDoesNotExist()
    {
        // Arrange
        var competition = new Competition { Id = 999, Name = "Non-existent Competition", Notation = "TC" };

        // Act & Assert
        var ex = Assert.ThrowsAsync<ArgumentException>(async () => await _competitionContext.UpdateAsync(competition));
        Assert.That(ex.Message, Is.EqualTo("Competition with this id was not found"));
    }

    [Test]
    public async Task CreateAsync_ShouldThrowException_WhenCompetitionIsNull()
    {
        // Act & Assert
        var ex = Assert.ThrowsAsync<ArgumentNullException>(async () => await _competitionContext.CreateAsync(null));
        Assert.That(ex.Message, Is.EqualTo("Value cannot be null. (Parameter 'Item cannot be null')"));
    }

    [Test]
    public async Task ReadAllAsync_ShouldReturnEmptyList_WhenNoCompetitionsExist()
    {
        // Act
        var competitions = await _competitionContext.ReadAllAsync();

        // Assert
        Assert.That(competitions.Count, Is.EqualTo(0));
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up the in-memory database after each test
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}