using System;
using System.Collections.Generic;

namespace MyConnections
{
    /// <summary>
    /// 多结果集读取器
    /// </summary>
    public class MyReader : IDisposable
    {
        private Dapper.SqlMapper.GridReader reader;

        public MyReader(Dapper.SqlMapper.GridReader reader)
        {
            this.reader = reader;
        }

        //读取列表返回dynamic
        public IEnumerable<dynamic> Read(bool buffered = true)
        {
            return reader.Read(buffered);
        }

        //读取列表返回T
        public IEnumerable<T> Read<T>(bool buffered = true)
        {
            return reader.Read<T>(buffered);
        }

        //读取列表返回dynamic
        public dynamic ReadFirstOrDefault()
        {
            return reader.ReadFirstOrDefault();
        }

        //读取一行结果集返回T
        public T ReadFirstOrDefault<T>()
        {
            return reader.ReadFirstOrDefault<T>();
        }

        public void Dispose()
        {
            if (reader != null)
                reader.Dispose();
        }
    }
}
