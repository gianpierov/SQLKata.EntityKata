using System;
using System.Data;
using SqlKata.Compilers;
using SqlKata.Execution;

namespace EntityKata
{
    /// <summary>
    /// Main service manager 
    /// </summary>
    public class EntityKataService : IDisposable
    {
        private bool _disposedValue;

        /// <summary>
        /// Exposed SQLKata Factory
        /// </summary>
        public QueryFactory Factory { get; }

        /// <summary>
        /// Constructor  
        /// </summary>
        public EntityKataService(IDbConnection connection, Compiler compiler, int timeout = 30)
        {
            Factory = new QueryFactory(connection, compiler, timeout);
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
        /// Instances a new entity
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public EntityManager<T> New<T>()
        {
            return new EntityManager<T>(Factory);
        }
    }
}