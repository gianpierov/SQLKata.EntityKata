using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using SqlKata;
using SqlKata.Execution;

namespace EntityKata
{
    public class EntityManager<T>
    {
        /// <summary>
        /// SQLKata QueryFactory 
        /// </summary>
        private readonly QueryFactory _kataFactory;

        public Query Query;

        /// <summary>
        /// Main type of the class
        /// </summary>
        private readonly Type _mainType;

        /// <summary>
        /// Last table used in the query
        /// </summary>
        private Type _lastJoinedType;

        /// <summary>
        /// Cached Table names 
        /// </summary>
        private Dictionary<Type, string> _TableNames = new Dictionary<Type, string>();

        /// <summary>
        /// Cached columns names
        /// </summary>
        private Dictionary<Type, string[]> _ColumnNames = new Dictionary<Type, string[]>();

        private Dictionary<string, string> _propertieNamesByFieldName = new Dictionary<string, string>();

        private Dictionary<Type, string[]> _ColumnNamesWithoutAutoincrement = new Dictionary<Type, string[]>();

        /// <summary>
        /// Cached properties
        /// </summary>
        private Dictionary<Type, IEnumerable<PropertyInfo>> _properties = new Dictionary<Type, IEnumerable<PropertyInfo>>();

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="sqlKataFactory"></param>
        public EntityManager(QueryFactory sqlKataFactory)
        {
            _kataFactory = sqlKataFactory;
            _mainType = typeof(T);

            // Save Tablename and Fields with attributes for the mainType
            updateTableNameAndFieldsCache(_mainType);

            InitializeQuery();
        }

        /// <summary>
        /// resets the query object to the initial state 
        /// </summary>
        private void InitializeQuery()
        {
            Query = _kataFactory.Query(_TableNames[_mainType]);
            _lastJoinedType = _mainType;
        }


        /// <summary>
        /// Order by single column ASC
        /// </summary>
        /// <param name="column">Column to use</param>
        /// <param name="type">Optional type if the column belongs to a joined table</param>
        /// <returns></returns>
        public EntityManager<T> OrderBy(string column, Type type = null)
        {
            Query.OrderBy(Order(new[] {column}, type).ToArray());
            return this;
        }

        /// <summary>
        /// Order by single column DESC
        /// </summary>
        /// <param name="column">Column to use</param>
        /// <param name="type">Optional type if the column belongs to a joined table</param>
        /// <returns></returns>
        public EntityManager<T> OrderByDesc(string column, Type type = null)
        {
            Query.OrderByDesc(Order(new[] {column}, type).ToArray());
            return this;
        }

        /// <summary>
        /// Order by multiple columns ASC
        /// </summary>
        /// <param name="columns">Multiple columns used to order</param>
        /// <param name="type">Optional type if the columns belong to a joined table</param>
        /// <returns></returns>
        public EntityManager<T> OrderBy(string[] columns, Type type = null)
        {
            Query.OrderBy(Order(columns, type).ToArray());
            return this;
        }

        /// <summary>
        /// Order by multiple columns DESC
        /// </summary>
        /// <param name="columns">Multiple columns used to order</param>
        /// <param name="type">Optional type if the columns belong to a joined table</param>/// <param name="type"></param>
        /// <returns></returns>
        public EntityManager<T> OrderByDesc(string[] columns, Type type = null)
        {
            Query.OrderByDesc(Order(columns, type).ToArray());
            return this;
        }

