using Prose.WriterUi.Services;

namespace Prose.UnitTests;

/// <summary>RA-T2.2.1.1: links into the Writer resolve a book by id, slug or code.</summary>
[TestFixture]
public class WriterLinkTests
{
    private static readonly WriterService.BookListItem A = new(Guid.NewGuid(), "salt-road-1a2b", "The Salt Road", "SALT");
    private static readonly WriterService.BookListItem B = new(Guid.NewGuid(), "other-9f8e", "Other", null);
    private static readonly WriterService.BookListItem[] Books = [A, B];

    [Test] public void By_id() => Assert.That(WriterLink.ResolveBook(Books, A.Id.ToString()), Is.EqualTo(A));
    [Test] public void By_slug_case_insensitive() => Assert.That(WriterLink.ResolveBook(Books, "SALT-ROAD-1A2B"), Is.EqualTo(A));
    [Test] public void By_code() => Assert.That(WriterLink.ResolveBook(Books, "salt"), Is.EqualTo(A));
    [Test] public void Unknown_is_null() => Assert.That(WriterLink.ResolveBook(Books, "nope"), Is.Null);
    [Test] public void Empty_is_null() => Assert.That(WriterLink.ResolveBook(Books, " "), Is.Null);

    [Test]
    public void Url_shape()
    {
        var beat = Guid.NewGuid();
        Assert.That(WriterLink.For(A.Id, beat), Is.EqualTo($"/writer?book={A.Id:D}&beat={beat:D}"));
        Assert.That(WriterLink.For(A.Id), Is.EqualTo($"/writer?book={A.Id:D}"));
    }
}
