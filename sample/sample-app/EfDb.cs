using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace SampleApp
{
    public class EfDb : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(Program.Cs);
        }

        public DbSet<Person> Persons { get; set; }
    }

    [Table("Person")]
    public class Person
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
