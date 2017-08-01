using System;
using System.Collections.Generic;

using Dapper;
using System.Data;
using MyConnections.Connection;

namespace MyConnections
{
    public abstract class MyConnection : IDisposable
    {
        /// <summary>
        /// 创建MySql
        /// </summary>
        public static MyConnection CreateMySqlConn(IDbConnection conn)
        {
            return new MySqlConnection(conn);
        }

        /// <summary>
        /// 创建SqlServer
        /// </summary>
        public static SqlServerConnection CreateSqlServerConn(IDbConnection conn)
        {
            return new SqlServerConnection(conn);
        }

        /// <summary>
        /// 创建Sqlite
        /// </summary>
        public static SqliteConnection CreateSqliteConn(IDbConnection conn)
        {
            return new SqliteConnection(conn);
        }

        protected IDbConnection conn { get; set; }
        protected IDbTransaction tran { get; set; }
        protected MyReader reader { get; set; }

        /// <summary>
        /// 超时时间
        /// </summary>
        public int? commandTimeout { get; set; }

        /// <summary>
        /// 请求类型Text：1  StoredProcedure：4
        /// </summary>
        public CommandType? commandType { get; set; }


        #region common method

        /// <summary>
        /// ChangeDatabase
        /// </summary>
        /// <param name="name"></param>
        public void ChangeDatabase(string name)
        {
            conn.ChangeDatabase(name);
        }

        /// <summary>
        /// 开始事物
        /// </summary>
        public void BeginTran()
        {
            this.tran = conn.BeginTransaction();
        }

        /// <summary>
        /// 提交事物
        /// </summary>
        public void CommitTran()
        {
            if (tran == null)
                throw new Exception("you must BeginTransaction");
            this.tran.Commit();
        }

        /// <summary>
        /// 回滚事物
        /// </summary>
        public void RollBackTran()
        {
            if (tran == null)
                throw new Exception("you must BeginTransaction");
            this.tran.Rollback();
        }


        private bool m_disposed;
        protected virtual void Dispose(bool disposing)
        {
            if (!m_disposed)
            {
                if (disposing)
                {
                    // Release managed resources (释放托管资源)
                }

                // Release unmanaged resources (释放非托管资源)
                if (tran != null)
                    tran.Dispose();

                if (reader != null)
                {
                    reader.Close();
                    reader = null;
                }

                if (conn != null && conn.State == ConnectionState.Open)
                    conn.Dispose();

                //释放状态改成true
                m_disposed = true;
            }
        }

        ~MyConnection()
        {
            Dispose(false);
        }

        /// <summary>
        /// 释放连接
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 执行增删改语句
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public int Execute(string sql, object param = null)
        {
            return conn.Execute(sql, param, tran, commandTimeout, commandType);
        }

        /// <summary>
        /// 返回第一行第一列object
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <param name="commandType"></param>
        /// <returns></returns>
        public object ExecuteScalar(string sql, object param = null)
        {
            return conn.ExecuteScalar(sql, param, tran, commandTimeout, commandType);
        }

        /// <summary>
        /// 返回第一行第一列T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <param name="commandType"></param>
        /// <returns></returns>
        public T ExecuteScalar<T>(string sql, object param = null)
        {
            return conn.ExecuteScalar<T>(sql, param, tran, commandTimeout, commandType);
        }

        /// <summary>
        /// Query
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <param name="buffered"></param>
        /// <returns></returns>
        public IEnumerable<dynamic> Query(string sql, object param = null, bool buffered = true)
        {
            return conn.Query(sql, param, tran, buffered, commandTimeout, commandType);
        }

        /// <summary>
        /// Query
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <param name="buffered"></param>
        /// <returns></returns>
        public IEnumerable<T> Query<T>(string sql, object param = null, bool buffered = true)
        {
            return conn.Query<T>(sql, param, tran, buffered, commandTimeout, commandType);
        }

        /// <summary>
        /// QueryFirstOrDefault
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public dynamic QueryFirstOrDefault(string sql, object param = null)
        {
            return conn.QueryFirstOrDefault(sql, param, tran, commandTimeout, commandType);
        }

