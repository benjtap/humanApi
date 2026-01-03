using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Webhttp.Models
{
    //public class BookRepository : IBookRepository
    //{
    //    private readonly ConcurrentDictionary<string, clsProject> books;
    //    private int nextId = 0;

    //    public BookRepository()
    //    {
    //        this.books = new ConcurrentDictionary<string, clsProject>();
    //        this.Add(new clsProject { Title = "RESTful API with ASP.NET Core MVC 1.0", Author = "Nick Soper" });
    //    }

    //    public void Add(clsProject book)
    //    {
    //        if (book == null)
    //        {
    //            return;
    //        }

    //        this.nextId++;
    //        book.Id = nextId.ToString();

    //        this.books.TryAdd(book.Id, book);
    //    }

    //    public clsProject Find(string id)
    //    {
    //        clsProject book;
    //        this.books.TryGetValue(id, out book);
    //        return book;
    //    }

    //    public IEnumerable<clsProject> GetAll()
    //    {
    //        return this.books.Values.OrderBy(b => b.Id);
    //    }

    //    public clsProject Remove(string id)
    //    {
    //        clsProject book;
    //        this.books.TryRemove(id, out book);
    //        return book;
    //    }

    //    public void Update(clsProject book)
    //    {
    //        this.books[book.Id] = book;
    //    }
    //}
}
