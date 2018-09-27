using System;
using System.Collections.Generic;

namespace MovChat.Data.Repositories
{
    public class UOW : IDisposable
    {
        private readonly LivechatContext context;
        private Dictionary<Type, object> repositories;

        public TRepository GetRepository<TRepository>() where TRepository : IRepositoryBase
        {
            if (repositories == null)
            {
                repositories = new Dictionary<Type, object>();
            }

            var type = typeof(TRepository);
            if (!repositories.ContainsKey(type))
            {
                repositories[type] = Activator.CreateInstance(typeof(TRepository), context);
            }

            return (TRepository)repositories[type];
        }

        #region Stuff
        public UOW(LivechatContext context)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
        }

        private bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    context.Dispose();
                }
            }
            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