        /// <summary>
        /// OrderBy using anonymous object
        /// </summary>
        /// <param name="columns">object with columns name</param>
        /// <param name="type">Referring entity type</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public EntityManager<T> Order(object columns, Type type = null)
        {
            if (type == null) type = _mainType;
            
            if (columns is null) throw new ArgumentNullException(nameof(columns));
            
            var perperties = columns.GetType().GetProperties(BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            foreach (var property in perperties)
            {
                var referenceProperty = type.GetProperty(property.Name);
                if (referenceProperty == null) throw new ArgumentException("Column not in main type");
            
                var value = property.GetValue(columns);

                if (!Enum.TryParse(value.ToString(), out Ordering ordering)) throw new ArgumentException("Invalid ordering");

                switch (ordering)
                {
                    case Ordering.Ascending: OrderBy(property.Name, type);
                        break;
                    case  Ordering.Descending: OrderByDesc(property.Name, type);
                        break;
                    default:
                        throw new ArgumentException("Invalid ordering");
                } 
            }

            return this;
        }

        /// <summary>
        /// Order by multiple columns
        /// </summary>
        /// <param name="columns"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        private List<string> Order(string[] columns, Type type = null)
        {
            if (columns is null) throw new ArgumentNullException(nameof(columns));

            var orderByColumnsList = new List<string>();

            foreach (var column in columns)
            {
                var typeOfColumnsObject = type ?? typeof(T);

                var referenceProperty = typeOfColumnsObject.GetProperty(column);
                if (referenceProperty is null)
                    throw new ArgumentException($"Property {column} is not matching type {typeOfColumnsObject.Name}");

                var attribute = referenceProperty.GetCustomAttribute(typeof(FieldAttribute));
                if (attribute != null)
                {
                    orderByColumnsList.Add(_TableNames[typeOfColumnsObject] + "." + ((FieldAttribute) attribute).Name);
                }
            }

            return orderByColumnsList;
        }

        /// <summary>
        /// Where clause using expression
        /// </summary>
        /// <param name="where"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public EntityManager<T> Where(Expression<Func<object, object>> where, Type type = null)
        {
            var fieldName = where.Parameters[0].Name;
            var value = ((ConstantExpression) ((UnaryExpression) where.Body).Operand).Value;

            var referenceTypeForWhereClause = type ?? typeof(T);

            var property = referenceTypeForWhereClause.GetProperty(fieldName);
            if (property == null) throw new ArgumentException("Property not found for the the reference type");
            
            var attribute = property.GetCustomAttribute(typeof(FieldAttribute));
            if (attribute == null) throw new ArgumentException("Not a field");
             
            Query.Where(_TableNames[referenceTypeForWhereClause] + "." + ((FieldAttribute) attribute).Name, value);
            
            return this;
        }


        /// <summary>
        /// Where clause
        /// </summary>
        /// <param name="where"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public EntityManager<T> Where(object where, Type type = null)
        {

            if (where is null) throw new ArgumentNullException(nameof(where));

            var whereObjectType = where.GetType();
            var typeOfWhereClause = !IsAnonymous(where) ? where.GetType() : type ?? typeof(T);
            updateTableNameAndFieldsCache(typeOfWhereClause);

            var keyProperties =
                whereObjectType.GetProperties(BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            foreach (var property in keyProperties)
            {
                
                var referenceProperty = typeOfWhereClause.GetProperty(property.Name);
                if (referenceProperty is null)
                    throw new ArgumentException($"Property {property.Name} is not matching type {typeOfWhereClause.Name}");

                var attribute = referenceProperty.GetCustomAttribute(typeof(FieldAttribute));
                if (attribute != null)
                {

                    var fieldName = _TableNames[typeOfWhereClause] + "." + ((FieldAttribute) attribute).Name;
                    var value = property.GetValue(where);

                    if (value == null || value.GetType().IsPrimitive || value is string)
                    {
                        Query.Where(fieldName, value);
                    }
                    else
                    {
                        var comparisonOperator = "";
                        
                        switch (value.GetType().ToString())
                        {
                            case "EntityKata.EqualTo":
                                comparisonOperator = "=";
                                break;
                            case "EntityKata.GreaterThan":
                                comparisonOperator = ">";
                                break;
                            case "EntityKata.GreaterThanOrEqualTo":
                                comparisonOperator = ">=";
                                break;
                            case "EntityKata.LessThan":
                                comparisonOperator = "<";
                                break;
                            case "EntityKata.LessThanOrEqualTo":
                                comparisonOperator = "<=";
                                break;
                            default:
                                throw new ArgumentException("Bad comparsion type");
                            
                        }

                        var innerProperty = value.GetType().GetProperties()[0];
                        if (innerProperty.Name != "Value") throw new ArgumentException("Bad comparsion type");
                        
                        Query.Where(fieldName, comparisonOperator, innerProperty.GetValue(value));
                    }
                    
                }
            }
            
            return this;
        }

        /// <summary>
        /// Delete records from table
        /// </summary>
        /// <returns></returns>
        public int Delete(IDbTransaction transaction = null)
        {
            var returnValue = Query.Delete(transaction);
            InitializeQuery();
            return returnValue;
        }

        /// <summary>
        /// Update one or more records
        /// </summary>
        /// <param name="transaction"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public int Update(object updateToValues, IDbTransaction transaction = null)
        {
            var updateToValuesList = new Dictionary<string, object>();

            var properties = updateToValues.GetType()
                .GetProperties(BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties)
            {
                var referenceProperty = _mainType.GetProperty(property.Name);
                if (referenceProperty is null)
                    throw new ArgumentException($"Property {property.Name} is not matching type {_mainType.Name}");

                var attribute = referenceProperty.GetCustomAttribute(typeof(FieldAttribute));
                if (attribute != null)
                {
                    //updateToValuesList.Add(_TableNames[_mainType] + "." + ((FieldAttribute)attribute).Name, property.GetValue(updateToValues));
                    updateToValuesList.Add(((FieldAttribute) attribute).Name, property.GetValue(updateToValues));
                }
            }

            var returnValue = Query.Update(updateToValuesList, transaction);
            InitializeQuery();
            return returnValue;
        }

        /// <summary>
        /// Check if the object used an argument is anonymous type
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private static bool IsAnonymous(object obj)
        {
            var type = obj.GetType();
            return Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), false)
                   && type.IsGenericType && type.Name.Contains("AnonymousType")
                   && (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$"))
                   && type.Attributes.HasFlag(TypeAttributes.NotPublic);
        }

        /// <summary>
        /// save TableName and Fields with attributes
        /// </summary>
        /// <param name="type"></param>
        private void updateTableNameAndFieldsCache(Type type)
        {
            var tableAttribue = Attribute.GetCustomAttribute(type, typeof(TableAttribute));
            if (tableAttribue is null) throw new ArgumentException("Not an usable entity: Missing Table attribute");

            // change for old framework compatibility
            if (_TableNames.TryGetValue(type, out _)) return;
            _TableNames.Add(type, ((TableAttribute) tableAttribue).Name);

            //var propertyNamesDictionary = new Dictionary<string, string>();

            var columns = new List<string>();
            var columnsNoIdentities = new List<string>();

            var properties = type.GetProperties(BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            _properties.Add(type, properties);
            foreach (var property in properties)
            {
                var attribute = property.GetCustomAttribute(typeof(FieldAttribute));

                if (attribute == null) continue;

                var fieldName = ((FieldAttribute) attribute).Name;
                var columnName = _TableNames[type] + "." + fieldName;
                columns.Add(columnName);
                var identityAttribute = property.GetCustomAttribute(typeof(AutoIncrementAttribute));
                if (identityAttribute is null) columnsNoIdentities.Add(columnName);
                //propertyNamesDictionary.Add(property.Name, columnName);
                
                // skip if the name is already in the dictionary
                if (_propertieNamesByFieldName.TryGetValue(fieldName, out _)) continue;
                _propertieNamesByFieldName.Add(fieldName, property.Name);
            }

            _ColumnNames.Add(type, columns.ToArray());
            _ColumnNamesWithoutAutoincrement.Add(type, columnsNoIdentities.ToArray());

            _lastJoinedType = type;
        }

        /// <summary>
        /// Get columns name and set the select statement 
        /// </summary>
        /// <returns></returns>
        private Query GetSelect()
        {
            var columnsNames = new List<string>();
            foreach (var columns in _ColumnNames)
            {
                columnsNames.AddRange(columns.Value);
            }
            
            return Query.Select(columnsNames.ToArray());
        }

        /// <summary>
        /// True is there at least one record
        /// </summary>
        /// <returns></returns>
        public bool Exists(IDbTransaction transaction = null)
        {
            var returnValue =
                GetSelect().Get(transaction).Any(); //Exists(transaction) to change with new version of SQLKata
            InitializeQuery();
            return returnValue;
        }

        /// <summary>
        /// Get First or Default
        /// </summary>
        /// <returns></returns>
        public T FirstOrDefault(IDbTransaction transaction = null)
        {
            // var query = GetCurrentQuery();
            // var result = query.FirstOrDefault();
            // return FillNewInstanceWithData(GetCurrentQuery().FirstOrDefault());

            var record = GetSelect().FirstOrDefault(transaction);
            if (record is null) return default;

            T returnValue = this.FillWithData((object) record);
            InitializeQuery();
            return returnValue;
        }
        

        /// <summary>
        /// Gets filled entities from the database
        /// </summary>
        /// <returns></returns>
        public IEnumerable<T> Get(IDbTransaction transaction = null)
        {
            var returnValue = FillAllWithData(GetSelect().Get(transaction));
            InitializeQuery();
            return returnValue;
        }
        
        public dynamic GetDynamic(IDbTransaction transaction = null)
        {
            var returnValue = fillDynamicWithRecords(GetSelect().Get(transaction));
            InitializeQuery();
            return returnValue;
        }

        /// <summary>
        /// Fills a dynamic object with the data from the database, using the entitis properties names as keys
        /// </summary>
        /// <param name="records"></param>
        /// <returns></returns>
        private dynamic fillDynamicWithRecords(dynamic records)
        {
            var newMappedRecord = new List<ExpandoObject>();

            foreach (IDictionary<string, object> record in records as List<object>)
            {
                var newRecord = new ExpandoObject();
                var newRecordDictionary = (IDictionary<string, object>) newRecord;
                
                    foreach (var item in record)
                    {
                        if (newRecordDictionary.TryGetValue(_propertieNamesByFieldName[item.Key], out _)) continue;
                        newRecordDictionary.Add(_propertieNamesByFieldName[item.Key], item.Value);
                    }

                    newMappedRecord.Add(newRecord);
            }
            
            return newMappedRecord;
        }

        /// <summary>
        /// Insert single record
        /// </summary>
        /// <param name="objectsToInsert"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public int Insert(object objectsToInsert, IDbTransaction transaction = null)
        {
            return Insert(new List<object> {objectsToInsert}, transaction);
        }

        /// <summary>
        /// Insert multiple records
        /// </summary>
        /// <param name="objectsToInsert"></param>
        /// <returns></returns>
        public int Insert(IEnumerable<object> objectsToInsert, IDbTransaction transaction = null)
        {
            var valuesToInsert = new List<List<object>>();
            // crea un elenco di valori per le colonne
            foreach (var singleObject in objectsToInsert)
            {
                var rowValues = new List<object>();

                foreach (var property in _properties[_mainType])
                {
                    var attribute = property.GetCustomAttribute(typeof(FieldAttribute));
                    if (attribute != null)
                    {
                        var identityAttribute = property.GetCustomAttribute(typeof(AutoIncrementAttribute));
                        if (identityAttribute is null)
                            rowValues.Add(property.GetValue(singleObject));
                    }
                }

                valuesToInsert.Add(rowValues);
            }

            var returnValue = Query.Insert(_ColumnNamesWithoutAutoincrement[_mainType], valuesToInsert, transaction);
            InitializeQuery();
            return returnValue;
        }

        /// <summary>
        /// Insert an object and return its generated identity
        /// </summary>
        /// <param name="objectToInsert"></param>
        /// <typeparam name="R"></typeparam>
        /// <returns></returns>
        public R InsertGetId<R>(object objectToInsert, IDbTransaction transaction = null)
        {
            var dictionaryToInsert = new Dictionary<string, object>();

            // reucpera le colonne da inserire
            foreach (var property in _properties[_mainType])
            {
                var attribute = property.GetCustomAttribute(typeof(FieldAttribute));
                if (attribute != null)
                {
                    var identityAttribute = property.GetCustomAttribute(typeof(AutoIncrementAttribute));
                    if (identityAttribute is null)
                        dictionaryToInsert.Add(((FieldAttribute) attribute).Name, property.GetValue(objectToInsert));
                }
            }

            var returnValue = _kataFactory.Query(_TableNames[_mainType])
                .InsertGetId<R>(dictionaryToInsert, transaction);
            InitializeQuery();
            return returnValue;
        }

        /// <summary>
        /// Returns paginated records
        /// </summary>
        /// <param name="page"></param>
        /// <param name="itemsPerPage"></param>
        /// <returns></returns>
        public IEnumerable<T> Paginate(int page = 1, int itemsPerPage = 10)
        {
            var returnValue = FillAllWithData(GetSelect().Paginate(page, itemsPerPage).List);
            InitializeQuery();
            return returnValue;
        }

        /// <summary>
        /// Fills all entities with data from the records
        /// </summary>
        /// <param name="records"></param>
        /// <returns></returns>
        private IEnumerable<T> FillAllWithData(IEnumerable<object> records)
        {
            var resultInstances = new List<T>();

            foreach (var record in records)
            {
                resultInstances.Add(this.FillWithData(record));
            }

            return resultInstances;
        }

        /// <summary>
        /// Fill single record
        /// </summary>
        /// <param name="record"></param>
        /// <returns></returns>
        private T FillWithData(object record)
        {
            var data = (IDictionary<string, object>) record;

            var instance = Activator.CreateInstance(_mainType);
            FillNewInstanceWithData(_properties[_mainType], instance, data);

            return (T) instance;
        }

        private void FillNewInstanceWithData(IEnumerable<PropertyInfo> properties, object instance,
            IDictionary<string, object> dati)
        {
            foreach (var property in properties)
            {
                var attribute = property.GetCustomAttribute(typeof(FieldAttribute));
                if (attribute == null) continue;
                if (property.PropertyType == typeof(bool?) || property.PropertyType == typeof(bool))
                {
                    property.SetValue(instance, dati[((FieldAttribute) attribute).Name] != (object) 0);
                }
                else
                {
                    property.SetValue(instance, dati[((FieldAttribute) attribute).Name]);
                }
            }
        }

        /// <summary>
        /// Join clause
        /// </summary>
        /// <param name="columns"></param>
        /// <typeparam name="J"></typeparam>
        public EntityManager<T> Join<J>(Expression<Func<object, Expression<Func<object, object>>>> columns)
        {
           return Join<J>(new [] {columns});
        }
        
        /// <summary>
        /// Join clause, multiple columns
        /// </summary>
        /// <param name="columnsList"></param>
        /// <typeparam name="TJ"></typeparam>
        public EntityManager<T> Join<TJ>(params Expression<Func<object, Expression<Func<object, object>>>>[] columnsList)
        {
            var previousType = _lastJoinedType; 
            updateTableNameAndFieldsCache(typeof(TJ));

            foreach (var columns in columnsList)
            {
                var first = columns.Parameters[0].Name;
                var leftTableProperty = previousType.GetProperty(first);
                if (leftTableProperty == null) throw new ArgumentException("Property not in the left table type");
                var leftTableColumAttribute = leftTableProperty.GetCustomAttribute(typeof(FieldAttribute));
                if (leftTableColumAttribute == null) throw new ArgumentException($"Property {leftTableProperty.Name} is not a column");
                var firstColumnName = _TableNames[previousType] + "." + ((FieldAttribute)leftTableColumAttribute).Name;
                
                var second = ((Expression<Func<object, object>>) ((UnaryExpression) columns.Body).Operand)
                    .Parameters[0].Name;
                
                var rightTableProperty = _lastJoinedType.GetProperty(second);
                if (rightTableProperty == null) throw new ArgumentException("Property not in the right table type");
                var rightTableColumnAttribute = rightTableProperty.GetCustomAttribute(typeof(FieldAttribute));
                if (rightTableColumnAttribute == null) throw new ArgumentException($"Property {rightTableProperty.Name} is not a column");
                
                var secondColumnName = _TableNames[_lastJoinedType] + "." + ((FieldAttribute)rightTableColumnAttribute).Name;
                
                // var value =
                //     ((ConstantExpression)
                //         ((UnaryExpression) ((Expression<Func<object, object>>) ((UnaryExpression) function.Body).Operand)
                //             .Body).Operand).Value;
                var value =
                    ((ConstantExpression) ((Expression<Func<object, object>>) ((UnaryExpression) columns.Body).Operand)
                        .Body).Value;

                Query.Join(_TableNames[_lastJoinedType], firstColumnName, secondColumnName, value.ToString());
            
            }

            return this;
        }
        
        // public void JoinTemp<T1>(Expression<Func<object, object>> function)
        // {
        //     Console.WriteLine(function.Parameters[0].Name);
        //
        //     Console.WriteLine(((NewExpression)function.Body).Members[0].Name);
        //     
        //     Console.WriteLine(((NewExpression)function.Body).Arguments[0]);
        //     
        // }
        
    }
}
