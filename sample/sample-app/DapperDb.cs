using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Dapper;

namespace SampleApp
{
    public class DapperDb
    {
        public void InsertPerson()
        {
            using (var conn = new SqlConnection(Program.Cs))
            {
                conn.Execute("insert into dbo.person values (@name)", new {@name = Guid.NewGuid().ToString()});
            }
        }

        public List<string> CallGetPersonsProc()
        {
            using (var conn = new SqlConnection(Program.Cs))
            {
                return conn.Query<string>("dbo.Get_Persons", commandType: CommandType.StoredProcedure).ToList();
            }
        }
    }
}
