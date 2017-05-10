using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyConnections.Common
{
    public class DapperCommon
    {
        /// <summary>
        /// 关键字处理[name] `name`
        /// 获取id,sex,name
        /// </summary>
        /// <param name="fieldList"></param>
        /// <param name="leftChar">左符号</param>
        /// <param name="rightChar">右符号</param>
        /// <returns></returns>
        public static string GetFieldsStr(IEnumerable<string> fieldList, string leftChar, string rightChar)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in fieldList)
            {
                sb.AppendFormat("{0}{1}{2}", leftChar, item, rightChar);

                if (item != fieldList.Last())
                {
                    sb.Append(",");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// //获取@id,@sex,@name
        /// </summary>
        /// <param name="fieldList"></param>
        /// <returns></returns>
        public static string GetFieldsAtStr(IEnumerable<string> fieldList)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in fieldList)
            {
                sb.AppendFormat("@{0}", item);

                if (item != fieldList.Last())
                {
                    sb.Append(",");
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// 关键字处理[name] `name`
        /// 获取id=@id,name=@name
        /// </summary>
        /// <param name="fieldList"></param>
        /// <param name="leftChar">左符号</param>
        /// <param name="rightChar">右符号</param>
        /// <returns></returns>
        public static string GetFieldsEqStr(IEnumerable<string> fieldList, string leftChar, string rightChar)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in fieldList)
            {
                sb.AppendFormat("{0}{1}{2}=@{1}", leftChar, item, rightChar);

                if (item != fieldList.Last())
                {
                    sb.Append(",");
                }
            }
            return sb.ToString();
        }

        public static IEnumerable GetMultiExec(object param)
        {
            return (param is IEnumerable && !(param is string || param is IEnumerable<KeyValuePair<string, object>>)) ? (IEnumerable)param : null;
        }

        /// <summary>
        /// 判断输入参数是否有个数，用于in判断
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public static bool ObjectIsEmpty(object param)
        {
            bool result = true;
            IEnumerable data = GetMultiExec(param);
            if (data != null)
            {
                foreach (var item in data)
                {
                    result = false;
                    break;
                }
            }
            return result;
        }

        public static DapperSqls GetDapperSqls(Type t)
        {
            Table table = t.GetCustomAttributes(false).FirstOrDefault(f => f is Table) as Table;
            if (table == null)
            {
                throw new Exception("类未标注Table的Attribute,请先标注");
            }
            else
            {
                DapperSqls DapperSqls = new DapperSqls();
                DapperSqls.TableName = table.TableName;
                DapperSqls.KeyName = table.KeyName;
                DapperSqls.IsIdentity = table.IsIdentity;
                DapperSqls.AllFieldList = new List<string>();
                DapperSqls.ExceptKeyFieldList = new List<string>();


                var allproperties = t.GetProperties();

                foreach (var item in allproperties)
                {
                    var igore = item.GetCustomAttributes(false).FirstOrDefault(f => f is Igore) as Igore;
                    if (igore == null)
                    {
                        DapperSqls.AllFieldList.Add(item.Name); //所有列

                        if (item.Name == DapperSqls.KeyName)
                            DapperSqls.KeyType = item.PropertyType;
                        else
                            DapperSqls.ExceptKeyFieldList.Add(item.Name); //去除主键后的所有列
                    }
                }

                return DapperSqls;
            }

        }

        public static string BuilderUpdateByIdSql(DapperSqls sqls, string updateFields, string leftChar, string rightChar)
        {
            string updateList = GetFieldsEqStr(updateFields.Split(',').ToList(), leftChar, rightChar);
            string sql = string.Format("UPDATE {0}{1}{2} SET {3} WHERE {0}{4}{2}=@{4}", leftChar, sqls.TableName, rightChar, updateList, sqls.KeyName);
            return sql;
        }
        public static string BuilderUpdateByWhereSql(DapperSqls sqls, string where, string updateFields, string leftChar, string rightChar)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("UPDATE {0}{1}{2} SET ", leftChar, sqls.TableName, rightChar);
            if (string.IsNullOrEmpty(updateFields)) //修改所有
            {
                if (!string.IsNullOrEmpty(sqls.KeyName)) //有主键
                    sb.AppendFormat(sqls.AllFieldsAtEqExceptKey);
                else
                    sb.AppendFormat(sqls.AllFieldsAtEq);
            }
            else
            {
                string updateList = GetFieldsEqStr(updateFields.Split(',').ToList(), leftChar, rightChar);
                sb.Append(updateList);
            }
            sb.Append(" ");
            sb.Append(where);

            return sb.ToString();
        }

    }
}
