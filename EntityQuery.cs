using System.Data;
using System.Reflection;
using System.Runtime.CompilerServices;
using SqlKata;
using SqlKata.Execution;

namespace EntityKata;

public class EntityQuery<T>
{
    /// <summary>
    /// SQLKata QueryFactory 
    /// </summary>
    private readonly QueryFactory _kataFactory;

    /// <summary>
    /// Main type of the class
    /// </summary>
    private readonly Type _mainType;

    /// <summary>
    /// Cached Table names 
    /// </summary>
    private Dictionary<Type, string> _TableNames = new ();

    /// <summary>
    /// Cached columns names
    /// </summary>
    private Dictionary<Type, string[]> _ColumnNames = new ();

    /// <summary>
    /// Cached properties
    /// </summary>
    private Dictionary<Type, IEnumerable<PropertyInfo>> _properties = new ();

    /// <summary>
    /// Where clause
    /// </summary>
    //private List<KeyValuePair<string, object?>> _where = new List<KeyValuePair<string, object?>>();
    private Dictionary<string, object?> _where = new ();

    private List<string> _orderBy = new ();
    private bool _orderByAscending = true;

    public EntityQuery(QueryFactory sqlKataFactory)
    {
        _kataFactory = sqlKataFactory;
        _mainType = typeof(T);

        // Save Tablename and Fields with attributes
        updateTableNameAndFieldsCache(_mainType);
    }


    /// <summary>
    /// Order by single column ASC
    /// </summary>
    /// <param name="column"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public EntityQuery<T> OrderBy(string column, Type? type = null)
    {
        _orderByAscending = true;
        return Order(new[] {column}, type);
    }

    /// <summary>
    /// Order by single column DESC
    /// </summary>
    /// <param name="column"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public EntityQuery<T> OrderByDesc(string column, Type? type = null)
    {
        _orderByAscending = false;
        return Order(new[] {column}, type);
    }

    /// <summary>
    /// Order by multiple columns ASC
    /// </summary>
    /// <param name="columns"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public EntityQuery<T> OrderBy(string[] columns, Type? type = null)
    {
        _orderByAscending = true;
        return Order(columns, type);
    }

    /// <summary>
    /// Order by multiple columns DESC
    /// </summary>
    /// <param name="columns"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public EntityQuery<T> OrderByDesc(string[] columns, Type? type = null)
    {
        _orderByAscending = false;
        return Order(columns, type);
    }

    /// <summary>
    /// Order by multiple columns
    /// </summary>
    /// <param name="columns"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public EntityQuery<T> Order(string[] columns, Type? type = null)
    {
        if (columns is null) throw new ArgumentNullException(nameof(columns));

        foreach (var column in columns)
        {
            var typeOfColumnsObject = type ?? typeof(T);

            var referenceProperty = typeOfColumnsObject.GetProperty(column);
            if (referenceProperty is null)
                throw new ArgumentException($"Property {column} is not matching type {typeOfColumnsObject.Name}");

            var attribute = referenceProperty.GetCustomAttribute(typeof(FieldAttribute));
            if (attribute != null)
            {
                _orderBy.Add(_TableNames[typeOfColumnsObject] + "." + ((FieldAttribute) attribute).Name);
            }
        }

        return this;
    }

