using System;
using System.Data;

namespace SmartParking.Api.Infrastructure
{
    internal static class Reader
    {
        public static int Int(IDataRecord row, string column) => Convert.ToInt32(row[column]);

        public static long Long(IDataRecord row, string column) => Convert.ToInt64(row[column]);

        public static double Double(IDataRecord row, string column) => Convert.ToDouble(row[column]);

        public static double? DoubleOrNull(IDataRecord row, string column)
        {
            var value = row[column];
            return value == DBNull.Value ? (double?)null : Convert.ToDouble(value);
        }

        public static string String(IDataRecord row, string column)
        {
            var value = row[column];
            return value == DBNull.Value ? null : Convert.ToString(value);
        }
    }
}
