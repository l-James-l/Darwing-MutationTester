using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Models;
using System.Data;

namespace ModelsTests;

[TestFixture]
public class SourceCodeFileCollectionTests
{
    private SourceCodeFileCollection _collection;
    private DocumentId _testId;
    private SyntaxTree _testTree;
    private const string TestPath = @"C:\Code\File.cs";

    [SetUp]
    public void SetUp()
    {
        _collection = new SourceCodeFileCollection();
        _testId = DocumentId.CreateNewId(ProjectId.CreateNewId());
        _testTree = CSharpSyntaxTree.ParseText("public class A {}").WithFilePath(TestPath);
    }

    [Test]
    public void AddDocument_WhenValid_AddsToBothIndexes()
    {
        // Act
        _collection.AddDocument(_testId, _testTree);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_collection.TryGetValue(TestPath, out var byPath), Is.True);
            Assert.That(_collection.TryGetValue(_testId, out var byId), Is.True);
            Assert.That(byPath, Is.EqualTo(byId));
        });
    }

    [Test]
    public void AddDocument_WithDuplicatePath_ThrowsDuplicateNameException()
    {
        // Arrange
        _collection.AddDocument(_testId, _testTree);
        var secondId = DocumentId.CreateNewId(ProjectId.CreateNewId());

        // Act & Assert
        Assert.Throws<DuplicateNameException>(() =>
            _collection.AddDocument(secondId, _testTree));
    }

    [Test]
    public void AddDocument_WithDuplicateId_ThrowsDuplicateNameException()
    {
        // Arrange
        _collection.AddDocument(_testId, _testTree);
        var secondTree = CSharpSyntaxTree.ParseText("class B {}").WithFilePath(@"C:\Code\Other.cs");

        // Act & Assert
        Assert.Throws<DuplicateNameException>(() =>
            _collection.AddDocument(_testId, secondTree));
    }

    [Test]
    public void TryGetValue_WhenKeyDoesNotExist_ReturnsFalse()
    {
        // Act & Assert
        Assert.Multiple(() =>
        {
            Assert.That(_collection.TryGetValue("NonExistent.cs", out _), Is.False);
            Assert.That(_collection.TryGetValue(DocumentId.CreateNewId(ProjectId.CreateNewId()), out _), Is.False);
        });
    }

    [Test]
    public void Enumeration_CoversAllAddedItems()
    {
        // Arrange
        var id2 = DocumentId.CreateNewId(ProjectId.CreateNewId());
        var tree2 = CSharpSyntaxTree.ParseText("class B {}").WithFilePath(@"C:\Code\File2.cs");
        _collection.AddDocument(_testId, _testTree);
        _collection.AddDocument(id2, tree2);

        // Act
        var list = _collection.ToList();

        // Assert
        Assert.That(list, Has.Count.EqualTo(2));
    }

    [Test]
    public void ForEach_ExecutesActionForEveryItem()
    {
        // Arrange
        _collection.AddDocument(_testId, _testTree);
        int callCount = 0;

        // Act
        _collection.ForEach(file => callCount++);

        // Assert
        Assert.That(callCount, Is.EqualTo(1));
    }

    [Test]
    public void NonGenericEnumerator_WorksCorrectly()
    {
        // Arrange
        _collection.AddDocument(_testId, _testTree);
        var enumerable = (System.Collections.IEnumerable)_collection;

        // Act
        var enumerator = enumerable.GetEnumerator();

        // Assert
        Assert.That(enumerator.MoveNext(), Is.True);
        Assert.That(enumerator.Current, Is.InstanceOf<SourceCodeFileContainer>());
    }
}