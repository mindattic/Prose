using Prose.Core.Models;

namespace Prose.Core.Interfaces;

public interface IBookRepository
{
    List<Book> ListBooks();
    Book? LoadBook(string id);
    void SaveBook(Book book);
    void ArchiveBook(string id);
}
