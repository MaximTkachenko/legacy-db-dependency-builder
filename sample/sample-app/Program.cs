using System;
using System.Linq;

namespace SampleApp
{
    class Program
    {
        public static readonly string Cs = @"Data Source=.\SQLEXPRESS;Initial Catalog=sample-db;Integrated Security=True";

        static void Main(string[] args)
        {
            Console.WriteLine(new DapperDb().CallGetPersonsProc().Count);
            new DapperDb().InsertPerson();
            Console.WriteLine(new DapperDb().CallGetPersonsProc().Count);

            using (var efdb = new EfDb())
            {
                Console.WriteLine(efdb.Persons.Count());
                efdb.Persons.Add(new Person {Name = Guid.NewGuid().ToString()});
                efdb.SaveChanges();
                Console.WriteLine(efdb.Persons.Count());
            }
        }
    }
}
