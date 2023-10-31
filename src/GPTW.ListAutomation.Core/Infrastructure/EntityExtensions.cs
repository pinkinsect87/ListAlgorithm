using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Data;
using System.Text;

namespace GPTW.ListAutomation.Core.Infrastructure
{
    public static class EntityExtensions
    {
        public static List<string> GetTableColumnNames<T>(this IList<T> data)
        {
            var colNames = new List<string>();
            var type = typeof(T);
            var properties = TypeDescriptor.GetProperties(type);

            foreach (PropertyDescriptor prop in properties)
            {
                var attributes = prop.PropertyType.GetCustomAttributes(false);
                var isPrimary = IsPrimaryKey(attributes);

                var colType = GetSqlDataType(prop.PropertyType, isPrimary);

                if (isPrimary && colType == "int")
                {
                    colNames.Add($"{prop.Name} {colType} IDENTITY(1,1) NOT NULL");
                }
                else
                {
                    colNames.Add($"{prop.Name} {colType} ");
                }
            }

            return colNames;
        }

        public static DataTable ConvertToDataTable<T>(this IList<T> data)
        {
            var type = typeof(T);
            var properties = TypeDescriptor.GetProperties(type);
            var table = new DataTable();

            foreach (PropertyDescriptor prop in properties)
            {
                table.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
            }

            foreach (T item in data)
            {
                var row = table.NewRow();
                foreach (PropertyDescriptor prop in properties)
                    row[prop.Name] = prop.GetValue(item) ?? DBNull.Value;
                table.Rows.Add(row);
            }

            return table;
        }

        private static bool IsPrimaryKey(object[] attributes)
        {
            bool skip = false;
            foreach (var attr in attributes)
            {
                if (attr.GetType() == typeof(PrimaryKeyAttribute))
                {
                    skip = true;
                }
            }
            return skip;
        }

        private static string GetSqlDataType(Type type, bool isPrimary = false)
        {
            var sqlType = new StringBuilder();
            var isNullable = false;
            if (Nullable.GetUnderlyingType(type) != null)
            {
                isNullable = true;
                type = Nullable.GetUnderlyingType(type);
            }
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.String:
                    isNullable = true;
                    sqlType.Append("nvarchar(MAX)");
                    break;
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Int16:
                    sqlType.Append("int");
                    break;
                case TypeCode.Boolean:
                    sqlType.Append("bit");
                    break;
                case TypeCode.DateTime:
                    sqlType.Append("datetime");
                    break;
                case TypeCode.Decimal:
                case TypeCode.Double:
                    sqlType.Append("decimal");
                    break;
            }
            if (!isNullable || isPrimary)
            {
                sqlType.Append(" NOT NULL");
            }
            return sqlType.ToString();
        }
    }
}
