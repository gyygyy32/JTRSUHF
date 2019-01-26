using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RFIDForm
{
    public class myDBHelp
    {
        public string ConnectionString { get; private set; }
        public  DbProviderType DBType { get; set; }
        //private DbProviderFactory providerFactory;
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="connectionString">数据库连接字符串</param>
        /// <param name="providerType">数据库类型枚举，参见<paramref name="providerType"/></param>
        public myDBHelp(string connectionString, DbProviderType dbtype)
        {
            ConnectionString = connectionString;
            DBType = dbtype;
            
        }
        public DbConnection createConn()
        {
            DbConnection dbconn = null;
            if (DBType == DbProviderType.Oracle)
            {
                dbconn = new OracleConnection(ConnectionString);
            }
            return dbconn;
        }

        public DbDataAdapter createAdapter(DbConnection conn)
        {
            DbDataAdapter adadapter = null;
            if (DBType == DbProviderType.Oracle)
            {
                
                adadapter = new OracleDataAdapter(null,(OracleConnection)conn);
            }
            return adadapter;
        }

        /// <summary>   
        /// 对数据库执行增删改操作，返回受影响的行数。   
        /// </summary>   
        /// <param name="sql">要执行的增删改的SQL语句</param>   
        /// <param name="parameters">执行增删改语句所需要的参数</param>
        /// <returns></returns>  
        public int ExecuteNonQuery(string sql, IList<DbParameter> parameters)
        {
            return ExecuteNonQuery(sql, parameters, CommandType.Text);
        }
        //public int ExecuteNonQuery(DbTransaction dbtrans, string sql, IList<DbParameter> parameters)
        //{
        //    return ExecuteNonQuery(dbtrans, sql, parameters, CommandType.Text);
        //}
        /// <summary>   
        /// 对数据库执行增删改操作，返回受影响的行数。   
        /// </summary>   
        /// <param name="sql">要执行的增删改的SQL语句</param>   
        /// <param name="parameters">执行增删改语句所需要的参数</param>
        /// <param name="commandType">执行的SQL语句的类型</param>
        /// <returns></returns>
        public int ExecuteNonQuery(string sql, IList<DbParameter> parameters, CommandType commandType)
        {

            using (DbCommand command = CreateDbCommand(sql, parameters, commandType))
            {
                command.Connection.Open();
                int affectedRows = command.ExecuteNonQuery();
                command.Connection.Close();
                return affectedRows;
            }
        }
        //public int ExecuteNonQuery(DbTransaction dbtrans, string sql, IList<DbParameter> parameters, CommandType commandType)
        //{

        //    using (DbCommand command = CreateDbCommand(dbtrans, sql, parameters, commandType))
        //    {
        //        //command.Connection.Open();

        //        int affectedRows = command.ExecuteNonQuery();
        //        //command.Connection.Close();
        //        return affectedRows;
        //    }
        //}
        /// <summary>   
        /// 执行一个查询语句，返回一个关联的DataReader实例   
        /// </summary>   
        /// <param name="sql">要执行的查询语句</param>   
        /// <param name="parameters">执行SQL查询语句所需要的参数</param>
        /// <returns></returns> 
        public DbDataReader ExecuteReader(string sql, IList<DbParameter> parameters)
        {
            return ExecuteReader(sql, parameters, CommandType.Text);
        }
        /// <summary>   
        /// 执行一个查询语句，返回一个关联的DataReader实例   
        /// </summary>   
        /// <param name="sql">要执行的查询语句</param>   
        /// <param name="parameters">执行SQL查询语句所需要的参数</param>
        /// <param name="commandType">执行的SQL语句的类型</param>
        /// <returns></returns> 
        public DbDataReader ExecuteReader(string sql, IList<DbParameter> parameters, CommandType commandType)
        {
            DbDataReader reader = null;
            DbCommand command = CreateDbCommand(sql, parameters, commandType);
            try
            {

                command.Connection.Open();
                reader = command.ExecuteReader(CommandBehavior.CloseConnection);
                //command.Connection.Close();
                return reader;
            }
            catch (Exception ex)
            {
                command.Connection.Close();
                throw new Exception(ex.ToString());

            }


        }
        /// <summary>   
        /// 执行一个查询语句，返回一个包含查询结果的DataTable   
        /// </summary>   
        /// <param name="sql">要执行的查询语句</param>   
        /// <param name="parameters">执行SQL查询语句所需要的参数</param>
        /// <returns></returns>
        public DataTable ExecuteDataTable(string sql, IList<DbParameter> parameters)
        {
            return ExecuteDataTable(sql, parameters, CommandType.Text);
        }
        /// <summary>   
        /// 执行一个查询语句，返回一个包含查询结果的DataTable   
        /// </summary>   
        /// <param name="sql">要执行的查询语句</param>   
        /// <param name="parameters">执行SQL查询语句所需要的参数</param>
        /// <param name="commandType">执行的SQL语句的类型</param>
        /// <returns></returns>
        public DataTable ExecuteDataTable(string sql, IList<DbParameter> parameters, CommandType commandType)
        {
            using (DbCommand command = CreateDbCommand(sql, parameters, commandType))
            {
                using (DbDataAdapter adapter = createAdapter(command.Connection))//providerFactory.CreateDataAdapter())
                {

                    adapter.SelectCommand = command;
                    DataTable data = new DataTable();
                    adapter.Fill(data);
                    return data;
                }
            }
        }
        /// <summary>   
        /// 执行一个查询语句，返回查询结果的第一行第一列   
        /// </summary>   
        /// <param name="sql">要执行的查询语句</param>   
        /// <param name="parameters">执行SQL查询语句所需要的参数</param>   
        /// <returns></returns>   
        public Object ExecuteScalar(string sql, IList<DbParameter> parameters)
        {
            return ExecuteScalar(sql, parameters, CommandType.Text);
        }

        /// <summary>   
        /// 执行一个查询语句，返回查询结果的第一行第一列   
        /// </summary>   
        /// <param name="sql">要执行的查询语句</param>   
        /// <param name="parameters">执行SQL查询语句所需要的参数</param>   
        /// <param name="commandType">执行的SQL语句的类型</param>
        /// <returns></returns>   
        public Object ExecuteScalar(string sql, IList<DbParameter> parameters, CommandType commandType)
        {
            using (DbCommand command = CreateDbCommand(sql, parameters, commandType))
            {
                command.Connection.Open();
                object result = command.ExecuteScalar();
                command.Connection.Close();
                return result;
            }
        }
        /// <summary>
        /// 创建一个DbCommand对象
        /// </summary>
        /// <param name="sql">要执行的查询语句</param>   
        /// <param name="parameters">执行SQL查询语句所需要的参数</param>
        /// <param name="commandType">执行的SQL语句的类型</param>
        /// <returns></returns>
        private DbCommand CreateDbCommand(string sql, IList<DbParameter> parameters, CommandType commandType)
        {
            DbConnection connection = createConn(); //providerFactory.CreateConnection();

            DbCommand command = connection.CreateCommand();
            connection.ConnectionString = ConnectionString;
            command.CommandText = sql;
            command.CommandType = commandType;
            command.Connection = connection;

            if (!(parameters == null || parameters.Count == 0))
            {
                foreach (DbParameter parameter in parameters)
                {
                    command.Parameters.Add(parameter);
                }
            }
            return command;
        }

        //private DbCommand CreateDbCommand(DbTransaction dbtrans, string sql, IList<DbParameter> parameters, CommandType commandType)
        //{
        //    DbConnection connection = dbtrans.Connection;
        //    DbCommand command = providerFactory.CreateCommand();

        //    //connection.ConnectionString = ConnectionString;
        //    command.CommandText = sql;
        //    command.CommandType = commandType;
        //    command.Connection = connection;
        //    command.Transaction = dbtrans;
        //    if (!(parameters == null || parameters.Count == 0))
        //    {
        //        foreach (DbParameter parameter in parameters)
        //        {
        //            command.Parameters.Add(parameter);

        //        }
        //    }
        //    return command;
        //}

    }

    //public enum DbProviderType : byte
    //{
    //    SqlServer,
    //    MySql,
    //    SQLite,
    //    Oracle,
    //    ODBC,
    //    OleDb,
    //    Firebird,
    //    PostgreSql,
    //    DB2,
    //    Informix,
    //    SqlServerCe
    //}
}
