using System.Data.SqlClient;
using System.Reflection;
using System.Transactions;
using SqlKata.Compilers;
using SqlKata.Execution;
using IsolationLevel = System.Transactions.IsolationLevel;

namespace EntityKata;

/// <summary>
/// Gestione delle entità usando SQLKata 
/// </summary>
public class EntityKataManagerOld : IDisposable
{
    private bool _disposedValue;

    /// <summary>
    /// Oggetto QueryFactory
    /// </summary>
    public QueryFactory Factory { get; }

    /// <summary>
    /// Costruttore
    /// </summary>
    public EntityKataManagerOld(string connectionString)
    {
        //Factory = new QueryFactory(new SqlConnection(AppSettings.Configuration["ConnectionString:IFILAV"]), new SqlServerCompiler());
        Factory = new QueryFactory(new SqlConnection(connectionString), new SqlServerCompiler());
    }

    /// <summary>
    /// Dispose per la gestione della chiusura della connessione al DB
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Dispose per la gestione della chiusura della connessione al DB
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposedValue) return;
        if (disposing && Factory.Connection != null)
        {
            Factory.Connection.Dispose();
            Factory.Connection = null;
        }
        _disposedValue = true;
    }

    /// <summary>
    /// inizia una transazione sulla connessione corrente
    /// </summary>
    /// <returns></returns>
    public static TransactionScope BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.ReadUncommitted)
    {
        return new TransactionScope(TransactionScopeOption.Required, new TransactionOptions
        {
            IsolationLevel = isolationLevel
        }, TransactionScopeAsyncFlowOption.Enabled);
    }
    
    /// <summary>
    /// Gestore delle entità 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public EntityQuery<T> Query<T>()
    {
        return new EntityQuery<T>(Factory);
    }
    

    /// <summary>
    /// Insert di un singolo oggetto
    /// </summary>
    /// <param name="objectToInsert"></param>
    /// <typeparam name="T"></typeparam>
    public int Insert<T>(T objectToInsert)
    {
        return MultiInsert(new List<T> { objectToInsert });
    }
    
    /// <summary>
    /// Inserisce un record e restituisce l'id generato nel database
    /// </summary>
    /// <param name="objectToInsert"></param>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="R"></typeparam>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public R InsertGetId<T, R>(T objectToInsert)
    {
        var tableAttribue = Attribute.GetCustomAttribute(typeof(T), typeof (TableAttribute));
                
        if (tableAttribue is null) throw new Exception("Not an entity");
                
        var tableName = ((TableAttribute)tableAttribue).Name;

        var dictionaryToInsert = new Dictionary<string, object?>();
        var properties = typeof(T).GetProperties(BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

        // reucpera le colonne da inserire
        foreach(var property in properties) {
            var attribute = property.GetCustomAttribute(typeof(FieldAttribute));
            if (attribute != null)
            {
                var identityAttribute = property.GetCustomAttribute(typeof(IdentityAttribute));
                if (identityAttribute is null)
                    dictionaryToInsert.Add(((FieldAttribute) attribute).Name, property.GetValue(objectToInsert));
            }
        }
    
        return Factory.Query(tableName)
            .InsertGetId<R>(dictionaryToInsert);
    }
    
    /// <summary>
    /// Insert di un elenco di oggetti
    /// </summary>
    /// <param name="objectsToInsert"></param>
    /// <typeparam name="T"></typeparam>
    /// <exception cref="Exception"></exception>
    public int MultiInsert<T>(IEnumerable<T> objectsToInsert)
    {
        var tableAttribue = Attribute.GetCustomAttribute(typeof(T), typeof (TableAttribute));
                
        if (tableAttribue is null) throw new Exception("Not an entity");
                
        var tableName = ((TableAttribute)tableAttribue).Name;

        var columsToInsert = new List<string>();
        var properties = typeof(T).GetProperties(BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

        // reucpera le colonne da inserire
        foreach(var property in properties) {
            var attribute = property.GetCustomAttribute(typeof(FieldAttribute));
            if (attribute != null)
            {
                var identityAttribute = property.GetCustomAttribute(typeof(IdentityAttribute));
                if (identityAttribute is null)
                    columsToInsert.Add(((FieldAttribute) attribute).Name);
            }
        }

        var valuesToInsert = new List<List<object?>>();
        // crea un elenco di valori per le colonne
        foreach (var singleObject in objectsToInsert)
        {
            var rowValues = new List<object?>();
            
            foreach(var property in properties) {
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

        return Factory.Query(tableName)
            .Insert(columsToInsert, valuesToInsert);
     
    } 
    
    /// <summary>
    /// Ritorna un elenco di oggetti per pagina
    /// </summary>
    /// <param name="page"></param>
    /// <param name="itemsPerPage"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public IEnumerable<T> Paginate<T>(int page = 1, int itemsPerPage = 10)
    {
                
        var tableAttribue = Attribute.GetCustomAttribute(typeof(T), typeof (TableAttribute));
                
        if (tableAttribue is null) throw new Exception("Not an entity");
                
        var tableName = ((TableAttribute)tableAttribue).Name;

        var columsToSelect = new List<string>();
        var properties = typeof(T).GetProperties(BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

        foreach(var property in properties) {
            var attribute = property.GetCustomAttribute(typeof(FieldAttribute));
            if (attribute != null)
            {
                columsToSelect.Add(((FieldAttribute) attribute).Name);
            }
        }
        
        var result = Factory.Query(tableName)
            .Select(columsToSelect.ToArray())
            .Paginate(page, itemsPerPage);
        
        var returnValue = new List<T>();
                
        foreach (var record in result.List)
        {
            var dati = (IDictionary<string, object>)record;
            
            if (dati is null) continue;
            
            var instance = Activator.CreateInstance(typeof(T));
                    
            if (instance is null) throw new Exception("Unable to instance object of type " + typeof(T).Name);
                    
            FillNewInstanceWithData<T>(properties, instance, dati);
                    
            returnValue.Add((T)instance);
        }
        
        return returnValue;
    }

    /// <summary>
    /// riempe un oggetto con i dati di una riga di una tabella
    /// </summary>
    /// <param name="properties"></param>
    /// <param name="instance"></param>
    /// <param name="dati"></param>
    /// <typeparam name="T"></typeparam>
    private static void FillNewInstanceWithData<T>(PropertyInfo[] properties, object instance, IDictionary<string, object> dati)
    {
        foreach (var property in properties)
        {
            var attribute = property.GetCustomAttribute(typeof(FieldAttribute));
            if (attribute != null)
            {
                // WriteLine($"{property.Name} : {property.GetValue(c)}");
                // WriteLine($"attribute: {((FieldAttribute)attribute).Name}");
                //property.SetValue(newInstance, record[((FieldAttribute)attribute).Name]);
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

    /// <summary>
    /// verifica l'esistenza di un oggetto
    /// </summary>
    /// <param name="key"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public bool Exist<T>(object key)
    {
        // TODO: da ottimizzare
        return FirstOrDefault<T>(key) is not null;
    }
    
    /// <summary>
    /// prende il primo oggetto che trova in archivio
    /// </summary>
    /// <param name="where"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public T? FirstOrDefault<T>(object where)
    {
        var tableAttribue = Attribute.GetCustomAttribute(typeof(T), typeof (TableAttribute));
                
        if (tableAttribue is null) throw new Exception("Not an entity");
                
        var tableName = ((TableAttribute)tableAttribue).Name;

        var columsToSelect = new List<string>();
        var properties = typeof(T).GetProperties(BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

        foreach(var property in properties) {
            var attribute = property.GetCustomAttribute(typeof(FieldAttribute));
            if (attribute != null)
            {
                columsToSelect.Add(((FieldAttribute) attribute).Name);
            }
        }

        var query = Factory.Query(tableName)
            .Select(columsToSelect.ToArray());

        // ciclare i campi valorizzati e impostare la where
        var keyProperties = where.GetType().GetProperties(BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
        foreach(var property in keyProperties)
        {
            var propertyOfT = typeof(T).GetProperty(property.Name);
            if (propertyOfT != null)
            {
                var attribute = propertyOfT.GetCustomAttribute(typeof(FieldAttribute));
                if (attribute != null)
                {
                    var value = property.GetValue(where);
                    if (value is not null) query = query.Where(((FieldAttribute) attribute).Name, value);
                }
            }
        }
        
        var result = query.FirstOrDefault();
        
        var dati = (IDictionary<string, object>)result;

        if (dati is null) return default;
        
        var instance = Activator.CreateInstance(typeof(T));
        
        FillNewInstanceWithData<T>(properties, instance, dati);

        return (T) instance;
    }
    
    /// <summary>
    /// Restituisce record multipli 
    /// </summary>
    /// <param name="where"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public IEnumerable<T> Get<T>(object where)
    {
        var tableAttribue = Attribute.GetCustomAttribute(typeof(T), typeof (TableAttribute));
                
        if (tableAttribue is null) throw new Exception("Not an entity");
                
        var tableName = ((TableAttribute)tableAttribue).Name;

        var columsToSelect = new List<string>();
        var properties = typeof(T).GetProperties(BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

        foreach(var property in properties) {
            var attribute = property.GetCustomAttribute(typeof(FieldAttribute));
            if (attribute != null)
            {
                columsToSelect.Add(((FieldAttribute) attribute).Name);
            }
        }

        var query = Factory.Query(tableName)
            .Select(columsToSelect.ToArray());

        // cicla i campi valorizzati e imposta la where
        var keyProperties = where.GetType().GetProperties(BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
        foreach(var property in keyProperties)
        {
            var propertyOfT = typeof(T).GetProperty(property.Name);
            if (propertyOfT != null)
            {
                var attribute = propertyOfT.GetCustomAttribute(typeof(FieldAttribute));
                if (attribute != null)
                {
                    var value = property.GetValue(where);
                    if (value is not null) query = query.Where(((FieldAttribute) attribute).Name, value);
                }
            }
        }
        
        var result = query.Get();

        var resultInstances = new List<T>();

        foreach (var record in result)
        {
            var dati = (IDictionary<string, object>)record;

            if (dati is null) continue;

            var instance = Activator.CreateInstance(typeof(T));
            FillNewInstanceWithData<T>(properties, instance, dati);
            resultInstances.Add((T)instance);

        }

        return resultInstances;
    }

}