        /// <summary>
        /// QueryFirstOrDefault
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public T QueryFirstOrDefault<T>(string sql, object param = null)
        {
            return conn.QueryFirstOrDefault<T>(sql, param, tran, commandTimeout, commandType);
        }

        /// <summary>
        /// QueryMultiple读取多结果集
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public MyReader QueryMultiple(string sql, object param = null)
        {
            var gridReader = conn.QueryMultiple(sql, param, tran, commandTimeout, commandType);
            if (reader != null)
                reader.Close();
            reader = new MyReader(gridReader);
            return reader;
        }

        /// <summary>
        /// ExecuteReader
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public IDataReader ExecuteReader(string sql, object param = null)
        {
            return conn.ExecuteReader(sql, param, tran, commandTimeout, commandType);
        }

        /// <summary>
        /// 返回DataTable
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public DataTable QueryDataTable(string sql, object param = null)
        {
            using (IDataReader reader = ExecuteReader(sql, param))
            {
                DataTable dt = new DataTable();
                dt.Load(reader);
                return dt;
            }
        }

        /// <summary>
        /// 返回DataSet
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public DataSet QueryDataSet(string sql, object param = null)
        {
            using (IDataReader reader = ExecuteReader(sql, param))
            {
                DataSet ds = new DataSet();
                int i = 0;
                while (!reader.IsClosed)
                {
                    i++;
                    DataTable dt = new DataTable();
                    dt.TableName = "T" + i;
                    dt.Load(reader);
                    ds.Tables.Add(dt);
                }
                return ds;
            }
        }

        #endregion

        #region abstract method

        /// <summary>
        /// Insert
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <returns></returns>
        public abstract int Insert<T>(T model);

        /// <summary>
        /// InsertMany
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="models"></param>
        /// <returns></returns>
        public abstract int InsertMany<T>(IEnumerable<T> models);

        /// <summary>
        /// Insert key if key exist will be update
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <param name="updateFields">null will be update all fields</param>
        /// <returns></returns>
        public abstract int InsertKeyIfExistUpdate<T>(T model, string updateFields = null, bool allowUpdate = true);

        /// <summary>
        /// InsertKeyMany if key exist will be update
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="models"></param>
        /// <param name="updateFields">null will be update all fields</param>
        /// <returns></returns>
        public abstract int InsertKeyManyIfExistUpdate<T>(IEnumerable<T> models, string updateFields = null, bool allowUpdate = true);

        /// <summary>
        /// Insert if key exist will be update
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <param name="updateFields">null will be update all fields</param>
        /// <returns></returns>
        public abstract int InsertIfExistUpdate<T>(T model, string updateFields = null, bool allowUpdate = true);

        /// <summary>
        /// InsertMany if key exist will be update
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="models"></param>
        /// <param name="updateFields">null will be update all fields</param>
        /// <returns></returns>
        public abstract int InsertManyIfExistUpdate<T>(IEnumerable<T> models, string updateFields = null, bool allowUpdate = true);

        /// <summary>
        /// InsertIdentity
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <returns></returns>
        public abstract int InsertIdentity<T>(T model);

        /// <summary>
        /// InsertIdentityMany
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="models"></param>
        /// <returns></returns>
        public abstract int InsertIdentityMany<T>(IEnumerable<T> models);

        /// <summary>
        /// Update by id
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <returns></returns>
        public abstract int Update<T>(T model);

        /// <summary>
        /// Update By id (update some fields)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <param name="updateFields"></param>
        /// <returns></returns>
        public abstract int Update<T>(T model, string updateFields);

        /// <summary>
        /// UpdateMany by id
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="models"></param>
        /// <returns></returns>
        public abstract int UpdateMany<T>(IEnumerable<T> models);

        /// <summary>
        /// UpdateMany by id (update some fields)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="models"></param>
        /// <param name="updateFilds"></param>
        /// <returns></returns>
        public abstract int UpdateMany<T>(IEnumerable<T> models, string updateFields);

