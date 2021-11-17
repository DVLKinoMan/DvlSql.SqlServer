# DvlSql.SqlServer
DSL for Working with SQL (Select, Update, Insert, Delete, Executing Stored procedures). Implementation for working with Microsoft Sql.
# Descirption
DvlSql.SqlServer is DSL on C# for working with MS Sql Server. You can make complex select, 
update, insert, delete statements with fluent interface. It also supports stored procedures execution and Transactions. It's interface can be used to
create custom implementation.

# How to use - Examples
```c#
string connectionString =
                "Data Source = serverName; Initial Catalog = databaseName; User Id = userId; Password=pass;";

var dvl_sql = new DvlSqlMs(connectionString);

//Select ids from table ordered by date
List<int> ids = dvl_sql.From("tableName")
                        .Select("id", "col1", "col2")
                        .OrderBy("date")
                        .ToListAsync(r => (int) r["id"])
                        .Result;

//using static DvlSql.Extensions.ExpressionHelpers; (For Expressions)
//Select top 100 youngest persons Name and Age, whose names contains david and are living in Tbilisi
var personInfo = dvl_sql.From("Persons")
                        .Join("Addresses", ConstantExp("Persons.Id") == ConstantExp("Addresses.PersonId"))
                        .Where(LikeExp("Persons.Name", "%david%") & 
                               ConstantExp("Addresses.City") == ConstantExp("Tbilisi"))
                        .SelectTop(100, "Name", "Age")
                        .OrderBy("Age")
                        .ToListAsync(r => new {Name = r["Name"].ToString(), Age = (int) r["Age"]})
                        .Result;

//using static DvlSql.Extensions.SqlType; (For NVarCharType)
//Inserting word and there meanings in table
var affectedRows1 = dvl_sql.InsertInto<string, string>("dbo.Words",
                                NVarCharType("Text", 50),
                                NVarCharType("Meaning", 1000)
                            )
                            .Values(
                                ("Pyramid", "Egypt sculpture"),
                                ("March", "Month's name")
                                //More values if you want
                            )
                            .ExecuteAsync().Result;

//using static DvlSql.Extensions.ExpressionHelpers; 
//using static DvlSql.Extensions.SqlType;
//Update product Price and UpdatedDate which price is 2.11
var affectedRows2 = dvl_sql.Update("dbo.Products")
                            .Set(Money("Price",new decimal(3.11)))
                            .Set(DateTime("UpdatedDate", System.DateTime.Now))
                            //Update more columns if you want
                            .Where(ConstantExp("Price") == ConstantExp("@price"),
                                Param("@price", Decimal(new decimal(2.11))))
                            .ExecuteAsync().Result;

//using static DvlSql.Extensions.ExpressionHelpers; 
//using static DvlSql.Extensions.SqlType;
//Delete all words which contains test 
var affectedRows3 = dvl_sql.DeleteFrom("dbo.Words")
                            .Where(LikeExp("Text", "%test%"))
                            .ExecuteAsync().Result;
```
