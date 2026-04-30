using StreetSamurai.Core.Models;

namespace StreetSamurai.Core.Interfaces;

public interface IBookRepository
{
    List<Book> ListBooks();
    Book? LoadBook(string id);
    void SaveBook(Book book);
    void ArchiveBook(string id);
}