        /// <summary>
        /// If updateFields is null or empty,it will be update all fields
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="where"></param>
        /// <param name="updateFields"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public abstract int UpdateByWhere<T>(string where, string updateFields, T model);

        /// <summary>
        ///  If updateFields is null or empty,it will be update all fields
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="where"></param>
        /// <param name="updateFields"></param>
        /// <param name="models"></param>
        /// <returns></returns>
        public abstract int UpdateManyByWhere<T>(string where, string updateFields, IEnumerable<T> models);

        /// <summary>
        /// Delete by id
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        public abstract int Delete<T>(object id);

        /// <summary>
        /// Delete by ids
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ids"></param>
        /// <returns></returns>
        public abstract int DeleteByIds<T>(object ids);

        /// <summary>
        /// DeleteByWhere
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="where"></param>
        /// <returns></returns>
        public abstract int DeleteByWhere<T>(string where, object param);

        /// <summary>
        /// delete all data
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public abstract int DeleteAll<T>();

        /// <summary>
        /// Identity Id
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public abstract T GetIdentity<T>();

        /// <summary>
        /// GetAll
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="order"></param>
        /// <param name="returnFields">if null will be return all fields</param>
        /// <returns></returns>
        public abstract IEnumerable<T> GetAll<T>(string returnFields = null, string orderBy = null);

        /// <summary>
        /// GetById
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <param name="returnFields">if null will be return all fields</param>
        /// <returns></returns>
        public abstract T GetById<T>(object id, string returnFields = null);

        /// <summary>
        /// GetByIds
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ids"></param>
        /// <param name="returnFields"></param>
        /// <returns></returns>
        public abstract IEnumerable<T> GetByIds<T>(object ids, string returnFields = null);

        /// <summary>
        /// GetByWhere
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="where"></param>
        /// <param name="param"></param>
        /// <param name="returnFields"></param>
        /// <param name="orderBy"></param>
        /// <returns></returns>
        public abstract IEnumerable<T> GetByWhere<T>(string where, object param = null, string returnFields = null);

        /// <summary>
        /// GetByWhereFirst
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="where"></param>
        /// <param name="param"></param>
        /// <param name="returnFields"></param>
        /// <returns></returns>
        public abstract T GetByWhereFirst<T>(string where, object param = null, string returnFields = null);

        /// <summary>
        /// GetCount
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="where"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public abstract int GetCount<T>(string where = null, object param = null);

        /// <summary>
        /// GetByIds
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ids"></param>
        /// <param name="whichField"></param>
        /// <param name="returnFields"></param>
        /// <returns></returns>
        public abstract IEnumerable<T> GetByIdsWhichField<T>(object ids, string whichField, string returnFields = null);

        /// <summary>
        /// GetBySkipTake
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <param name="where"></param>
        /// <param name="param"></param>
        /// <param name="returnFields"></param>
        /// <returns></returns>
        public abstract IEnumerable<T> GetBySkipTake<T>(int skip, int take, string where = null, object param = null, string returnFields = null, string orderBy = null);

        /// <summary>
        /// GetByPageIndex
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="where"></param>
        /// <param name="param"></param>
        /// <param name="returnFields"></param>
        /// <param name="orderBy"></param>
        /// <returns></returns>
        public abstract IEnumerable<T> GetByPageIndex<T>(int pageIndex, int pageSize, string where = null, object param = null, string returnFields = null, string orderBy = null);

        /// <summary>
        /// GetByPage
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="total">total</param>
        /// <param name="where"></param>
        /// <param name="param"></param>
        /// <param name="returnFields"></param>
        /// <param name="orderBy"></param>
        /// <returns></returns>
        public abstract IEnumerable<T> GetByPage<T>(int pageIndex, int pageSize, out int total, string where = null, object param = null, string returnFields = null, string orderBy = null);

        /// <summary>
        /// Get the empty table,but it contain columns
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="returnFields"></param>
        /// <returns></returns>
        public abstract DataTable GetSchemaTable<T>(string returnFields = null);


        #endregion


    }
}
