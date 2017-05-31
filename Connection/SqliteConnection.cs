using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;

using MyConnections.Common;
using System.Linq;
using System.Text;

namespace MyConnections.Connection
{
    public class SqliteConnection : MyConnection
    {
        public SqliteConnection(IDbConnection conn)
        {
            this.conn = conn;
        }

        /// <summary>
        /// Cache
        /// </summary>
        private static readonly ConcurrentDictionary<RuntimeTypeHandle, DapperSqls> DapperSqlsDict = new ConcurrentDictionary<RuntimeTypeHandle, DapperSqls>();
        private static object _locker = new object();
        public static DapperSqls GetDapperSqls(Type t)
        {
            if (!DapperSqlsDict.Keys.Contains(t.TypeHandle))
            {
                lock (_locker)
                {
                    if (!DapperSqlsDict.Keys.Contains(t.TypeHandle))
                    {
                        DapperSqls sqls = DapperCommon.GetDapperSqls(t);

                        string Fields = DapperCommon.GetFieldsStr(sqls.AllFieldList, "[", "]");
                        string FieldsAt = DapperCommon.GetFieldsAtStr(sqls.AllFieldList);
                        string FieldsEq = DapperCommon.GetFieldsEqStr(sqls.AllFieldList, "[", "]");

                        string FieldsExtKey = DapperCommon.GetFieldsStr(sqls.ExceptKeyFieldList, "[", "]");
                        string FieldsAtExtKey = DapperCommon.GetFieldsAtStr(sqls.ExceptKeyFieldList);
                        string FieldsEqExtKey = DapperCommon.GetFieldsEqStr(sqls.ExceptKeyFieldList, "[", "]");

                        sqls.AllFields = Fields;
                        sqls.AllFieldsAt = FieldsAt;
                        sqls.AllFieldsAtEq = FieldsEq;

                        sqls.AllFieldsExceptKey = FieldsExtKey;
                        sqls.AllFieldsAtExceptKey = FieldsAtExtKey;
                        sqls.AllFieldsAtEqExceptKey = FieldsEqExtKey;

                        if (!string.IsNullOrEmpty(sqls.KeyName) && sqls.IsIdentity) //有主键并且是自增
                        {
                            sqls.InsertSql = string.Format("INSERT INTO [{0}]({1})VALUES({2})", sqls.TableName, FieldsExtKey, FieldsAtExtKey);
                            sqls.InsertIdentitySql = string.Format("INSERT INTO [{0}]({1})VALUES({2})", sqls.TableName, Fields, FieldsAt);
                        }
                        else
                        {
                            sqls.InsertSql = string.Format("INSERT INTO [{0}]({1})VALUES({2})", sqls.TableName, Fields, FieldsAt);
                        }

                        if (!string.IsNullOrEmpty(sqls.KeyName)) //含有主键
                        {
                            sqls.DeleteByIdSql = string.Format("DELETE FROM [{0}] WHERE [{1}]=@id", sqls.TableName, sqls.KeyName);
                            sqls.DeleteByIdsSql = string.Format("DELETE FROM [{0}] WHERE [{1}] IN @ids", sqls.TableName, sqls.KeyName);
                            sqls.GetByIdSql = string.Format("SELECT {0} FROM [{1}] WHERE [{2}]=@id", Fields, sqls.TableName, sqls.KeyName);
                            sqls.GetByIdsSql = string.Format("SELECT {0} FROM [{1}] WHERE [{2}] IN @ids", Fields, sqls.TableName, sqls.KeyName);
                            sqls.UpdateSql = string.Format("UPDATE [{0}] SET {1} WHERE [{2}]=@{2}", sqls.TableName, FieldsEqExtKey, sqls.KeyName);
                        }
                        sqls.DeleteAllSql = string.Format("DELETE FROM [{0}]", sqls.TableName);
                        sqls.GetAllSql = string.Format("SELECT {0} FROM [{1}]", Fields, sqls.TableName);

                        DapperSqlsDict[t.TypeHandle] = sqls;
                    }
                }
            }

            return DapperSqlsDict[t.TypeHandle];
        }

