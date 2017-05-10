# MyConnections
This  project base on Dapper https://github.com/StackExchange/Dapper.
It is an simple orm,an it is easy to use,it supports mysql,sqlserver,sqlite

1、Create an MyConnection
<pre>

  //IDbConnection
  public static IDbConnection MySqlConn()
  {
      IDbConnection conn = new MySqlConnection(MySqlConnettionString);

      if (conn.State == ConnectionState.Closed)
          conn.Open();

      return conn;
  }
  
  
  public static MyConnection GetMyConn()
  {
      
      return MyConnection.CreateMySqlConn(MySqlConn()); //mysql
      
      //return MyConnection.CreateSqlServerConn(IDbConnection...); //sqlserver
      //return MyConnection.CreateSqliteConn(IDbConnection...); //sqlite
  }

</pre>

2、Create an Model
<pre>

    [Table(TableName = "people", KeyName = "Id", IsIdentity = true)]
    public class peopleT
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int Score { get; set; }

        public DateTime Time { get; set; }

        [Igore]
        public int age { get; set; } //no table column
    }
    
</pre>

3、Now we can use it
<pre>

  using(var conn = GetMyConn())
  {
      peopleT p = new peopleT();
      p.Name = "Jone";
      p.Score = 100;
      p.Time = DateTime.Now;
      
      int result = conn.Insert(p); //insert
      int identityId = conn.GetIdentity&lt;int&gt;();
      
      p.Id =1;
      conn.Update(p) //update ... where Id=@id
      
      conn.Delete(1) //delete by id
      
      peopleT p = conn.GetById&lt;peopleT&gt;(1); //get by id
      
      //More Method
      InsertMany，InsertKeyIfExistUpdate，InsertIdentity，Update，UpdateByWhere，DeleteByIds，DeleteAll，GetAll
      GetByWhere，GetByWhereFirst，GetCount，GetByIdsWhichField，GetBySkipTake，GetByPage
      
      //Transaction
      
      conn.BeginTran()
      try
      {
          //...some options
          
          conn.CommitTran(); //commit
      }
      catch
      {
          conn.RollBackTran(); //if error rollback
      }
      
  }

</pre>

Download  https://github.com/znyet/MyConnections/releases