    public EntityQuery<T> Where(object where, Type? type = null)
    {
        if (where is null) throw new ArgumentNullException(nameof(where));

        var whereObjectType = where.GetType();

        var keyProperties =
            whereObjectType.GetProperties(BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
        foreach (var property in keyProperties)
        {
            var typeOfWhereClause = !IsAnonymous(where) ? where.GetType() : type ?? typeof(T);

            updateTableNameAndFieldsCache(typeOfWhereClause);

            var referenceProperty = typeOfWhereClause.GetProperty(property.Name);
            if (referenceProperty is null)
                throw new ArgumentException($"Property {property.Name} is not matching type {typeOfWhereClause.Name}");

            var attribute = referenceProperty.GetCustomAttribute(typeof(FieldAttribute));
            if (attribute != null)
            {
                // _where.Add(new KeyValuePair<string, object?>(
                //     _TableNames[typeOfWhereClause] + "." + ((FieldAttribute) attribute).Name,
                //     property.GetValue(where)));
                _where.Add(_TableNames[typeOfWhereClause] + "." + ((FieldAttribute) attribute).Name,
                    property.GetValue(where));
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
        if (_orderBy.Count > 0)
            throw new InvalidOperationException("Cannot use OrderBy()/OrderByDesc and Delete() together");

        return GetCurrentQuery().Delete(transaction);
    }
    
    /// <summary>
    /// Update one or more records
    /// </summary>
    /// <param name="transaction"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public int Update(object updateToValues, IDbTransaction transaction = null)
    {
        if (_orderBy.Count > 0)
            throw new InvalidOperationException("Cannot use OrderBy()/OrderByDesc and Update() together");

        var updateToValuesList = new Dictionary<string, object?>();
        
        var properties = updateToValues.GetType().GetProperties(BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {

            var referenceProperty = _mainType.GetProperty(property.Name);
            if (referenceProperty is null)
                throw new ArgumentException($"Property {property.Name} is not matching type {_mainType.Name}");
            
            var attribute = referenceProperty.GetCustomAttribute(typeof(FieldAttribute));
            if (attribute != null)
            {
                //updateToValuesList.Add(_TableNames[_mainType] + "." + ((FieldAttribute)attribute).Name, property.GetValue(updateToValues));
                updateToValuesList.Add(((FieldAttribute)attribute).Name, property.GetValue(updateToValues));
            }
        }
        
        return GetCurrentQuery().Update(updateToValuesList, transaction);
    }

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

        if (!_TableNames.TryAdd(type, ((TableAttribute) tableAttribue).Name)) return;

        var columns = new List<string>();

        var properties = type.GetProperties(BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
        _properties.Add(type, properties);
        foreach (var property in properties)
        {
            var attribute = property.GetCustomAttribute(typeof(FieldAttribute));
            if (attribute != null)
            {
                columns.Add(((FieldAttribute) attribute).Name);
            }
        }

        _ColumnNames.Add(type, columns.ToArray());
    }

    private Query GetCurrentQuery()
    {
        // columns
        var query = _kataFactory.Query(_TableNames[_mainType])
            .Select(_ColumnNames[_mainType]);

        // where
        if (_where.Count > 0) query.Where( _where);

        // orderby
        if (_orderBy.Count > 0)
        {
            if (_orderByAscending) query.OrderBy(_orderBy.ToArray());
            else query.OrderByDesc(_orderBy.ToArray());
        }

        return query;
    }

    /// <summary>
    /// True is there at least one record
    /// </summary>
    /// <returns></returns>
    public bool Exist(IDbTransaction transaction = null)
    {
        return GetCurrentQuery().Get(transaction).Any(); //Exists(transaction) solo nelle versioni successive di sqlkata 
    }

    /// <summary>
    /// Get First or Default
    /// </summary>
    /// <returns></returns>
    public T? FirstOrDefault(IDbTransaction transaction = null)
    {
        // var query = GetCurrentQuery();
        // var result = query.FirstOrDefault();
        // return FillNewInstanceWithData(GetCurrentQuery().FirstOrDefault());

        var record = GetCurrentQuery().FirstOrDefault(transaction);
        if (record is null) return default; 
        
        return FillWithData(record);
        
    }

    /// <summary>
    /// Gets filled entities from the database
    /// </summary>
    /// <returns></returns>
    public IEnumerable<T> Get(IDbTransaction transaction = null)
    {
        return FillAllWithData(GetCurrentQuery().Get(transaction));
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
        var valuesToInsert = new List<List<object?>>();
        // crea un elenco di valori per le colonne
        foreach (var singleObject in objectsToInsert)
        {
            var rowValues = new List<object?>();

            foreach (var property in _properties[_mainType])
            {
                var attribute = property.GetCustomAttribute(typeof(FieldAttribute));
                if (attribute != null)
                {
                    var identityAttribute = property.GetCustomAttribute(typeof(IdentityAttribute));
                    if (identityAttribute is null)
                        rowValues.Add(property.GetValue(singleObject));
                }
            }

            valuesToInsert.Add(rowValues);
        }

        return _kataFactory.Query(_TableNames[_mainType])
            .Insert(_ColumnNames[_mainType], valuesToInsert, transaction);
    }

    /// <summary>
    /// Insert an object and return its generated identity
    /// </summary>
    /// <param name="objectToInsert"></param>
    /// <typeparam name="R"></typeparam>
    /// <returns></returns>
    public R InsertGetId<R>(object objectToInsert, IDbTransaction transaction = null)
    {
        var dictionaryToInsert = new Dictionary<string, object?>();

        // reucpera le colonne da inserire
        foreach (var property in _properties[_mainType])
        {
            var attribute = property.GetCustomAttribute(typeof(FieldAttribute));
            if (attribute != null)
            {
                var identityAttribute = property.GetCustomAttribute(typeof(IdentityAttribute));
                if (identityAttribute is null)
                    dictionaryToInsert.Add(((FieldAttribute) attribute).Name, property.GetValue(objectToInsert));
            }
        }

        return _kataFactory.Query(_TableNames[_mainType])
            .InsertGetId<R>(dictionaryToInsert, transaction);
    }

    /// <summary>
    /// Returns paginated records
    /// </summary>
    /// <param name="page"></param>
    /// <param name="itemsPerPage"></param>
    /// <returns></returns>
    public IEnumerable<T> Paginate(int page = 1, int itemsPerPage = 10)
    {
        return FillAllWithData(GetCurrentQuery().Paginate(page, itemsPerPage).List);
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
            resultInstances.Add(FillWithData(record));
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

        return (T)instance;
    }

    private void FillNewInstanceWithData(IEnumerable<PropertyInfo> properties, object instance,
        IDictionary<string, object> dati)
    {
        foreach (var property in properties)
        {
            var attribute = property.GetCustomAttribute(typeof(FieldAttribute));
            if (attribute != null)
            {
                if (property.PropertyType == typeof(bool?) || property.PropertyType == typeof(bool))
                {
                    property.SetValue(instance, dati[((FieldAttribute) attribute).Name] is 0 ? false : true);
                }
                else
                {
                    property.SetValue(instance, dati[((FieldAttribute) attribute).Name]);
                }
            }
        }
    }
}