        public override int Insert<T>(T model)
        {
            return Execute(GetDapperSqls(typeof(T)).InsertSql, model);
        }

        public override int InsertMany<T>(IEnumerable<T> models)
        {
            return Execute(GetDapperSqls(typeof(T)).InsertSql, models);
        }

        public override int InsertKeyIfExistUpdate<T>(T model, string updateFields = null, bool allowUpdate = true)
        {
            DapperSqls sqls = GetDapperSqls(typeof(T));
            if (string.IsNullOrEmpty(sqls.KeyName))
                throw new Exception("表[" + sqls.TableName + "]没有主键");

            string sqlTotal = string.Format("SELECT COUNT(1) FROM [{0}] WHERE [{1}]=@{1}", sqls.TableName, sqls.KeyName);
            int total = ExecuteScalar<int>(sqlTotal, model);
            if (total > 0) //Exists
            {
                if (allowUpdate)
                {
                    if (string.IsNullOrEmpty(updateFields))
                        return Update(model);
                    else
                        return Update(model, updateFields);
                }
                return 0;
            }
            else
            {
                if (sqls.IsIdentity) //identity key
                    return InsertIdentity(model);
                else
                    return Insert(model);
            }
        }

        public override int InsertKeyManyIfExistUpdate<T>(IEnumerable<T> models, string updateFields = null, bool allowUpdate = true)
        {
            int result = 0;
            foreach (var item in models)
            {
                var result1 = InsertKeyIfExistUpdate(item, updateFields, allowUpdate);
                result = result + result1;
            }
            return result;
        }

        public override int InsertIdentity<T>(T model)
        {
            var sqls = GetDapperSqls(typeof(T));
            if (sqls.IsIdentity == false)
                throw new Exception("表[" + sqls.TableName + "]没有自增列");

            return Execute(sqls.InsertIdentitySql, model);
        }

        public override int InsertIdentityMany<T>(IEnumerable<T> models)
        {
            var sqls = GetDapperSqls(typeof(T));
            if (sqls.IsIdentity == false)
                throw new Exception("表[" + sqls.TableName + "]没有自增列");

            return Execute(sqls.InsertIdentitySql, models);
        }

        public override int Update<T>(T model)
        {
            var sqls = GetDapperSqls(typeof(T));
            if (string.IsNullOrEmpty(sqls.KeyName))
                throw new Exception("表[" + sqls.TableName + "]没有主键");

            return Execute(sqls.UpdateSql, model);
        }

        public override int Update<T>(T model, string updateFields)
        {
            var sqls = GetDapperSqls(typeof(T));
            if (string.IsNullOrEmpty(sqls.KeyName))
                throw new Exception("表[" + sqls.TableName + "]没有主键");

            string sql = DapperCommon.BuilderUpdateByIdSql(sqls, updateFields, "[", "]");
            return Execute(sql, model);
        }

        public override int UpdateMany<T>(IEnumerable<T> models)
        {
            var sqls = GetDapperSqls(typeof(T));
            if (string.IsNullOrEmpty(sqls.KeyName))
                throw new Exception("表[" + sqls.TableName + "]没有主键");

            return Execute(sqls.UpdateSql, models);
        }

        public override int UpdateMany<T>(IEnumerable<T> models, string updateFields)
        {
            var sqls = GetDapperSqls(typeof(T));
            if (string.IsNullOrEmpty(sqls.KeyName))
                throw new Exception("表[" + sqls.TableName + "]没有主键");

            string sql = DapperCommon.BuilderUpdateByIdSql(sqls, updateFields, "[", "]");
            return Execute(sql, models);
        }

        public override int UpdateByWhere<T>(string where, string updateFields, T model)
        {
            var sqls = GetDapperSqls(typeof(T));
            string sql = DapperCommon.BuilderUpdateByWhereSql(sqls, where, updateFields, "[", "]");
            return Execute(sql, model);
        }

