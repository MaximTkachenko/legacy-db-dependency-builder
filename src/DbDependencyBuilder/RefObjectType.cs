using System.ComponentModel;

namespace DbDependencyBuilder
{
    public enum RefObjectType
    {
        [Description("Tables")]
        Tbl = 1,
        [Description("Synonyms")]
        Syn,
        [Description("Stored Procedures")]
        Sp,
        [Description("Functions")]
        Fun,
        [Description("Views")]
        V,
        [Description("Cs")]
        Cs,
        [Description("ETL")]
        Etl
    }
}
