using System.Data;
using SqlKata.Compilers;
using SqlKata.Execution;

namespace EntityKata;

/// <summary>
/// Gestione delle entità usando SQLKata 
/// </summary>
public class EntityKataService : IDisposable
{
    
    private bool _disposedValue;

    /// <summary>
    /// Oggetto QueryFactory
    /// </summary>
    public QueryFactory Factory { get; }

    /// <summary>
    /// Costruttore
    /// </summary>
    public EntityKataService(IDbConnection connection, Compiler compiler)
    {
        Factory = new QueryFactory(connection, compiler);
    }

    /// <summary>
    /// Inizia una transazione
    /// </summary>
    /// <returns></returns>
    public IDbTransaction BeginTransaction()
    {
        Factory.Connection.Open();        
        return Factory.Connection.BeginTransaction();
        
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
    /// Gestore delle entità 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public EntityManager<T> Create<T>()
    {
        return new EntityManager<T>(Factory);
    }

}