        public override int UpdateManyByWhere<T>(string where, string updateFields, IEnumerable<T> models)
        {
            var sqls = GetDapperSqls(typeof(T));
            string sql = DapperCommon.BuilderUpdateByWhereSql(sqls, where, updateFields, "[", "]");
            return Execute(sql, models);
        }

        public override int Delete<T>(object id)
        {
            var sqls = GetDapperSqls(typeof(T));
            if (string.IsNullOrEmpty(sqls.KeyName))
                throw new Exception("表[" + sqls.TableName + "]没有主键");

            return Execute(sqls.DeleteByIdSql, new { id = id });
        }

        public override int DeleteByIds<T>(object ids)
        {
            var sqls = GetDapperSqls(typeof(T));
            if (string.IsNullOrEmpty(sqls.KeyName))
                throw new Exception("表[" + sqls.TableName + "]没有主键");
            if (DapperCommon.ObjectIsEmpty(ids))
                return 0;
            Dapper.DynamicParameters dpar = new Dapper.DynamicParameters();
            dpar.Add("@ids", ids);
            return Execute(sqls.DeleteByIdsSql, dpar);
        }

        public override int DeleteByWhere<T>(string where, object param)
        {
            DapperSqls sqls = GetDapperSqls(typeof(T));
            return Execute(sqls.DeleteAllSql + " " + where, param);
        }

        public override int DeleteAll<T>()
        {
            return Execute(GetDapperSqls(typeof(T)).DeleteAllSql);
        }

        public override T GetIdentity<T>()
        {
            return ExecuteScalar<T>("SELECT last_insert_rowid()");
        }

        public override IEnumerable<T> GetAll<T>(string returnFields = null, string orderBy = null)
        {
            DapperSqls sqls = GetDapperSqls(typeof(T));
            if (string.IsNullOrEmpty(returnFields))
                return Query<T>(sqls.GetAllSql + " " + orderBy);
            else
            {
                string sql = string.Format("SELECT {0} FROM [{1}] " + orderBy, returnFields, sqls.TableName);
                return Query<T>(sql);
            }
        }

        public override T GetById<T>(object id, string returnFields = null)
        {
            DapperSqls sqls = GetDapperSqls(typeof(T));
            if (string.IsNullOrEmpty(sqls.KeyName))
                throw new Exception("表[" + sqls.TableName + "]没有主键");

            if (string.IsNullOrEmpty(returnFields))
                return QueryFirstOrDefault<T>(sqls.GetByIdSql, new { id = id });
            else
            {
                string sql = string.Format("SELECT {0} FROM [{1}] WHERE [{2}]=@id", returnFields, sqls.TableName, sqls.KeyName);
                return QueryFirstOrDefault<T>(sql, new { id = id });
            }
        }

        public override IEnumerable<T> GetByIds<T>(object ids, string returnFields = null)
        {
            var sqls = GetDapperSqls(typeof(T));
            if (string.IsNullOrEmpty(sqls.KeyName))
                throw new Exception("表[" + sqls.TableName + "]没有主键");
            if (DapperCommon.ObjectIsEmpty(ids))
                return Enumerable.Empty<T>();

            Dapper.DynamicParameters dpar = new Dapper.DynamicParameters();
            dpar.Add("@ids", ids);
            if (string.IsNullOrEmpty(returnFields))
                return Query<T>(sqls.GetByIdsSql, dpar);
            else
            {
                string sql = string.Format("SELECT {0} FROM [{1}] WHERE [{2}] IN @ids", returnFields, sqls.TableName, sqls.KeyName);
                return Query<T>(sql, dpar);
            }
        }

        public override IEnumerable<T> GetByWhere<T>(string where, object param = null, string returnFields = null)
        {
            DapperSqls sqls = GetDapperSqls(typeof(T));
            if (string.IsNullOrEmpty(returnFields))
                returnFields = sqls.AllFields;
            string sql = string.Format("SELECT {0} FROM [{1}] {2}", returnFields, sqls.TableName, where);

            return Query<T>(sql, param);
        }

