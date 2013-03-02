﻿using System;
using System.Data;

using System.Data.SQLite;

namespace LinqToDB.DataProvider.SQLite
{
	using Mapping;
	using SqlProvider;

	public class SQLiteDataProvider : DataProviderBase
	{
		public SQLiteDataProvider()
			: this(ProviderName.SQLite, new SQLiteMappingSchema())
		{
		}

		protected SQLiteDataProvider(string name, MappingSchema mappingSchema)
			: base(name, mappingSchema)
		{
			SqlProviderFlags.IsSkipSupported       = false;
			SqlProviderFlags.IsSkipSupportedIfTake = true;

			SetCharField("char",  (r,i) => r.GetString(i).TrimEnd());
			SetCharField("nchar", (r,i) => r.GetString(i).TrimEnd());
		}

		public override Type ConnectionType { get { return typeof(SQLiteConnection); } }
		public override Type DataReaderType { get { return typeof(SQLiteDataReader); } }
		
		public override IDbConnection CreateConnection(string connectionString )
		{
			return new SQLiteConnection(connectionString);
		}

		public override ISqlProvider CreateSqlProvider()
		{
			return new SQLiteSqlProvider(SqlProviderFlags);
		}

		public override void SetParameter(IDbDataParameter parameter, string name, DataType dataType, object value)
		{
			base.SetParameter(parameter, "@" + name, dataType, value);
		}

		protected override void SetParameterType(IDbDataParameter parameter, DataType dataType)
		{
			switch (dataType)
			{
				case DataType.UInt32    : dataType = DataType.Int64;    break;
				case DataType.UInt64    : dataType = DataType.Decimal;  break;
				case DataType.DateTime2 : dataType = DataType.DateTime; break;
			}

			base.SetParameterType(parameter, dataType);
		}
	}
}