        public override T GetByWhereFirst<T>(string where, object param = null, string returnFields = null)
        {
            DapperSqls sqls = GetDapperSqls(typeof(T));
            if (string.IsNullOrEmpty(returnFields))
                returnFields = sqls.AllFields;
            string sql = string.Format("SELECT {0} FROM [{1}] {2}", returnFields, sqls.TableName, where);

            return QueryFirstOrDefault<T>(sql, param);
        }

        public override int GetCount<T>(string where = null, object param = null)
        {
            DapperSqls sqls = GetDapperSqls(typeof(T));
            string sql = string.Format("SELECT COUNT(1) FROM [{0}] {1}", sqls.TableName, where);
            return ExecuteScalar<int>(sql, param);
        }

        public override IEnumerable<T> GetByIdsWhichField<T>(object ids, string whichField, string returnFields = null)
        {
            var sqls = GetDapperSqls(typeof(T));

            if (DapperCommon.ObjectIsEmpty(ids))
                return Enumerable.Empty<T>();

            Dapper.DynamicParameters dpar = new Dapper.DynamicParameters();
            dpar.Add("@ids", ids);
            if (string.IsNullOrEmpty(returnFields))
                returnFields = sqls.AllFields;
            string sql = string.Format("SELECT {0} FROM [{1}] WHERE [{2}] IN @ids", returnFields, sqls.TableName, whichField);
            return Query<T>(sql, dpar);
        }

        public override IEnumerable<T> GetBySkipTake<T>(int skip, int take, string where = null, string param = null, string returnFields = null, string orderBy = null)
        {
            DapperSqls sqls = GetDapperSqls(typeof(T));
            if (returnFields == null)
                returnFields = sqls.AllFields;

            if (orderBy == null)
            {
                if (!string.IsNullOrEmpty(sqls.KeyName))
                {
                    orderBy = string.Format("ORDER BY [{0}]", sqls.KeyName);
                }
                else
                {
                    orderBy = string.Format("ORDER BY [{0}]", sqls.AllFieldList.First());
                }
            }

            string sql = string.Format("SELECT {0} FROM [{1}] {2} {3} LIMIT {4},{5}", returnFields, sqls.TableName, where, orderBy, skip, take);

            return Query<T>(sql, param);
        }

        public override IEnumerable<T> GetByPageIndex<T>(int pageIndex, int pageSize, string where = null, string param = null, string returnFields = null, string orderBy = null)
        {
            int skip = 0;
            if (pageIndex > 0)
            {
                skip = (pageIndex - 1) * pageSize;
            }
            return GetBySkipTake<T>(skip, pageIndex, where, param, returnFields, orderBy);
        }

        public override IEnumerable<T> GetByPage<T>(int pageIndex, int pageSize, out int total, string where = null, string param = null, string returnFields = null, string orderBy = null)
        {
            DapperSqls sqls = GetDapperSqls(typeof(T));
            if (returnFields == null)
                returnFields = sqls.AllFields;

            if (orderBy == null)
            {
                if (!string.IsNullOrEmpty(sqls.KeyName))
                {
                    orderBy = string.Format("ORDER BY [{0}]", sqls.KeyName);
                }
                else
                {
                    orderBy = string.Format("ORDER BY [{0}]", sqls.AllFieldList.First());
                }
            }

            int skip = 0;
            if (pageIndex > 0)
            {
                skip = (pageIndex - 1) * pageSize;
            }

            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("SELECT COUNT(1) FROM [{0}] {1};", sqls.TableName, where);
            sb.AppendFormat("SELECT {0} FROM [{1}] {2} {3} LIMIT {4},{5}", returnFields, sqls.TableName, where, orderBy, skip, pageSize);
            
            QueryMultiple(sb.ToString(), param);
            total = reader.ReadFirstOrDefault<int>();
            return reader.Read<T>();
        }

        public override DataTable GetSchemaTable<T>(string returnFields = null)
        {
            DapperSqls sqls = GetDapperSqls(typeof(T));
            if (returnFields == null)
                returnFields = sqls.AllFields;

            string sql = string.Format("SELECT {0} FROM [{1}] LIMIT 0", returnFields, sqls.TableName);
            return QueryDataTable(sql);
        }
    }
}
